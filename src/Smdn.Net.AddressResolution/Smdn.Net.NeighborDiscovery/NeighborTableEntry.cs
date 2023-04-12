// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Smdn.Net.NeighborDiscovery;

#pragma warning disable CA2231
public readonly struct NeighborTableEntry : IEquatable<NeighborTableEntry>, IEquatable<IPAddress>, IEquatable<PhysicalAddress> {
#pragma warning restore CA2231
  public static readonly NeighborTableEntry Empty = default;

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
  [MemberNotNullWhen(false, nameof(IPAddress))]
#endif
  public bool IsEmpty => IPAddress is null;

  public IPAddress? IPAddress { get; }
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

  // On Windows, NetworkInterface.Id is set to a string representing
  // the GUID of the network interface, but its casing conventions is
  // not specified explicitly, so perform the case-insensitive comparison.
  private static readonly StringComparer interfaceIdComparer =
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
      ? StringComparer.OrdinalIgnoreCase
      : StringComparer.Ordinal;

  internal bool InterfaceIdEquals(string? other)
    => interfaceIdComparer.Equals(InterfaceId, other);

  private int GetHashCodeForInterfaceId()
    => InterfaceId is null ? 0 : interfaceIdComparer.GetHashCode(InterfaceId);

  public override bool Equals(object? obj)
    => obj switch {
      null => false,
      NeighborTableEntry entry => Equals(entry),
      _ => false,
    };

  public bool Equals(NeighborTableEntry other)
    =>
      Equals(other.IPAddress) &&
      Equals(other.PhysicalAddress) &&
      IsPermanent == other.IsPermanent &&
      State == other.State &&
      InterfaceIdEquals(other.InterfaceId);

  public bool Equals(IPAddress? other)
  {
    if (IPAddress is null)
      return other is null;

    return IPAddress.Equals(other);
  }

  public bool Equals(PhysicalAddress? other)
  {
    if (PhysicalAddress is null)
      return other is null;

    return PhysicalAddress.Equals(other);
  }

  public override int GetHashCode()
#if SYSTEM_HASHCODE
    => HashCode.Combine(
      IPAddress,
      PhysicalAddress,
      IsPermanent,
      State,
      GetHashCodeForInterfaceId()
    );
#else
  {
    var hash = 17;

    unchecked {
      hash = (hash * 31) + IPAddress?.GetHashCode() ?? 0;
      hash = (hash * 31) + PhysicalAddress?.GetHashCode() ?? 0;
      hash = (hash * 31) + IsPermanent.GetHashCode();
      hash = (hash * 31) + State.GetHashCode();
      hash = (hash * 31) + GetHashCodeForInterfaceId();
    }

    return hash;
  }
#endif

  public override string ToString()
    => $"{{IP={IPAddress}, MAC={PhysicalAddress?.ToMacAddressString() ?? "(null)"}, IsPermanent={IsPermanent}, State={State}, Iface={InterfaceId}}}";
}
