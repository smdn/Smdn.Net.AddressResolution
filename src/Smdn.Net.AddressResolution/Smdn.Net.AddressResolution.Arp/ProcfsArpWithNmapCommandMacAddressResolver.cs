// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution.Arp;

internal sealed class ProcfsArpWithNmapCommandMacAddressResolver : ProcfsArpMacAddressResolver {
  public static new bool IsSupported => ProcfsArpMacAddressResolver.IsSupported && NmapCommandNeighborDiscoverer.IsSupported;

  /*
   * instance members
   */
  public ProcfsArpWithNmapCommandMacAddressResolver(
    MacAddressResolverOptions options,
    IServiceProvider? serviceProvider
  )
    : base(
      options: options,
      neighborDiscoverer: new NmapCommandNeighborDiscoverer(options, serviceProvider),
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ProcfsArpWithNmapCommandMacAddressResolver>(),
      serviceProvider: serviceProvider
    )
  {
  }
}
