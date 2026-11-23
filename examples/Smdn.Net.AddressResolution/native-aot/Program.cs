// SPDX-FileCopyrightText: 2026 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;

using Smdn.Net;
using Smdn.Net.AddressResolution;

using var resolver = new MacAddressResolver(IPNetworkProfile.Create());

var targetIPAddress = IPAddress.Parse("192.0.2.1");
var resolvedMacAddress = await resolver.ResolveIPAddressToMacAddressAsync(targetIPAddress);

if (resolvedMacAddress == null)
  Console.WriteLine($"Not resolved: {targetIPAddress}");
else
  Console.WriteLine($"Resolved: {targetIPAddress} => {resolvedMacAddress.ToMacAddressString()}");
