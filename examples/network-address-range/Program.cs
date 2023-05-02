// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Smdn.Net;

// Create IPNetworkProfile with address range of 192.0.2.0/24.
var networkProfile0 = IPNetworkProfile.Create(
  baseAddress: IPAddress.Parse("192.0.2.0"),
  prefixLength: 24
);

Console.WriteLine($"{networkProfile0.GetAddressRange()!.First()}-{networkProfile0.GetAddressRange()!.Last()}");

// Create IPNetworkProfile with address range of 192.0.2.0/255.255.255.0.
var networkProfile1 = IPNetworkProfile.Create(
  baseAddress: IPAddress.Parse("192.0.2.0"),
  subnetMask: IPAddress.Parse("255.255.255.0")
);

Console.WriteLine($"{networkProfile1.GetAddressRange()!.First()}-{networkProfile1.GetAddressRange()!.Last()}");

// Create IPNetworkProfile with address range of 192.0.2.100-192.0.2.119.
// (For example, only for a range of DHCP address pools.)
static IEnumerable<IPAddress> GenerateDHCPPoolAddresses()
  // 192.0.2.100-192.0.2.119
  => Enumerable.Range(100, 20).Select(b => new IPAddress(new byte[] { 192, 0, 2, (byte)b }));

var networkProfile2 = IPNetworkProfile.Create(
  addressRangeGenerator: GenerateDHCPPoolAddresses
);

Console.WriteLine($"{networkProfile2.GetAddressRange()!.First()}-{networkProfile2.GetAddressRange()!.Last()}");

// Create IPNetworkProfile with default network interface.
// The address range is automatically determined according to the interface configurations.
var networkProfile3 = IPNetworkProfile.Create();

Console.WriteLine($"{networkProfile3.GetAddressRange()!.First()}-{networkProfile3.GetAddressRange()!.Last()} ({networkProfile3.NetworkInterface?.Id})");
