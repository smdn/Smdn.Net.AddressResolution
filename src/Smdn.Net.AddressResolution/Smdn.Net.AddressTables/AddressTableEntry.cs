// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Smdn.Net.AddressTables;

#pragma warning disable CA2231
public readonly struct AddressTableEntry : IEquatable<AddressTableEntry>, IEquatable<IPAddress>, IEquatable<PhysicalAddress> {
#pragma warning restore CA2231
  public static readonly AddressTableEntry Empty = default;

  /// <summary>Gets the <see cref="IEqualityComparer{AddressTableEntry}"/> that performs the default equality comparison.</summary>
  public static IEqualityComparer<AddressTableEntry> DefaultEqualityComparer { get; } = new EqualityComparer(compareExceptState: false);

  /// <summary>Gets the <see cref="IEqualityComparer{AddressTableEntry}"/> that performs equality comparisons except the value of <see cref="State"/> property.</summary>
  public static IEqualityComparer<AddressTableEntry> ExceptStateEqualityComparer { get; } = new EqualityComparer(compareExceptState: true);

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
  [MemberNotNullWhen(false, nameof(IPAddress))]
#endif
  public bool IsEmpty => IPAddress is null;

  public IPAddress? IPAddress { get; }
  public PhysicalAddress? PhysicalAddress { get; }
  public bool IsPermanent { get; }

  /// <summary>
  /// Gets the value of <see cref="AddressTableEntryState"/> that represents the state of this entry.
  /// </summary>
  public AddressTableEntryState State { get; }

  /// <summary>
  /// Gets the netowrk interface ID corresponding to this entry.
  /// </summary>
  /// <remarks>
  /// On Windows OS, this property represents the string of GUID in 'B' format representing the specific network interface, such as <c>{00000000-0000-0000-0000-000000000000}</c>.
  /// On other system, this property represents the string of specific network interface ID, such as 'eth0' or 'wlan0', etc.
  /// </remarks>
  public string? InterfaceId { get; }

  public AddressTableEntry(
    IPAddress ipAddress,
    PhysicalAddress? physicalAddress,
    bool isPermanent,
    AddressTableEntryState state,
    string? interfaceId
  )
  {
    IPAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
    PhysicalAddress = physicalAddress;
    State = state;
    IsPermanent = isPermanent;
    InterfaceId = interfaceId;
  }

  internal bool InterfaceIdEquals(string? otherInterfaceId)
    => EqualityComparer.InterfaceIdEquals(InterfaceId, otherInterfaceId);

  public override bool Equals(object? obj)
    => obj switch {
      null => false,
      AddressTableEntry entry => Equals(entry),
      _ => false,
    };

  public bool Equals(AddressTableEntry other)
    => DefaultEqualityComparer.Equals(this, other);

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
    => DefaultEqualityComparer.GetHashCode(this);

  public override string ToString()
    => $"{{IP={IPAddress}, MAC={PhysicalAddress?.ToMacAddressString() ?? "(null)"}, IsPermanent={IsPermanent}, State={State}, Iface={InterfaceId}}}";

  private sealed class EqualityComparer : EqualityComparer<AddressTableEntry> {
    // On Windows, NetworkInterface.Id is set to a string representing
    // the GUID of the network interface, but its casing conventions is
    // not specified explicitly, so perform the case-insensitive comparison.
    private static readonly StringComparer interfaceIdComparer =
      RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private readonly bool compareExceptState;

    public EqualityComparer(bool compareExceptState)
    {
      this.compareExceptState = compareExceptState;
    }

    internal static bool InterfaceIdEquals(string? x, string? y)
      => interfaceIdComparer.Equals(x, y);

    public override bool Equals(AddressTableEntry x, AddressTableEntry y)
      =>
        x.Equals(y.IPAddress) &&
        x.Equals(y.PhysicalAddress) &&
        x.IsPermanent == y.IsPermanent &&
        (compareExceptState || x.State == y.State) &&
        InterfaceIdEquals(x.InterfaceId, y.InterfaceId);

    public override int GetHashCode(AddressTableEntry obj)
    {
      static int GetHashCodeForInterfaceId(AddressTableEntry obj)
        => obj.InterfaceId is null ? 0 : interfaceIdComparer.GetHashCode(obj.InterfaceId);

#if SYSTEM_HASHCODE
      return HashCode.Combine(
        obj.IPAddress,
        obj.PhysicalAddress,
        obj.IsPermanent,
        compareExceptState ? 0 : obj.State,
        GetHashCodeForInterfaceId(obj)
      );
#else
      var hash = 17;

      unchecked {
        hash = (hash * 31) + IPAddress?.GetHashCode() ?? 0;
        hash = (hash * 31) + PhysicalAddress?.GetHashCode() ?? 0;
        hash = (hash * 31) + IsPermanent.GetHashCode();
        if (!compareExceptState)
          hash = (hash * 31) + State.GetHashCode();
        hash = (hash * 31) + GetHashCodeForInterfaceId();
      }

      return hash;
#endif
    }
  }
}
