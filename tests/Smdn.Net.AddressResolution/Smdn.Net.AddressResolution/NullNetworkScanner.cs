// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

internal sealed class NullNetworkScanner : INetworkScanner {
  public void Dispose()
  {
  }

  public ValueTask ScanAsync(CancellationToken cancellationToken)
    => default;

  public ValueTask ScanAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken)
    => default;
}
