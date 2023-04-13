// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net.NetworkInformation;

namespace Smdn.Net;

internal sealed class PseudoNetworkInterface : NetworkInterface {
  private readonly string id;
  private readonly bool supportsIPv4;
  private readonly bool supportsIPv6;

  public PseudoNetworkInterface(
    string id,
    bool supportsIPv4 = true,
    bool supportsIPv6 = true
  )
  {
    this.id = id;
    this.supportsIPv4 = supportsIPv4;
    this.supportsIPv6 = supportsIPv6;
  }

  public override string Id => id;
  public override string Name => nameof(PseudoNetworkInterface);
  public override string Description => nameof(PseudoNetworkInterface);
  public override OperationalStatus OperationalStatus => OperationalStatus.Unknown;
  public override long Speed => 0L;
  public override bool IsReceiveOnly => false;
  public override bool SupportsMulticast => false;
  public override NetworkInterfaceType NetworkInterfaceType => NetworkInterfaceType.Loopback;

  public override IPInterfaceProperties GetIPProperties() => throw new NotImplementedException();
  public override IPInterfaceStatistics GetIPStatistics() => throw new NotImplementedException();
  public override IPv4InterfaceStatistics GetIPv4Statistics() => throw new NotImplementedException();
  public override PhysicalAddress GetPhysicalAddress() => throw new NotImplementedException();

  public override bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
    => networkInterfaceComponent switch {
      NetworkInterfaceComponent.IPv4 => supportsIPv4,
      NetworkInterfaceComponent.IPv6 => supportsIPv6,
      _ => false
    };
}
