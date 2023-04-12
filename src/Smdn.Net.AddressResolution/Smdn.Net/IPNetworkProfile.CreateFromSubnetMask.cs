// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if NET8_0_OR_GREATER
// #define SYSTEM_NET_IPNETWORK
#endif

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Smdn.Net;

#pragma warning disable IDE0040
partial class IPNetworkProfile {
#pragma warning restore IDE0040

#if SYSTEM_NET_IPNETWORK
  public static IPNetworkProfile Create(
    IPNetwork network,
    NetworkInterface? networkInterface = null
  )
    => Create(
      baseAddress: network.BaseAddress,
      prefixLength: network.PrefixLength,
      networkInterface: networkInterface
    );
#endif

  public static IPNetworkProfile Create(
    IPAddress baseAddress,
    int prefixLength,
    NetworkInterface? networkInterface = null
  )
  {
    if (baseAddress is null)
      throw new ArgumentNullException(nameof(baseAddress));

    switch (baseAddress.AddressFamily) {
      case AddressFamily.InterNetwork: {
        if (prefixLength is < 1 or > 32)
          throw new ArgumentOutOfRangeException(paramName: nameof(prefixLength));

        var ui32SubnetMask = prefixLength == 32
          ? 0xFFFFFFFFu
          : ~((1u << (32 - prefixLength)) - 1u);

        return Create(
          baseAddress,
          new IPAddress(HostToNetworkOrder(ui32SubnetMask)),
          networkInterface
        );
      }

      case AddressFamily.InterNetworkV6:
        // TODO: IPv6
        throw CreateIPv6FeatureNotImplemented();

      default:
        throw CreateNonIPAddressFamilyNotSupported();
    }
  }

  public static IPNetworkProfile Create(
    IPAddress baseAddress,
    IPAddress subnetMask,
    NetworkInterface? networkInterface = null
  )
  {
    if (baseAddress is null)
      throw new ArgumentNullException(nameof(baseAddress));
    if (subnetMask is null)
      throw new ArgumentNullException(nameof(subnetMask));
    if (baseAddress.AddressFamily != subnetMask.AddressFamily)
      throw new ArgumentException("address family mismatch");

    return baseAddress.AddressFamily switch {
      AddressFamily.InterNetwork => new IPv4AddressRangeNetworkProfile(
        networkInterface: networkInterface,
        addressRange: IPv4AddressRange.Create(baseAddress, subnetMask)
      ),

      // TODO: IPv6
      AddressFamily.InterNetworkV6 => throw CreateIPv6FeatureNotImplemented(),

      _ => throw CreateNonIPAddressFamilyNotSupported(),
    };
  }

  private static uint HostToNetworkOrder(uint host)
    => unchecked((uint)IPAddress.HostToNetworkOrder((int)host));

  // ref: https://stackoverflow.com/questions/14327022/calculate-ip-range-by-subnet-mask
  private readonly struct IPv4AddressRange : IEnumerable<IPAddress> {
    public static IPv4AddressRange Create(IPAddress ipv4Address, IPAddress ipv4Mask)
    {
      var address = BinaryPrimitives.ReadUInt32BigEndian(ipv4Address.GetAddressBytes());
      var addressMask = BinaryPrimitives.ReadUInt32BigEndian(ipv4Mask.GetAddressBytes());

      return new(address, addressMask);
    }

    private readonly uint networkAddress;
    private readonly uint broadcastAddress;

    public IPv4AddressRange(uint address, uint addressMask)
    {
      networkAddress = address & addressMask;
      broadcastAddress = networkAddress + ~addressMask;
    }

    public IEnumerator<IPAddress> GetEnumerator()
    {
      if (networkAddress == broadcastAddress) {
        yield return new IPAddress(HostToNetworkOrder(networkAddress));
        yield break;
      }

      for (var hostAddress = networkAddress + 1u; hostAddress < broadcastAddress; hostAddress++) {
        yield return new IPAddress(HostToNetworkOrder(hostAddress));
      }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
  }

  private sealed class IPv4AddressRangeNetworkProfile : IPNetworkProfile {
    private readonly IPv4AddressRange addressRange;

    public IPv4AddressRangeNetworkProfile(
      NetworkInterface? networkInterface,
      IPv4AddressRange addressRange
    )
      : base(networkInterface: networkInterface)
    {
      this.addressRange = addressRange;
    }

    public override IEnumerable<IPAddress>? GetAddressRange()
      => addressRange;
  }
}
