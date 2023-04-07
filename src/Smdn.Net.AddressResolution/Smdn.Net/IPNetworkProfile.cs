// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace Smdn.Net;

public abstract partial class IPNetworkProfile {
  private static Exception CreateIPv6FeatureNotImplemented()
    => new NotImplementedException("IPv6 is not supported yet. Please contribute to the implementation of the feature.");

  private static Exception CreateNonIPAddressFamilyNotSupported()
    => new NotSupportedException("Addresses other than IPv4 and IPv6 are not supported.");

  public NetworkInterface? NetworkInterface { get; }

  protected IPNetworkProfile(NetworkInterface? networkInterface)
  {
    NetworkInterface = networkInterface;
  }

  public abstract IEnumerable<IPAddress>? GetAddressRange();
}
