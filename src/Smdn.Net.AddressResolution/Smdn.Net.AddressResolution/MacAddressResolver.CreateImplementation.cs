// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Runtime.InteropServices;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

public partial class MacAddressResolver : MacAddressResolverBase {
#pragma warning disable IDE0060
  private static INeighborTable CreateNeighborTable(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider
  )
#pragma warning restore IDE0060
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      if (IpHlpApiNeighborTable.IsSupported)
        return new IpHlpApiNeighborTable(serviceProvider);
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
      if (ProcfsArpNeighborTable.IsSupported)
        return new ProcfsArpNeighborTable(serviceProvider);
    }

    throw new PlatformNotSupportedException($"There is no {nameof(INeighborTable)} implementation available to perform neighbor table lookups for this platform currently. Please implement and supply {nameof(INeighborTable)} for this platform.");
  }

  private static INeighborDiscoverer CreateNeighborDiscoverer(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider
  )
  {
    if (NmapCommandNeighborDiscoverer.IsSupported) {
      return new NmapCommandNeighborDiscoverer(
        networkProfile: networkProfile ?? throw CreateMandatoryArgumentNullException(typeof(NmapCommandNeighborDiscoverer), nameof(networkProfile)),
        serviceProvider: serviceProvider
      );
    }

    if (ArpScanCommandNeighborDiscoverer.IsSupported) {
      return new ArpScanCommandNeighborDiscoverer(
        networkProfile: networkProfile, // nullable
        serviceProvider: serviceProvider
      );
    }

    return new PingNeighborDiscoverer(
      networkProfile: networkProfile ?? throw CreateMandatoryArgumentNullException(typeof(PingNeighborDiscoverer), nameof(networkProfile)),
      serviceProvider: serviceProvider
    );
  }

  private static Exception CreateMandatoryArgumentNullException(Type type, string paramName)
    => new InvalidOperationException(
      message: $"To construct the instance of the type {type.FullName}, the parameter '{paramName}' cannot be null.",
      innerException: new ArgumentNullException(paramName: paramName)
    );
}
