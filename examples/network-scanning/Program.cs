// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smdn.Net;
using Smdn.Net.AddressResolution;

var services = new ServiceCollection();

// Configure console logger.
services
  .AddLogging(
    static builder => builder
      .AddSimpleConsole(static options => options.SingleLine = true)
      .AddFilter(static level => LogLevel.Debug <= level)
  );

// Create IPNetworkProfile with address range of 192.0.2.100-192.0.2.119.
// When MacAddressResolver performs a network scan, this address range will be scanned.
static IEnumerable<IPAddress> GenerateDHCPPoolAddresses()
  => Enumerable.Range(100, 20).Select(n => IPAddress.Parse($"192.0.2.{n}"));

var networkProfile = IPNetworkProfile.Create(
  addressRangeGenerator: GenerateDHCPPoolAddresses
);

// Create a MacAddressResolver.
using var resolver = new MacAddressResolver(
  networkProfile,
  services.BuildServiceProvider()
);

// Resolve IP address to MAC address.
var targetIPAddress = IPAddress.Parse("192.0.2.105");

var resolvedAddress = await resolver.ResolveIPAddressToMacAddressAsync(targetIPAddress);

// Do something with the resolved address.
Console.WriteLine($"Resolved address: {resolvedAddress?.ToMacAddressString()}");

// At this time, if the address is unresolvable, unreachable or expired,
// you can mark the address as 'invalid'.
if (resolvedAddress == null) {
  resolver.Invalidate(IPAddress.Parse("192.0.2.105"));
}

// After that, a call to RefreshInvalidatedAddressesAsync will perform a network scan
// target to the invalidated addresses to update the address table cache.
if (resolver.HasInvalidated) {
  await resolver.RefreshInvalidatedAddressesAsync();
}

// By resolving again, you may be able to get the updated address.
resolvedAddress = await resolver.ResolveIPAddressToMacAddressAsync(targetIPAddress);

Console.WriteLine($"Resolved address: {resolvedAddress?.ToMacAddressString()}");

// Even without explicitly calling RefreshInvalidatedAddressesAsync, a network scan can be
// performed automatically before address resolution by calling
// ResolveIPAddressToMacAddressAsync or ResolveMacAddressToIPAddressAsync.
//
// The interval for performing automatic network scan (updating address table cache)
// can be configure with the following properties.

// If more than 10 minutes have elapsed since the latest scan, scan before address resolution.
resolver.NetworkScanInterval = TimeSpan.FromMinutes(10);

// If it is necessary to scan the entire network (full scan),
// skip scanning if it's been less than 30 seconds since the latest scan.
resolver.NetworkScanMinInterval = TimeSpan.FromSeconds(30);

// RefreshAddressTableAsync can be used to scan for the entire address range.
// This can be used, for example, when you want to update the address
// table at startup time or before performing address resolution.
await resolver.RefreshAddressTableAsync();
