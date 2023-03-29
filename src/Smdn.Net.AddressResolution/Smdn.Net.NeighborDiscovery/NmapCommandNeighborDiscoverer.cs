// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smdn.Net.AddressResolution;

namespace Smdn.Net.NeighborDiscovery;

public class NmapCommandNeighborDiscoverer : RunCommandNeighborDiscovererBase {
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
    MacAddressResolverOptions options,
    IServiceProvider? serviceProvider
  )
    : base(
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<NmapCommandNeighborDiscoverer>()
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
