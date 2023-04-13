// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Runtime.InteropServices;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

public partial class MacAddressResolver : MacAddressResolverBase {
#pragma warning disable IDE0060
  private static IAddressTable CreateAddressTable(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider
  )
#pragma warning restore IDE0060
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      if (IpHlpApiAddressTable.IsSupported)
        return new IpHlpApiAddressTable(serviceProvider);
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
      if (ProcfsArpAddressTable.IsSupported)
        return new ProcfsArpAddressTable(serviceProvider);
    }

    throw new PlatformNotSupportedException($"There is no {nameof(IAddressTable)} implementation available to perform address table lookups for this platform currently. Please implement and supply {nameof(IAddressTable)} for this platform.");
  }

  private static INetworkScanner CreateNetworkScanner(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider
  )
  {
    if (NmapCommandNetworkScanner.IsSupported) {
      return new NmapCommandNetworkScanner(
        networkProfile: networkProfile ?? throw CreateMandatoryArgumentNullException(typeof(NmapCommandNetworkScanner), nameof(networkProfile)),
        serviceProvider: serviceProvider
      );
    }

    if (ArpScanCommandNetworkScanner.IsSupported) {
      return new ArpScanCommandNetworkScanner(
        networkProfile: networkProfile, // nullable
        serviceProvider: serviceProvider
      );
    }

    return new PingNetworkScanner(
      networkProfile: networkProfile ?? throw CreateMandatoryArgumentNullException(typeof(PingNetworkScanner), nameof(networkProfile)),
      serviceProvider: serviceProvider
    );
  }

  private static Exception CreateMandatoryArgumentNullException(Type type, string paramName)
    => new InvalidOperationException(
      message: $"To construct the instance of the type {type.FullName}, the parameter '{paramName}' cannot be null.",
      innerException: new ArgumentNullException(paramName: paramName)
    );
}
