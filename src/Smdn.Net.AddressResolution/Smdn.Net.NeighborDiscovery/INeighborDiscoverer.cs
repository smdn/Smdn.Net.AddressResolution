// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.NeighborDiscovery;

public interface INeighborDiscoverer : IDisposable {
  ValueTask DiscoverAsync(CancellationToken cancellationToken);
  ValueTask DiscoverAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken);
}
