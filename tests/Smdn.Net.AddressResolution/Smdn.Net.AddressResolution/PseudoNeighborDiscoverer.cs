// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

internal sealed class PseudoNeighborDiscoverer : INeighborDiscoverer {
  public bool IsDisposed { get; private set; }
  public bool FullDiscoveryRequested { get; private set; }
  public bool PartialDiscoveryRequested { get; private set; }
  public List<IPAddress> DiscoveryRequestedAddresses { get; } = new();

  public void Dispose() => IsDisposed = true;

  public void Reset()
  {
    FullDiscoveryRequested = false;
    PartialDiscoveryRequested = false;
    DiscoveryRequestedAddresses.Clear();
  }

  public ValueTask DiscoverAsync(CancellationToken cancellationToken)
  {
    FullDiscoveryRequested = true;

    return default;
  }

  public ValueTask DiscoverAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken)
  {
    PartialDiscoveryRequested = true;
    DiscoveryRequestedAddresses.AddRange(addresses);

    return default;
  }
}
