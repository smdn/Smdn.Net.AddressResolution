// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

internal sealed class NullAddressTable : IAddressTable {
  public NullAddressTable()
  {
  }

  public void Dispose()
  {
  }

  public IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken)
    => Array.Empty<AddressTableEntry>().ToAsyncEnumerable();
}
