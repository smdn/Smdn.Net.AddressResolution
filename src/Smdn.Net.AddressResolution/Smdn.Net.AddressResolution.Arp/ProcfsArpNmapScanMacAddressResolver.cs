// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

  protected override async ValueTask ArpScanAsyncCore(CancellationToken cancellationToken)
  {
    // -sn: Ping Scan - disable port scan
    // -n: Never do DNS resolution
    // -T<0-5>: Set timing template (higher is faster)
    //   4 = aggressive
    // -oG <file>: Output scan in Grepable format
    const string nmapOptions = "-sn -n -T4 -oG - ";

    var nmapProcessStartInfo = new ProcessStartInfo() {
      FileName = lazyPathToNmap.Value,
      Arguments = nmapOptions + nmapTargetSpecification,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
    };

    Logger?.LogDebug(
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

      if (Logger is not null) {
        const LogLevel logLevelForStandardOutput = LogLevel.Trace;
        const LogLevel logLevelForStandardError = LogLevel.Error;

        static IEnumerable<(StreamReader, LogLevel)> EnumerateLogTarget(StreamReader stdout, StreamReader stderr)
        {
          yield return (stdout, logLevelForStandardOutput);
          yield return (stderr, logLevelForStandardError);
        }

        foreach (var (stdio, logLevel) in EnumerateLogTarget(nmapProcess.StandardOutput, nmapProcess.StandardError)) {
          if (!Logger.IsEnabled(logLevel))
            continue;

          for (; ;) {
            var line = await stdio.ReadLineAsync().ConfigureAwait(false);

            if (line is null)
              break;

            Logger.Log(logLevel, "[nmap] {Line}", line);
          }
        }
      }
    }
    catch (Exception ex) {
      Logger?.LogError(ex, "[nmap] failed to perform ARP scanning");
    }
  }
}
