// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

namespace Smdn.Net.AddressResolution;

public sealed class MacAddressResolverOptions {
  public static readonly MacAddressResolverOptions Default = new() { };

  /// <summary>
  /// Gets the network profile which specifying the network interface and target addresses.
  /// This is used as necessary for neighbor search in address resolution.
  /// </summary>
  public IPNetworkProfile? NetworkProfile { get; init; }

  public TimeSpan NeighborDiscoveryInterval { get; init; } = TimeSpan.FromMinutes(15.0);
}
