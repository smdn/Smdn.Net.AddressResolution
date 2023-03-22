// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressResolution.Arp;

internal sealed class ProcfsArpWithArpScanCommandMacAddressResolver : ProcfsArpMacAddressResolver {
  // ref: https://manpages.ubuntu.com/manpages/jammy/man1/arp-scan.1.html
  //   --numeric: IP addresses only, no hostnames.
  //   --quiet: Only display minimal output. No protocol decoding.
  private const string ArpScanCommandBaseOptions = "--numeric --quiet ";

  public static new bool IsSupported =>
    ProcfsArpMacAddressResolver.IsSupported &&
    lazyPathToArpScanCommand.Value is not null &&
#pragma warning disable IDE0047, SA1119
    (
#if NET8_0_OR_GREATER
      Environment.IsPrivilegedProcess ||
#endif
#if NET7_0_OR_GREATER
      HasSgidOrSuid(File.GetUnixFileMode(lazyPathToArpScanCommand.Value))
#else
      false // TODO: use Mono.Posix
#endif
    );
#pragma warning restore IDE0047, SA1119

#if NET7_0_OR_GREATER
  private static bool HasSgidOrSuid(UnixFileMode fileMode)
    => fileMode.HasFlag(UnixFileMode.SetGroup) || fileMode.HasFlag(UnixFileMode.SetUser);
#endif

  private static readonly Lazy<string?> lazyPathToArpScanCommand = new(valueFactory: GetPathToArpScanCommand, isThreadSafe: true);
  private static readonly string[] BinDirs = new[] { "/sbin/", "/usr/sbin/", "/bin/", "/usr/bin/" };

  private static string? GetPathToArpScanCommand()
    => BinDirs
      .Select(static dir => Path.Combine(dir, "arp-scan"))
      .FirstOrDefault(static pathToArpScanCommand => File.Exists(pathToArpScanCommand));

  /*
   * instance members
   */
  private readonly string arpScanCommandCommonOptions;
  private readonly string arpScanCommandFullScanOptions;

  public ProcfsArpWithArpScanCommandMacAddressResolver(
    MacAddressResolverOptions options,
    ILogger? logger
  )
    : base(
      options,
      logger
    )
  {
    arpScanCommandCommonOptions = ArpScanCommandBaseOptions;

    if (!string.IsNullOrEmpty(options.ArpScanCommandInterfaceSpecification))
      arpScanCommandCommonOptions += $"--interface={options.ArpScanCommandInterfaceSpecification} ";

    arpScanCommandFullScanOptions = string.Concat(
      arpScanCommandCommonOptions,
      // ref: https://manpages.ubuntu.com/manpages/jammy/man1/arp-scan.1.html
      //   --localnet: Generate addresses from network interface configuration.
      options.ArpScanCommandTargetSpecification ?? "--localnet "
    );
  }

  protected override ValueTask ArpFullScanAsyncCore(CancellationToken cancellationToken)
    // perform full scan
    => RunArpScanCommandAsync(
      arpScanCommandOptions: arpScanCommandFullScanOptions,
      logger: Logger,
      cancellationToken: cancellationToken
    );

  protected override ValueTask ArpScanAsyncCore(
    IEnumerable<IPAddress> invalidatedIPAddresses,
    CancellationToken cancellationToken
  )
  {
    // perform scan for specific target IPs
    var arpScanCommandTargetSpecification = string.Join(" ", invalidatedIPAddresses);

    return arpScanCommandTargetSpecification.Length == 0
      ? default // do nothing
      : RunArpScanCommandAsync(
          arpScanCommandOptions: arpScanCommandCommonOptions + arpScanCommandTargetSpecification,
          logger: Logger,
          cancellationToken: cancellationToken
        );
  }

  private static async ValueTask RunArpScanCommandAsync(
    string arpScanCommandOptions,
    ILogger? logger,
    CancellationToken cancellationToken
  )
  {
    var arpScanCommandProcessStartInfo = new ProcessStartInfo() {
      FileName = lazyPathToArpScanCommand.Value,
      Arguments = arpScanCommandOptions,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
    };

    logger?.LogDebug(
      "[arp-scan] {ProcessStartInfoFileName} {ProcessStartInfoArguments}",
      arpScanCommandProcessStartInfo.FileName,
      arpScanCommandProcessStartInfo.Arguments
    );

    using var arpScanProcess = new Process() {
      StartInfo = arpScanCommandProcessStartInfo,
    };

    try {
      arpScanProcess.Start();

#if SYSTEM_DIAGNOSTICS_PROCESS_WAITFOREXITASYNC
      await arpScanProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
#else
      arpScanProcess.WaitForExit(); // TODO: cacellation
#endif

      if (logger is not null) {
        const LogLevel logLevelForStandardOutput = LogLevel.Trace;
        const LogLevel logLevelForStandardError = LogLevel.Error;

        static IEnumerable<(StreamReader, LogLevel)> EnumerateLogTarget(StreamReader stdout, StreamReader stderr)
        {
          yield return (stdout, logLevelForStandardOutput);
          yield return (stderr, logLevelForStandardError);
        }

        foreach (var (stdio, logLevel) in EnumerateLogTarget(arpScanProcess.StandardOutput, arpScanProcess.StandardError)) {
          if (!logger.IsEnabled(logLevel))
            continue;

          for (; ;) {
            var line = await stdio.ReadLineAsync().ConfigureAwait(false);

            if (line is null)
              break;

            logger.Log(logLevel, "[arp-scan] {Line}", line);
          }
        }

        if (!logger.IsEnabled(LogLevel.Error))
          logger.LogDebug("[arp-scan] process exited with code {ExitCode}", arpScanProcess.ExitCode);
      }

      if (arpScanProcess.ExitCode != 0)
        logger?.LogError("[arp-scan] process exited with code {ExitCode}", arpScanProcess.ExitCode);
    }
    catch (Exception ex) {
      logger?.LogError(ex, "[arp-scan] failed to perform ARP scanning");
    }
  }
}
