// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressResolution.Arp;

internal sealed class ProcfsArpWithNmapCommandMacAddressResolver : ProcfsArpMacAddressResolver {
  // ref: https://nmap.org/book/man-briefoptions.html
  //   -sn: Ping Scan - disable port scan
  //   -n: Never do DNS resolution
  //   -T<0-5>: Set timing template (higher is faster)
  //     4 = aggressive
  //   -oG <file>: Output scan in Grepable format
  private const string NmapCommandBaseOptions = "-sn -n -T4 -oG - ";

  public static new bool IsSupported => ProcfsArpMacAddressResolver.IsSupported && lazyPathToNmapCommand.Value is not null;

  private static readonly Lazy<string?> lazyPathToNmapCommand = new(valueFactory: GetPathToNmapCommand, isThreadSafe: true);
  private static readonly string[] BinDirs = new[] { "/bin/", "/sbin/", "/usr/bin/" };

  private static string? GetPathToNmapCommand()
    => BinDirs
      .Select(static dir => Path.Combine(dir, "nmap"))
      .FirstOrDefault(static nmap => File.Exists(nmap));

  /*
   * instance members
   */
  private readonly string nmapCommandCommonOptions;
  private readonly string nmapCommandFullScanOptions;

  public ProcfsArpWithNmapCommandMacAddressResolver(
    MacAddressResolverOptions options,
    IServiceProvider? serviceProvider
  )
    : base(
      options: options,
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ProcfsArpWithArpScanCommandMacAddressResolver>(),
      serviceProvider: serviceProvider
    )
  {
    if (string.IsNullOrEmpty(options.NmapCommandInterfaceSpecification))
      nmapCommandCommonOptions = NmapCommandBaseOptions;
    else
      // -e <iface>: Use specified interface
      nmapCommandCommonOptions = NmapCommandBaseOptions + $"-e {options.NmapCommandInterfaceSpecification} ";

    nmapCommandFullScanOptions = string.Concat(
      nmapCommandCommonOptions,
      options.NmapCommandTargetSpecification
        ?? throw new ArgumentException($"{nameof(options.NmapCommandTargetSpecification)} must be specified with {nameof(MacAddressResolverOptions)}")
    );
  }

  protected override ValueTask ArpFullScanAsyncCore(CancellationToken cancellationToken)
    // perform full scan
    => RunNmapCommandAsync(
      nmapCommandOptions: nmapCommandFullScanOptions,
      logger: Logger,
      cancellationToken: cancellationToken
    );

  protected override ValueTask ArpScanAsyncCore(
    IEnumerable<IPAddress> invalidatedIPAddresses,
    CancellationToken cancellationToken
  )
  {
    // perform scan for specific target IPs
    var nmapCommandOptionTargetSpecification = string.Join(" ", invalidatedIPAddresses);

    return nmapCommandOptionTargetSpecification.Length == 0
      ? default // do nothing
      : RunNmapCommandAsync(
          nmapCommandOptions: nmapCommandCommonOptions + nmapCommandOptionTargetSpecification,
          logger: Logger,
          cancellationToken: cancellationToken
        );
  }

  private static async ValueTask RunNmapCommandAsync(
    string nmapCommandOptions,
    ILogger? logger,
    CancellationToken cancellationToken
  )
  {
    var nmapCommandProcessStartInfo = new ProcessStartInfo() {
      FileName = lazyPathToNmapCommand.Value,
      Arguments = nmapCommandOptions,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
    };

    logger?.LogDebug(
      "[nmap] {ProcessStartInfoFileName} {ProcessStartInfoArguments}",
      nmapCommandProcessStartInfo.FileName,
      nmapCommandProcessStartInfo.Arguments
    );

    using var nmapProcess = new Process() {
      StartInfo = nmapCommandProcessStartInfo,
    };

    try {
      nmapProcess.Start();

#if SYSTEM_DIAGNOSTICS_PROCESS_WAITFOREXITASYNC
      await nmapProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
#else
      nmapProcess.WaitForExit(); // TODO: cacellation
#endif

      if (logger is not null) {
        const LogLevel logLevelForStandardOutput = LogLevel.Trace;
        const LogLevel logLevelForStandardError = LogLevel.Error;

        static IEnumerable<(StreamReader, LogLevel)> EnumerateLogTarget(StreamReader stdout, StreamReader stderr)
        {
          yield return (stdout, logLevelForStandardOutput);
          yield return (stderr, logLevelForStandardError);
        }

        foreach (var (stdio, logLevel) in EnumerateLogTarget(nmapProcess.StandardOutput, nmapProcess.StandardError)) {
          if (!logger.IsEnabled(logLevel))
            continue;

          for (; ;) {
            var line = await stdio.ReadLineAsync().ConfigureAwait(false);

            if (line is null)
              break;

            logger.Log(logLevel, "[nmap] {Line}", line);
          }
        }

        if (!logger.IsEnabled(LogLevel.Error))
          logger.LogDebug("[nmap] process exited with code {ExitCode}", nmapProcess.ExitCode);
      }

      if (nmapProcess.ExitCode != 0)
        logger?.LogError("[nmap] process exited with code {ExitCode}", nmapProcess.ExitCode);
    }
    catch (Exception ex) {
      logger?.LogError(ex, "[nmap] failed to perform ARP scanning");
    }
  }
}
