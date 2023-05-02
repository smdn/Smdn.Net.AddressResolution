// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;
using Smdn.Net;
using Smdn.Net.AddressResolution;

// Create a IPNetworkProfile.
//
// IPNetworkProfile is used to determine the target network interface and its
// network address range when resolving an address or performing network scan.
//
// If the argument is omitted, the first available NetworkInterface is automatically selected.
var defaultNetworkProfile = IPNetworkProfile.Create();

// You can select a specific NetworkInterface by its physical address, ID or name.
// If any NetworkInterface cannot be selected, an InvalidOperationException is thrown.

// Select by network device's physical address (platform independed)
IPNetworkProfile.CreateFromNetworkInterface(physicalAddress: PhysicalAddress.Parse("00:00:5E:00:53:00"));
// Select by network device GUID (for Windows OS)
IPNetworkProfile.CreateFromNetworkInterface(id: Guid.Parse("00000000-0000-0000-0000-000000000000"));
// Select by network device ID (for Unix-like OS)
IPNetworkProfile.CreateFromNetworkInterface(id: "wlan0");
// Select by network device name (names are platform-specific)
IPNetworkProfile.CreateFromNetworkInterfaceName(name: "name");

// For more specific conditions, you can use Predicate<NetworkInterface> as follows:
var networkProfile = IPNetworkProfile.Create(
  static iface => iface.Id == "wlan0" && 100_000_000_000 <= iface.Speed
);

Console.WriteLine($"Selected NetworkInterface: {networkProfile.NetworkInterface!.Id}");

// Create a MacAddressResolver for the specific network profile.
using var resolver = new MacAddressResolver(networkProfile);

await resolver.ResolveIPAddressToMacAddressAsync(IPAddress.Parse("192.0.2.1"));
