// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using Smdn.Net;
using Smdn.Net.AddressResolution;

// Create a IPNetworkProfile.
//
// IPNetworkProfile is used to determine the target network interface and its
// network address range when resolving an address or performing network scan.
//
// If the argument is omitted, the first available NetworkInterface is automatically selected.
var defaultNetworkProfile = IPNetworkProfile.Create();

// You can select NetworkInterface by using Predicate<NetworkInterface> as follows:
var networkProfile = IPNetworkProfile.Create(
  static iface =>
    iface.Id == "wlan0" // Select by network device ID (for Unix-like OS)
    // iface.Id == "{00000000-0000-0000-0000-000000000000}" // Select by network device GUID (for Windows)
);

// If a network interface cannot be selected, an InvalidOperationException is thrown.

Console.WriteLine($"Selected NetworkInterface: {networkProfile.NetworkInterface!.Id}");

// Create a MacAddressResolver for the specific network profile.
using var resolver = new MacAddressResolver(networkProfile);

await resolver.ResolveIPAddressToMacAddressAsync(IPAddress.Parse("192.168.2.1"));
