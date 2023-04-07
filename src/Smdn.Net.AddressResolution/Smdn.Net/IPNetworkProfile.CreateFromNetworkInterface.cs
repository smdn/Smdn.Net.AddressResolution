// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Smdn.Net;

#pragma warning disable IDE0040
partial class IPNetworkProfile {
#pragma warning restore IDE0040

  public static IPNetworkProfile Create()
    => Create(predicate: static _ => true /* select first one */);

  public static IPNetworkProfile Create(Predicate<NetworkInterface> predicate)
  {
    if (predicate is null)
      throw new ArgumentNullException(nameof(predicate));

    foreach (var iface in NetworkInterface.GetAllNetworkInterfaces()) {
      if (!(iface.Supports(NetworkInterfaceComponent.IPv4) || iface.Supports(NetworkInterfaceComponent.IPv6)))
        continue; // except interfaces that does not suport IPv4/IPv6
      if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
        continue; // except loopback interfaces
      if (iface.OperationalStatus != OperationalStatus.Up)
        continue; // except inoperational interfaces

      if (predicate(iface))
        return Create(iface);
    }

    throw new InvalidOperationException("The appropriate network interface was not selected.");
  }

  public static IPNetworkProfile Create(NetworkInterface networkInterface)
  {
    if (networkInterface is null)
      throw new ArgumentNullException(nameof(networkInterface));

    var ipProperties = networkInterface.GetIPProperties();

    // prefer IPv4 address
    foreach (
      var ipv4unicastAddress in ipProperties
        .UnicastAddresses
        .Where(static addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
    ) {
      return new IPv4AddressRangeNetworkProfile(
        networkInterface: networkInterface,
        addressRange: IPv4AddressRange.Create(ipv4unicastAddress.Address, ipv4unicastAddress.IPv4Mask)
      );
    }

    foreach (
      var ipv6unicastAddress in ipProperties
        .UnicastAddresses
        .Where(static addr => addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
    ) {
      // TODO: IPv6
      throw CreateIPv6FeatureNotImplemented();
    }

    throw CreateNonIPAddressFamilyNotSupported();
  }
}
