// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.NeighborDiscovery;

/// <summary>
/// Provides a mechanism for performing neighbor discovery or network scan.
/// </summary>
public interface INeighborDiscoverer : IDisposable {
  /// <summary>
  /// Performs neighbor discovery or network scan for all targets.
  /// </summary>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  ValueTask DiscoverAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Performs neighbor discovery or network scan for the targets specified by <paramref name="addresses"/>.
  /// </summary>
  /// <param name="addresses">The target addresses to perform neighbor discovery.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  ValueTask DiscoverAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken);
}
