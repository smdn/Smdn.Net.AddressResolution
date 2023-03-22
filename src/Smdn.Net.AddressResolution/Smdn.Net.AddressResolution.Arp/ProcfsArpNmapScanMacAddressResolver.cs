// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressResolution.Arp;

internal sealed class ProcfsArpNmapScanMacAddressResolver : ProcfsArpMacAddressResolver {
  // ref: https://nmap.org/book/man-briefoptions.html
  //   -sn: Ping Scan - disable port scan
  //   -n: Never do DNS resolution
  //   -T<0-5>: Set timing template (higher is faster)
  //     4 = aggressive
  //   -oG <file>: Output scan in Grepable format
  private const string NmapCommandBaseOptions = "-sn -n -T4 -oG - ";

  public static new bool IsSupported => ProcfsArpMacAddressResolver.IsSupported && lazyPathToNmap.Value is not null;

  private static readonly Lazy<string?> lazyPathToNmap = new(valueFactory: GetPathToNmap, isThreadSafe: true);
  private static readonly string[] BinDirs = new[] { "/bin/", "/sbin/", "/usr/bin/" };

  private static string? GetPathToNmap()
    => BinDirs
      .Select(static dir => Path.Combine(dir, "nmap"))
      .FirstOrDefault(static nmap => File.Exists(nmap));

  /*
   * instance members
   */
  private readonly string nmapCommandCommonOptions;
  private readonly string nmapCommandFullScanOptions;

  public ProcfsArpNmapScanMacAddressResolver(
    MacAddressResolverOptions options,
    ILogger? logger
  )
    : base(
      options,
      logger
    )
  {
    if (string.IsNullOrEmpty(options.NmapInterfaceSpecification))
      nmapCommandCommonOptions = NmapCommandBaseOptions;
    else
      // -e <iface>: Use specified interface
      nmapCommandCommonOptions = NmapCommandBaseOptions + $"-e {options.NmapInterfaceSpecification} ";

    nmapCommandFullScanOptions = string.Concat(
      nmapCommandCommonOptions,
      options.NmapTargetSpecification
        ?? throw new ArgumentException($"{nameof(options.NmapTargetSpecification)} must be specified with {nameof(MacAddressResolverOptions)}")
    );
  }

  protected override ValueTask ArpFullScanAsyncCore(CancellationToken cancellationToken)
    => NmapScanAsync(
      nmapCommandOptions: nmapCommandFullScanOptions,
      logger: Logger,
      cancellationToken: cancellationToken
    );

  protected override ValueTask ArpScanAsyncCore(
    IEnumerable<IPAddress> invalidatedIPAddresses,
    IEnumerable<PhysicalAddress> invalidatedMacAddresses,
    CancellationToken cancellationToken
  )
  {
    if (invalidatedMacAddresses.Any()) {
      // perform full scan
      return NmapScanAsync(
        nmapCommandOptions: nmapCommandFullScanOptions,
        logger: Logger,
        cancellationToken: cancellationToken
      );
    }

    // perform scan for specific target IPs
    var nmapCommandOptionTargetSpecification = string.Join(" ", invalidatedIPAddresses);

    return nmapCommandOptionTargetSpecification.Length == 0
      ? default // do nothing
      : NmapScanAsync(
          nmapCommandOptions: nmapCommandCommonOptions + nmapCommandOptionTargetSpecification,
          logger: Logger,
          cancellationToken: cancellationToken
        );
  }

  private static async ValueTask NmapScanAsync(
    string nmapCommandOptions,
    ILogger? logger,
    CancellationToken cancellationToken
  )
  {
    var nmapCommandProcessStartInfo = new ProcessStartInfo() {
      FileName = lazyPathToNmap.Value,
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
      }
    }
    catch (Exception ex) {
      logger?.LogError(ex, "[nmap] failed to perform ARP scanning");
    }
  }
}
