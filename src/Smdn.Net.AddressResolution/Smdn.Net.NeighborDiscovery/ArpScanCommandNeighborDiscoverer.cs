// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smdn.Net.AddressResolution;

namespace Smdn.Net.NeighborDiscovery;

public class ArpScanCommandNeighborDiscoverer : RunCommandNeighborDiscovererBase {
  // ref: https://manpages.ubuntu.com/manpages/jammy/man1/arp-scan.1.html
  //   --numeric: IP addresses only, no hostnames.
  //   --quiet: Only display minimal output. No protocol decoding.
  private const string ArpScanCommandBaseOptions = "--numeric --quiet ";

  public static bool IsSupported =>
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

  private static readonly Lazy<string> lazyPathToArpScanCommand = new(
    valueFactory: static () => FindPathToCommand(
      command: "arp-scan",
      paths: new[] { "/sbin/", "/usr/sbin/", "/bin/", "/usr/bin/" }
    ),
    isThreadSafe: true
  );

  /*
   * instance members
   */
  private readonly string arpScanCommandCommonOptions;
  private readonly string arpScanCommandFullScanOptions;

  public ArpScanCommandNeighborDiscoverer(
    MacAddressResolverOptions options,
    IServiceProvider? serviceProvider
  )
    : base(
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ArpScanCommandNeighborDiscoverer>()
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

  protected override bool GetCommandLineArguments(
    out string executable,
    out string arguments
  )
  {
    executable = lazyPathToArpScanCommand.Value;

    // perform full scan
    arguments = arpScanCommandFullScanOptions;

    return true;
  }

  protected override bool GetCommandLineArguments(
    IEnumerable<IPAddress> addressesToDiscover,
    out string executable,
    out string arguments
  )
  {
    executable = lazyPathToArpScanCommand.Value;

    var arpScanCommandTargetSpecification = string.Join(" ", addressesToDiscover);

    if (arpScanCommandTargetSpecification.Length == 0) {
      arguments = string.Empty;
      return false; // do nothing
    }

    // perform scan for specific target IPs
    arguments = arpScanCommandCommonOptions + arpScanCommandTargetSpecification;

    return true;
  }
}
