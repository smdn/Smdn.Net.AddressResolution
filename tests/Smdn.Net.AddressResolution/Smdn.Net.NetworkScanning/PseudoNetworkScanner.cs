// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.NetworkScanning;

internal sealed class PseudoNetworkScanner : INetworkScanner {
  public bool IsDisposed { get; private set; }
  public bool FullScanRequested { get; private set; }
  public bool PartialScanRequested { get; private set; }
  public List<IPAddress> ScanRequestedAddresses { get; } = new();

  public void Dispose() => IsDisposed = true;

  public void Reset()
  {
    FullScanRequested = false;
    PartialScanRequested = false;
    ScanRequestedAddresses.Clear();
  }

  public ValueTask ScanAsync(CancellationToken cancellationToken)
  {
    FullScanRequested = true;

    return default;
  }

  public ValueTask ScanAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken)
  {
    PartialScanRequested = true;
    ScanRequestedAddresses.AddRange(addresses);

    return default;
  }
}
