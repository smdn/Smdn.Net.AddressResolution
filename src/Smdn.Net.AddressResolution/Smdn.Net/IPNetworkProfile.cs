// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Smdn.Net;

public abstract partial class IPNetworkProfile {
  public NetworkInterface? NetworkInterface { get; }

  protected IPNetworkProfile(NetworkInterface? networkInterface)
  {
    NetworkInterface = networkInterface;
  }

  public abstract IEnumerable<IPAddress>? GetAddressRange();
}
