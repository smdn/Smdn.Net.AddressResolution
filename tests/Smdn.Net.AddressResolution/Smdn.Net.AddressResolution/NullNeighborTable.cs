// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

internal sealed class NullNeighborTable : INeighborTable {
  public NullNeighborTable()
  {
  }

  public void Dispose()
  {
  }

  public IAsyncEnumerable<NeighborTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken)
    => Array.Empty<NeighborTableEntry>().ToAsyncEnumerable();
}
