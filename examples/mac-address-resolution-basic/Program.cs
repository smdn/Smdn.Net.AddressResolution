// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using Smdn.Net;
using Smdn.Net.AddressResolution;

// Create a MacAddressResolver that scopes to the network range of
// the automatically selected network interface.
using var resolver = new MacAddressResolver(IPNetworkProfile.Create());

// Resolve MAC address corresponding to the IP address.
var targetIPAddress = IPAddress.Parse("192.168.2.1");
var resolvedMacAddress = await resolver.ResolveIPAddressToMacAddressAsync(targetIPAddress);

// If address could not be resolved, the method returns null.
if (resolvedMacAddress == null)
  Console.WriteLine($"Not resolved: {targetIPAddress}");
else
  Console.WriteLine($"Resolved: {targetIPAddress} => {resolvedMacAddress.ToMacAddressString()}");
