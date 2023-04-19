[![GitHub license](https://img.shields.io/github/license/smdn/Smdn.Net.AddressResolution)](https://github.com/smdn/Smdn.Net.AddressResolution/blob/main/LICENSE.txt)
[![tests/main](https://img.shields.io/github/actions/workflow/status/smdn/Smdn.Net.AddressResolution/test.yml?branch=main&label=tests%2Fmain)](https://github.com/smdn/Smdn.Net.AddressResolution/actions/workflows/test.yml)
[![CodeQL](https://github.com/smdn/Smdn.Net.AddressResolution/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/smdn/Smdn.Net.AddressResolution/actions/workflows/codeql-analysis.yml)
[![NuGet](https://img.shields.io/nuget/v/Smdn.Net.AddressResolution.svg)](https://www.nuget.org/packages/Smdn.Net.AddressResolution/)

# Smdn.Net.AddressResolution
`Smdn.Net.AddressResolution` provides APIs for resolving between IP addresses and MAC addresses, mainly through the `MacAddressResolver` class.

[MacAddressResolver](src/Smdn.Net.AddressResolution/Smdn.Net.AddressResolution/) class performs address resolution by referencing the system's address table (e.g., ARP table). Address tables are referenced via the `IAddressTable` interface and its implementations ([Smdn.Net.AddressTables](src/Smdn.Net.AddressResolution/Smdn.Net.AddressTables/) namespace). `MacAddressResolver` resolves to the appropriate address based on the address cache retrieved from the address table and its cache status.

This library also provides network scanning APIs. These APIs can be used to update the address table before performing address resolution or in cases when the address is unresolvable, unreachable, etc. Network scanning can be performed via the the `INetworkScanner` interface using with the `MacAddressResolver` class.

The specific implementation of `INetworkScanner` invokes functions for each platform, such as `nmap` or `arp-scan` command ([Smdn.Net.NetworkScanning](src/Smdn.Net.AddressResolution/Smdn.Net.NetworkScanning/) namespace). If these implementation are not available, a fallback implementation can be used that sends ping using the [Ping](https://learn.microsoft.com/dotnet/api/system.net.networkinformation.ping) class.

# Usage
First, add `<PackageReference>` to the project file.

```xml
  <ItemGroup>
    <PackageReference Include="Smdn.Net.AddressResolution" Version="1.*" />
  </ItemGroup>
```

Then write the code like below. This code resolves MAC address corresponding to the IP address.

```cs
using System;
using System.Net;
using Smdn.Net;
using Smdn.Net.AddressResolution;

using var resolver = new MacAddressResolver(IPNetworkProfile.Create());

// Resolve MAC address corresponding to the IP address.
var targetIPAddress = IPAddress.Parse("192.168.2.1");
var resolvedMacAddress = await resolver.ResolveIPAddressToMacAddressAsync(targetIPAddress);
```

For a more detailed example, see [this example](examples/mac-address-resolution-basic/).

`IPNetworkProfile` specifies the network interface and address range for the address resolution. If no parameter is specified, the first available network interface found is used by default. For more information on this feature, see [this example](examples/mac-address-resolution-networkprofile/) or [this example](examples/network-address-range/).

If there is no corresponding entry in the address table, `null` is returned as the result of address resolution. The address table can be updated by performing a network scan prior to address resolution. For more information about network scanning, see [this example](examples/network-scanning/).

See the projects in the [examples](./examples/) directory for more usages.



# Implementations and limitations
As of version 1.0.0.

## IPv4/IPv6
IPv4 is supported and well tested.

IPv6 can be used on the API, but due to lack of implementation, exceptions such as `NotImplementedException` or `NotSupportedException` may be thrown.

## Address table (ARP cache, neighbor cache)
|OS|API|Status|
|--|---|------|
|Windows|IP Helper API(`IPHLPAPI`)|Supported|
|Linux|`/proc/net/arp`|Supported|
|Linux|`/proc/net/ipv6_route`|Planning|
|macOS|-|Planning|

Operations that require administrator privileges, such as flushing the ARP cache or adding/deleting static entries, are not supported and will not be supported.

These implementations are in the [Smdn.Net.AddressTables](src/Smdn.Net.AddressResolution/Smdn.Net.AddressTables/) namespace.

## Network scaning
|OS|API/Command|Status|Note|
|-|-|-|-|
|Windows|IP Helper API(`IPHLPAPI`)|Supported|-|
|(any)|`nmap` command|Supported|Recommended|
|(any)|`arp-scan` command|Supported|Requires administrator privileges|
|(any)|[Ping](https://learn.microsoft.com/en-us/dotnet/api/system.net.networkinformation.ping) class<br/>(`ping` command)|Supported|Default fallback implementation|

To perform a network scan using the commands above, the command must be installed on your system and the `PATH` environment variable must be set.

These implementations are in the [Smdn.Net.NetworkScanning](src/Smdn.Net.AddressResolution/Smdn.Net.NetworkScanning/) namespace.

# For contributers
Contributions are appreciated!

If there's a feature you would like to add or a bug you would like to fix, please read [Contribution guidelines](./CONTRIBUTING.md) and create an Issue or Pull Request.

IssueやPull Requestを送る際は、[Contribution guidelines](./CONTRIBUTING.md)をご覧頂ください。　可能なら英語が望ましいですが、日本語で構いません。　

# Notice
This project is licensed under the terms of the [MIT License](./LICENSE.txt).

This project uses the following components. See [ThirdPartyNotices.md](./ThirdPartyNotices.md) for detail.

- [Vanara.PInvoke.IpHlpApi](https://www.nuget.org/packages/Vanara.PInvoke.IpHlpApi) ([https://github.com/dahall/vanara](https://github.com/dahall/vanara))
