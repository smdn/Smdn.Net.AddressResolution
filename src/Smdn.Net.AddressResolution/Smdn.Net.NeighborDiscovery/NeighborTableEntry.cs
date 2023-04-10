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
  public NeighborTableEntryState State { get; }
  public string? InterfaceId { get; }

  public NeighborTableEntry(
    IPAddress ipAddress,
    PhysicalAddress? physicalAddress,
    bool isPermanent,
    NeighborTableEntryState state,
    string? interfaceId
  )
  {
    IPAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
    PhysicalAddress = physicalAddress;
    State = state;
    IsPermanent = isPermanent;
    InterfaceId = interfaceId;
  }

  public bool Equals(IPAddress? other)
    => IPAddress.Equals(other);

  public bool Equals(PhysicalAddress? other)
  {
    if (PhysicalAddress is null && other is null)
      return true;

    return PhysicalAddress is not null && PhysicalAddress.Equals(other);
  }

  public override string ToString()
    => $"{{IP={IPAddress}, MAC={PhysicalAddress?.ToMacAddressString() ?? "(null)"}, IsPermanent={IsPermanent}, State={State}, Iface={InterfaceId}}}";
}
