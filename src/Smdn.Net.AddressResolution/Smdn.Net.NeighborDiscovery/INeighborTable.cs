// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Threading;

namespace Smdn.Net.NeighborDiscovery;

/// <summary>
/// Provides a mechanism for referencing address tables such as ARP table.
/// </summary>
public interface INeighborTable : IDisposable {
  /// <summary>
  /// Refers to and enumerates the list of entries from the address table.
  /// </summary>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  IAsyncEnumerable<NeighborTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken);
}
