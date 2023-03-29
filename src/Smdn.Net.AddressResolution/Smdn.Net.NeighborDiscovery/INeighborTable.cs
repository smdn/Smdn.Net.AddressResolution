// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Smdn.Net.NeighborDiscovery;

public interface INeighborTable {
  IAsyncEnumerable<NeighborTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken);
}
