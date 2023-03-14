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
  private readonly string nmapTargetSpecification;

  public ProcfsArpNmapScanMacAddressResolver(
    MacAddressResolverOptions options,
    ILogger? logger
  )
    : base(
      options,
      logger
    )
  {
    nmapTargetSpecification = options.NmapTargetSpecification
      ?? throw new ArgumentException($"{nameof(options.NmapTargetSpecification)} must be specified with {nameof(MacAddressResolverOptions)}");
  }

  protected override ValueTask ArpFullScanAsyncCore(CancellationToken cancellationToken)
    => NmapScanAsync(
      nmapOptionTargetSpecification: nmapTargetSpecification,
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
        nmapOptionTargetSpecification: nmapTargetSpecification,
        logger: Logger,
        cancellationToken: cancellationToken
      );
    }

    // perform scan for specific target IPs
    var nmapOptionTargetSpecification = string.Join(" ", invalidatedIPAddresses);

    return nmapOptionTargetSpecification.Length == 0
      ? default // do nothing
      : NmapScanAsync(
          nmapOptionTargetSpecification: nmapOptionTargetSpecification,
          logger: Logger,
          cancellationToken: cancellationToken
        );
  }

  private static async ValueTask NmapScanAsync(
    string nmapOptionTargetSpecification,
    ILogger? logger,
    CancellationToken cancellationToken
  )
  {
    // -sn: Ping Scan - disable port scan
    // -n: Never do DNS resolution
    // -T<0-5>: Set timing template (higher is faster)
    //   4 = aggressive
    // -oG <file>: Output scan in Grepable format
    const string nmapOptions = "-sn -n -T4 -oG - ";

    var nmapProcessStartInfo = new ProcessStartInfo() {
      FileName = lazyPathToNmap.Value,
      Arguments = nmapOptions + nmapOptionTargetSpecification,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
    };

    logger?.LogDebug(
      "[nmap] {ProcessStartInfoFileName} {ProcessStartInfoArguments}",
      nmapProcessStartInfo.FileName,
      nmapProcessStartInfo.Arguments
    );

    using var nmapProcess = new Process() {
      StartInfo = nmapProcessStartInfo,
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
