// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

internal sealed class StaticNeighborTable : INeighborTable {
  private readonly IList<NeighborTableEntry> staticEntries;

  public StaticNeighborTable(IList<NeighborTableEntry> staticEntries)
  {
    this.staticEntries = staticEntries;
  }

  public void Dispose()
  {
  }

  public IAsyncEnumerable<NeighborTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken)
    => staticEntries.ToAsyncEnumerable();
}
