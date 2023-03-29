// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution.Arp;

internal sealed class ProcfsArpWithArpScanCommandMacAddressResolver : ProcfsArpMacAddressResolver {
  public static new bool IsSupported => ProcfsArpMacAddressResolver.IsSupported && ArpScanCommandNeighborDiscoverer.IsSupported;

  /*
   * instance members
   */
  public ProcfsArpWithArpScanCommandMacAddressResolver(
    MacAddressResolverOptions options,
    IServiceProvider? serviceProvider
  )
    : base(
      options: options,
      neighborDiscoverer: new ArpScanCommandNeighborDiscoverer(options, serviceProvider),
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ProcfsArpWithArpScanCommandMacAddressResolver>(),
      serviceProvider: serviceProvider
    )
  {
  }
}
