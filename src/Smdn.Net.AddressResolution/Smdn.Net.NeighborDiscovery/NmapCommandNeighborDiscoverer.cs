// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.NeighborDiscovery;

public sealed class NmapCommandNeighborDiscoverer : RunCommandNeighborDiscovererBase {
  // ref: https://nmap.org/book/man-briefoptions.html
  //   -sn: Ping Scan - disable port scan
  //   -n: Never do DNS resolution
  //   -T<0-5>: Set timing template (higher is faster)
  //     4 = aggressive
  //   -oG <file>: Output scan in Grepable format
  private const string NmapCommandBaseOptions = "-sn -n -T4 -oG - ";

  public static bool IsSupported => lazyPathToNmapCommand.Value is not null;

  private static readonly Lazy<string> lazyPathToNmapCommand = new(
    valueFactory: static () => FindPathToCommand(
      command: "nmap",
      paths: new[] { "/bin/", "/sbin/", "/usr/bin/" }
    ),
    isThreadSafe: true
  );

  /*
   * instance members
   */
  private readonly string nmapCommandCommonOptions;
  private readonly string nmapCommandFullScanOptions;

  public NmapCommandNeighborDiscoverer(
    IPNetworkProfile networkProfile,
    IServiceProvider? serviceProvider
  )
    : base(
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<NmapCommandNeighborDiscoverer>()
    )
  {
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
    executable = lazyPathToNmapCommand.Value;

    // perform full scan
    arguments = nmapCommandFullScanOptions;

    return true;
  }

  protected override bool GetCommandLineArguments(
    IEnumerable<IPAddress> addressesToDiscover,
    out string executable,
    out string arguments
  )
  {
    executable = lazyPathToNmapCommand.Value;

    var nmapCommandOptionTargetSpecification = string.Join(" ", addressesToDiscover);

    if (nmapCommandOptionTargetSpecification.Length == 0) {
      arguments = string.Empty;
      return false; // do nothing
    }

    // perform scan for specific target IPs
    arguments = nmapCommandCommonOptions + nmapCommandOptionTargetSpecification;

    return true;
  }
}
