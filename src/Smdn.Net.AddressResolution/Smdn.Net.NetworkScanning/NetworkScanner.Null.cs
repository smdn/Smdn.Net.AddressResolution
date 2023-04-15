// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.NetworkScanning;

#pragma warning disable IDE0040
partial class NetworkScanner {
#pragma warning restore IDE0040
  public static INetworkScanner Null { get; } = new NullNetworkScanner();

  private sealed class NullNetworkScanner : INetworkScanner {
    internal NullNetworkScanner()
    {
    }

    void IDisposable.Dispose()
    {
      // do nothing
    }

    public ValueTask ScanAsync(CancellationToken cancellationToken)
    {
      if (cancellationToken.IsCancellationRequested) {
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
        return ValueTask.FromCanceled(cancellationToken);
#else
        return ValueTaskShim.FromCanceled(cancellationToken);
#endif
      }

      return default; // do nothing
    }

    public ValueTask ScanAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken)
    {
      if (addresses is null)
        throw new ArgumentNullException(nameof(addresses));

      if (cancellationToken.IsCancellationRequested) {
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
        return ValueTask.FromCanceled(cancellationToken);
#else
        return ValueTaskShim.FromCanceled(cancellationToken);
#endif
      }

      return default; // do nothing
    }
  }
}
