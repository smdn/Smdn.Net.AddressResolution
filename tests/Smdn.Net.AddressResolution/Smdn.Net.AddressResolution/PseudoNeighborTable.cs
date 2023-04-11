// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

internal sealed class PseudoNeighborTable : INeighborTable {
  public bool IsDisposed { get; private set; }

  public void Dispose() => IsDisposed = true;

  public IAsyncEnumerable<NeighborTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken)
    => throw new NotImplementedException();
}