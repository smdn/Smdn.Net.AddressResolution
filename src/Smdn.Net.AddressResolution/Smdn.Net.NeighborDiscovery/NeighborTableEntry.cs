// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;

namespace Smdn.Net.NeighborDiscovery;

public readonly struct NeighborTableEntry : IEquatable<IPAddress?>, IEquatable<PhysicalAddress?> {
  public IPAddress IPAddress { get; }
  public PhysicalAddress? PhysicalAddress { get; }
  public bool IsPermanent { get; }
  public NeighborTableEntryState State { get; } = default;
  public int? InterfaceIndex { get; }
  public string? InterfaceName { get; }

  public NeighborTableEntry(
    IPAddress ipAddress,
    PhysicalAddress? physicalAddress,
    bool isPermanent,
    NeighborTableEntryState state,
    int? interfaceIndex = null,
    string? interfaceName = null
  )
  {
    IPAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
    PhysicalAddress = physicalAddress;
    State = state;
    IsPermanent = isPermanent;
    InterfaceIndex = interfaceIndex;
    InterfaceName = interfaceName;
  }

  public bool Equals(IPAddress? other)
  {
    if (IPAddress is null && other is null)
      return true;

    return IPAddress is not null && IPAddress.Equals(other);
  }

  public bool Equals(PhysicalAddress? other)
  {
    if (PhysicalAddress is null && other is null)
      return true;

    return PhysicalAddress is not null && PhysicalAddress.Equals(other);
  }
}
