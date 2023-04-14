// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.AddressTables;

#pragma warning disable IDE0040
partial class AddressTable {
#pragma warning restore IDE0040
  public static IAddressTable Create(
    IServiceProvider? serviceProvider = null
  )
  {
    if (IpHlpApiAddressTable.IsSupported)
      return new IpHlpApiAddressTable(serviceProvider);

    if (ProcfsArpAddressTable.IsSupported)
      return new ProcfsArpAddressTable(serviceProvider);

    throw new PlatformNotSupportedException(
      message:
        $"There is no {nameof(IAddressTable)} implementation available for the current platform. " +
        $"Implement {nameof(IAddressTable)} for the current platform instead."
    );
  }
}
