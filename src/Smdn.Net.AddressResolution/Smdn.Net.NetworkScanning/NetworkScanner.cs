// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.NetworkScanning;

/// <summary>
/// Provides a mechanism for performing network scan.
/// </summary>
public abstract partial class NetworkScanner : INetworkScanner {
  private IPNetworkProfile networkProfile; // if null, it indicates a 'disposed' state.

  protected IPNetworkProfile NetworkProfile {
    get {
      ThrowIfDisposed();

      return networkProfile;
    }
  }

  protected ILogger? Logger { get; }

  protected NetworkScanner(
    IPNetworkProfile networkProfile,
    ILogger? logger = null
  )
  {
    this.networkProfile = networkProfile ?? throw new ArgumentNullException(nameof(networkProfile));
    Logger = logger;
  }

  /// <inheritdoc cref="IDisposable.Dispose" />
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    networkProfile = null!;
  }

  protected void ThrowIfDisposed()
  {
    if (networkProfile is null)
      throw new ObjectDisposedException(GetType().FullName);
  }

  /// <summary>
  /// Performs network scan for all targets.
  /// </summary>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  public virtual ValueTask ScanAsync(
    CancellationToken cancellationToken = default
  )
    => ScanAsync(
      addresses: NetworkProfile.GetAddressRange()
        ?? throw new InvalidOperationException($"could not get address range from current {nameof(IPNetworkProfile)}"),
      cancellationToken: cancellationToken
    );

  /// <summary>
  /// Performs network scan for the targets specified by <paramref name="addresses"/>.
  /// </summary>
  /// <param name="addresses">The target addresses to perform network scan.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  public virtual ValueTask ScanAsync(
    IEnumerable<IPAddress> addresses,
    CancellationToken cancellationToken = default
  )
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

    ThrowIfDisposed();

    return Core();

    async ValueTask Core()
    {
      foreach (var address in addresses) {
        cancellationToken.ThrowIfCancellationRequested();

        await ScanAsyncCore(address, cancellationToken);
      }
    }
  }

  /// <summary>
  /// Performs network scan for the single target specified by <paramref name="address"/>.
  /// </summary>
  /// <param name="address">The target address to perform network scan.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  protected virtual ValueTask ScanAsyncCore(IPAddress address, CancellationToken cancellationToken)
    => default;
}
