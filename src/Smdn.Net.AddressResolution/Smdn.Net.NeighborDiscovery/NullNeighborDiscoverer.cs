// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.NeighborDiscovery;

public sealed class NullNeighborDiscoverer : INeighborDiscoverer {
  public static readonly NullNeighborDiscoverer Instance = new();

  private NullNeighborDiscoverer()
  {
  }

  public ValueTask DiscoverAsync(
    CancellationToken cancellationToken = default
  ) => default;

  public ValueTask DiscoverAsync(
    IEnumerable<IPAddress> addresses,
    CancellationToken cancellationToken = default
  ) => default;

  void IDisposable.Dispose()
  {
    // nothing to do
  }
}
