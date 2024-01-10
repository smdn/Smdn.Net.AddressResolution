// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace Smdn.Net;

/// <summary>Defines the target network interface and/or the network address range.</summary>
/// <remarks>
/// <see cref="Smdn.Net.AddressResolution.MacAddressResolver"/> uses <see cref="IPNetworkProfile"/> to specify a
/// range of network addresses when performing address resolution and network scanning,
/// as well as the target network interface to be scanned.
/// </remarks>
/// <seealso cref="Smdn.Net.AddressResolution.MacAddressResolver"/>
/// <seealso cref="Smdn.Net.NetworkScanning.NetworkScanner"/>
public abstract partial class IPNetworkProfile {
  private static NotImplementedException CreateIPv6FeatureNotImplemented()
    => new("IPv6 is not supported yet. Please contribute to the implementation of the feature.");

  private static NotSupportedException CreateNonIPAddressFamilyNotSupported()
    => new("Addresses other than IPv4 and IPv6 are not supported.");

  public NetworkInterface? NetworkInterface { get; }

  protected IPNetworkProfile(NetworkInterface? networkInterface)
  {
    NetworkInterface = networkInterface;
  }

  public abstract IEnumerable<IPAddress>? GetAddressRange();
}
