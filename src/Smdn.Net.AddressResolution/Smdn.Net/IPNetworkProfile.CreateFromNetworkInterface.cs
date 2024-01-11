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

  /// <summary>
  /// Creates an <see cref="IPNetworkProfile"/> using the first <see cref="System.Net.NetworkInformation.NetworkInterface"/> found.
  /// </summary>
  /// <exception cref="InvalidOperationException">The appropriate <see cref="System.Net.NetworkInformation.NetworkInterface"/> could not be selected.</exception>
  public static IPNetworkProfile Create()
    => Create(predicateForNetworkInterface: static _ => true /* select first one */);

  /// <summary>
  /// Creates an <see cref="IPNetworkProfile"/> using <see cref="System.Net.NetworkInformation.NetworkInterface"/>
  /// which has GUID specified by <paramref name="id"/>.
  /// </summary>
  /// <param name="id">A <see cref="Guid"/> for selecting a specific network interface.</param>
  /// <exception cref="InvalidOperationException">The appropriate <see cref="System.Net.NetworkInformation.NetworkInterface"/> could not be selected.</exception>
  public static IPNetworkProfile CreateFromNetworkInterface(Guid id)
    // On Windows OS, GUID in 'B' format is used for the network interface ID.
    => CreateFromNetworkInterface(id: id.ToString("B", provider: null));

  /// <summary>
  /// Creates an <see cref="IPNetworkProfile"/> using <see cref="System.Net.NetworkInformation.NetworkInterface"/>
  /// which has ID specified by <paramref name="id"/>.
  /// </summary>
  /// <param name="id">An ID for selecting a specific network interface.</param>
  /// <exception cref="InvalidOperationException">The appropriate <see cref="System.Net.NetworkInformation.NetworkInterface"/> could not be selected.</exception>
  public static IPNetworkProfile CreateFromNetworkInterface(string id)
  {
    if (id is null)
      throw new ArgumentNullException(nameof(id));

    return Create(predicateForNetworkInterface: iface => string.Equals(iface.Id, id, NetworkInterfaceIdComparer.Comparison));
  }

  /// <summary>
  /// Creates an <see cref="IPNetworkProfile"/> using <see cref="System.Net.NetworkInformation.NetworkInterface"/>
  /// whose physical address equals to <paramref name="physicalAddress"/>.
  /// </summary>
  /// <param name="physicalAddress">A <see cref="PhysicalAddress"/> for selecting a specific network interface.</param>
  /// <exception cref="InvalidOperationException">The appropriate <see cref="System.Net.NetworkInformation.NetworkInterface"/> could not be selected.</exception>
  public static IPNetworkProfile CreateFromNetworkInterface(PhysicalAddress physicalAddress)
  {
    if (physicalAddress is null)
      throw new ArgumentNullException(nameof(physicalAddress));

    return Create(predicateForNetworkInterface: iface => physicalAddress.Equals(iface.GetPhysicalAddress()));
  }

  /// <summary>
  /// Creates an <see cref="IPNetworkProfile"/> using <see cref="System.Net.NetworkInformation.NetworkInterface"/>
  /// which has the name specified by <paramref name="name"/>.
  /// </summary>
  /// <param name="name">A name for selecting a specific network interface.</param>
  /// <exception cref="InvalidOperationException">The appropriate <see cref="System.Net.NetworkInformation.NetworkInterface"/> could not be selected.</exception>
  public static IPNetworkProfile CreateFromNetworkInterfaceName(string name)
  {
    if (name is null)
      throw new ArgumentNullException(nameof(name));

    return Create(predicateForNetworkInterface: iface => string.Equals(iface.Name, name, StringComparison.Ordinal));
  }

  /// <summary>
  /// Creates an <see cref="IPNetworkProfile"/> using <see cref="System.Net.NetworkInformation.NetworkInterface"/>
  /// which specified by <paramref name="predicateForNetworkInterface"/>.
  /// </summary>
  /// <param name="predicateForNetworkInterface">A <see cref="Predicate{NetworkInterface}"/> for selecting a specific network interface.</param>
  /// <exception cref="InvalidOperationException">
  /// The appropriate <see cref="System.Net.NetworkInformation.NetworkInterface"/> could not be selected.
  /// Or <paramref name="predicateForNetworkInterface"/> threw an exception. See <see cref="Exception.InnerException"/> for the actual exception thrown.
  /// </exception>
  public static IPNetworkProfile Create(Predicate<NetworkInterface> predicateForNetworkInterface)
  {
    if (predicateForNetworkInterface is null)
      throw new ArgumentNullException(nameof(predicateForNetworkInterface));

    foreach (var iface in NetworkInterface.GetAllNetworkInterfaces()) {
      if (!(iface.Supports(NetworkInterfaceComponent.IPv4) || iface.Supports(NetworkInterfaceComponent.IPv6)))
        continue; // except interfaces that does not suport IPv4/IPv6
      if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
        continue; // except loopback interfaces
      if (iface.OperationalStatus != OperationalStatus.Up)
        continue; // except inoperational interfaces

      try {
        if (predicateForNetworkInterface(iface))
          return Create(iface);
      }
      catch (Exception ex) {
        throw new InvalidOperationException(
          $"{nameof(predicateForNetworkInterface)} threw an exception for network interface '{iface.Name}'.",
          innerException: ex
        );
      }
    }

    throw new InvalidOperationException("The appropriate network interface was not selected.");
  }

  /// <summary>
  /// Creates an <see cref="IPNetworkProfile"/> using <see cref="System.Net.NetworkInformation.NetworkInterface"/>.
  /// </summary>
  /// <param name="networkInterface">A <see cref="System.Net.NetworkInformation.NetworkInterface"/> describing the network used by <see cref="IPNetworkProfile"/>.</param>
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
