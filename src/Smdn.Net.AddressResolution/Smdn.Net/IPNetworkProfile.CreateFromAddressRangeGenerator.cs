// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace Smdn.Net;

#pragma warning disable IDE0040
partial class IPNetworkProfile {
#pragma warning restore IDE0040
  public static IPNetworkProfile Create(
    Func<IEnumerable<IPAddress>?> addressRangeGenerator,
    NetworkInterface? networkInterface = null
  )
    => new AddressRangeGeneratorIPNetworkProfile(
      addressRangeGenerator: addressRangeGenerator,
      networkInterface: networkInterface
    );

  private sealed class AddressRangeGeneratorIPNetworkProfile : IPNetworkProfile {
    private readonly Func<IEnumerable<IPAddress>?> addressRangeGenerator;

    public AddressRangeGeneratorIPNetworkProfile(
      Func<IEnumerable<IPAddress>?> addressRangeGenerator,
      NetworkInterface? networkInterface
    )
      : base(networkInterface: networkInterface)
    {
      this.addressRangeGenerator = addressRangeGenerator;
    }

    public override IEnumerable<IPAddress>? GetAddressRange()
      => addressRangeGenerator();
  }
}
