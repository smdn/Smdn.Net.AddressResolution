// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Runtime.InteropServices;

using Smdn.Net.AddressTables;

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
}
