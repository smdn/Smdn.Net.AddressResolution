// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.AddressResolution;

public sealed class MacAddressResolverOptions {
  public static readonly MacAddressResolverOptions Default = new() { };

  /// <summary>
  /// Gets the string value passed to the argument &lt;target specification&gt; of nmap command.
  /// </summary>
  public string? NmapTargetSpecification { get; init; }

  public TimeSpan ProcfsArpFullScanInterval { get; init; } = TimeSpan.FromMinutes(15.0);
}
