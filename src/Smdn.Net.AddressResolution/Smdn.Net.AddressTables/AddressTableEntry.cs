// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Net;
using System.Net.NetworkInformation;

namespace Smdn.Net.AddressTables;

#pragma warning disable CA2231
public readonly struct AddressTableEntry : IEquatable<AddressTableEntry>, IEquatable<IPAddress>, IEquatable<PhysicalAddress> {
#pragma warning restore CA2231
  public static readonly AddressTableEntry Empty;

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
    => Equals(other, shouldConsiderIPv4MappedIPv6Address: false);

  /// <summary>
  /// Indicates whether the member <see cref="AddressTableEntry.IPAddress"/> is equal to the IP address passed to the parameter.
  /// </summary>
  /// <param name="other">The <see cref="System.Net.IPAddress"/> to be compared with the <see cref="AddressTableEntry.IPAddress"/>.</param>
  /// <param name="shouldConsiderIPv4MappedIPv6Address">
  /// Specifies whether or not to be aware that the IP address to be an IPv4-mapped IPv6 address or not when comparing IP addresses.
  /// </param>
  /// <returns>
  /// <see langword="true"/> if the <see cref="AddressTableEntry.IPAddress"/> is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.
  /// </returns>
  public bool Equals(IPAddress? other, bool shouldConsiderIPv4MappedIPv6Address)
  {
    if (other is null)
      return IPAddress is null;

    if (shouldConsiderIPv4MappedIPv6Address) {
      if (other.IsIPv4MappedToIPv6 && other.MapToIPv4().Equals(IPAddress))
        return true;
      if (IPAddress is not null && IPAddress.IsIPv4MappedToIPv6 && other.Equals(IPAddress.MapToIPv4()))
        return true;
    }

    return other.Equals(IPAddress);
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
    private readonly bool compareExceptState;

    public EqualityComparer(bool compareExceptState)
    {
      this.compareExceptState = compareExceptState;
    }

    internal static bool InterfaceIdEquals(string? x, string? y)
      => NetworkInterfaceIdComparer.Comparer.Equals(x, y);

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
        => obj.InterfaceId is null ? 0 : NetworkInterfaceIdComparer.Comparer.GetHashCode(obj.InterfaceId);

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
