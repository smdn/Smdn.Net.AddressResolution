// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
// cSpell:ignore nmap
using System;

namespace Smdn.Net.NetworkScanning;

#pragma warning disable IDE0040
partial class NetworkScanner {
#pragma warning restore IDE0040
  public static INetworkScanner Create(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider = null
  )
  {
    if (NmapCommandNetworkScanner.IsSupported && networkProfile is not null) {
      return new NmapCommandNetworkScanner(
        networkProfile: networkProfile,
        serviceProvider: serviceProvider
      );
    }

    if (ArpScanCommandNetworkScanner.IsSupported) {
      return new ArpScanCommandNetworkScanner(
        networkProfile: networkProfile, // nullable
        serviceProvider: serviceProvider
      );
    }

    if (networkProfile is not null) {
      return new PingNetworkScanner(
        networkProfile: networkProfile,
        serviceProvider: serviceProvider
      );
    }

    throw new PlatformNotSupportedException(
      message:
        $"There is no {nameof(INetworkScanner)} implementation available for the current platform. " +
        $"Consider installing the supported network scan command (nmap or arp-scan), making sure the PATH environment variable is set, or specifying {nameof(IPNetworkProfile)} to {nameof(networkProfile)} parameter."
    );
  }
}
