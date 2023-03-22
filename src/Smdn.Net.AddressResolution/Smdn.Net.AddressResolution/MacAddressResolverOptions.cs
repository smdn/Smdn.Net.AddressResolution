// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.AddressResolution;

public sealed class MacAddressResolverOptions {
  public static readonly MacAddressResolverOptions Default = new() { };

  /// <summary>
  /// Gets the string value passed to the argument '-e &lt;iface&gt;' of nmap command.
  /// </summary>
  public string? NmapCommandInterfaceSpecification { get; init; }

  /// <summary>
  /// Gets the string value passed to the argument &lt;target specification&gt; of nmap command.
  /// </summary>
  public string? NmapCommandTargetSpecification { get; init; }

  /// <summary>
  /// Gets the string value passed to the argument '--interface=&lt;s&gg;' of arp-scan command.
  /// </summary>
  public string? ArpScanCommandInterfaceSpecification { get; init; }

  /// <summary>
  /// Gets the string value represents the 'target hosts' pass to the arp-scan command. This value can be IP addresses or hostnames.
  /// </summary>
  public string? ArpScanCommandTargetSpecification { get; init; }

  public TimeSpan ProcfsArpFullScanInterval { get; init; } = TimeSpan.FromMinutes(15.0);
}
