// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
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

    var outputNmapOutputToTraceLog = Logger is not null && Logger.IsEnabled(LogLevel.Trace);
    var outputNmapErrorToErrorLog = Logger is not null && Logger.IsEnabled(LogLevel.Error);

    var nmapProcessStartInfo = new ProcessStartInfo() {
      FileName = lazyPathToNmap.Value,
      Arguments = nmapOptions + nmapTargetSpecification,
      RedirectStandardOutput = outputNmapOutputToTraceLog,
      RedirectStandardError = outputNmapErrorToErrorLog,
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

      if (outputNmapOutputToTraceLog) {
        for (
          var line = await nmapProcess.StandardOutput.ReadLineAsync().ConfigureAwait(false);
          line is not null;
          line = await nmapProcess.StandardOutput.ReadLineAsync().ConfigureAwait(false)
        ) {
          Logger!.LogTrace("[nmap] {StdOut}", line);
        }
      }

      if (outputNmapErrorToErrorLog) {
        for (
          var line = await nmapProcess.StandardError.ReadLineAsync().ConfigureAwait(false);
          line is not null;
          line = await nmapProcess.StandardError.ReadLineAsync().ConfigureAwait(false)
        ) {
          Logger!.LogError("[nmap] {StdErr}", line);
        }
      }
    }
    catch (Exception ex) {
      Logger?.LogError(ex, "[nmap] failed to perform ARP scanning");
    }
  }
}
