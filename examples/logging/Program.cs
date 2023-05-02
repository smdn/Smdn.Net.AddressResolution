// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smdn.Net;
using Smdn.Net.AddressResolution;

var services = new ServiceCollection();

services
  .AddLogging(
    static builder => builder
      .AddSimpleConsole(static options => options.SingleLine = true)
      .AddFilter(static level => LogLevel.Trace <= level)
  );

using var resolver = new MacAddressResolver(
  IPNetworkProfile.Create(),
  services.BuildServiceProvider()
);

await resolver.ResolveIPAddressToMacAddressAsync(IPAddress.Parse("192.0.2.1"));
