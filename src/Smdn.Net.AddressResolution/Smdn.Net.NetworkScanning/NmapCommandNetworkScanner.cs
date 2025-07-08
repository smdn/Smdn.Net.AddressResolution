// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
// cSpell:ignore nmap,grepable
using System;
using System.Collections.Generic;
using System.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.NetworkScanning;

public sealed class NmapCommandNetworkScanner : CommandNetworkScanner {
  // ref: https://nmap.org/book/man-briefoptions.html
  //   -sn: Ping Scan - disable port scan
  //   -n: Never do DNS resolution
  //   -T<0-5>: Set timing template (higher is faster)
  //     4 = aggressive
  //   -oG <file>: Output scan in Grepable format
  private const string NmapCommandBaseOptions = "-sn -n -T4 -oG - ";

  public static bool IsSupported => LazyNmapCommand.Value.IsAvailable;

  private static readonly Lazy<Command> LazyNmapCommand = new(
    valueFactory: static () => FindCommand(
      command: "nmap",
      paths: DefaultCommandPaths
    ),
    isThreadSafe: true
  );

  /*
   * instance members
   */
  private readonly string nmapCommandCommonOptions;
  private readonly string nmapCommandFullScanOptions;

  public NmapCommandNetworkScanner(
    IPNetworkProfile networkProfile,
    IServiceProvider? serviceProvider = null
  )
    : base(
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<NmapCommandNetworkScanner>(),
      serviceProvider: serviceProvider
    )
  {
    if (networkProfile is null)
      throw new ArgumentNullException(nameof(networkProfile));

    if (networkProfile.NetworkInterface is null)
      nmapCommandCommonOptions = NmapCommandBaseOptions;
    else
      // -e <iface>: Use specified interface
      nmapCommandCommonOptions = NmapCommandBaseOptions + $"-e {networkProfile.NetworkInterface.Id} ";

    var addressRange = networkProfile.GetAddressRange();
    var nmapCommandTargetSpecification = addressRange is null
      ? null
      : string.Join(" ", addressRange);

    if (string.IsNullOrEmpty(nmapCommandTargetSpecification))
      throw new InvalidOperationException($"One or more {nameof(IPAddress)} must be specified in address range.");

    nmapCommandFullScanOptions = string.Concat(
      nmapCommandCommonOptions,
      nmapCommandTargetSpecification
    );
  }

  protected override bool GetCommandLineArguments(
    out string executable,
    out string arguments
  )
  {
    executable = LazyNmapCommand.Value.GetExecutablePathOrThrow();

    // perform full scan
    arguments = nmapCommandFullScanOptions;

    return true;
  }

  protected override bool GetCommandLineArguments(
    IEnumerable<IPAddress> addressesToScan,
    out string executable,
    out string arguments
  )
  {
    executable = LazyNmapCommand.Value.GetExecutablePathOrThrow();

    var nmapCommandOptionTargetSpecification = string.Join(" ", addressesToScan);

    if (nmapCommandOptionTargetSpecification.Length == 0) {
      arguments = string.Empty;
      return false; // do nothing
    }

    // perform scan for specific target IPs
    arguments = nmapCommandCommonOptions + nmapCommandOptionTargetSpecification;

    return true;
  }
}
