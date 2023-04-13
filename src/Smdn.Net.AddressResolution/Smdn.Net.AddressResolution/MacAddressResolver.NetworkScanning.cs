// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressResolution;

#pragma warning disable IDE0040
partial class MacAddressResolver {
#pragma warning restore IDE0040
  protected override void InvalidateCore(IPAddress ipAddress)
    => invalidatedIPAddressSet.Add(ipAddress);

  protected override void InvalidateCore(PhysicalAddress macAddress)
    => invalidatedMacAddressSet.Add(macAddress);

  protected override ValueTask RefreshCacheAsyncCore(
    CancellationToken cancellationToken = default
  )
  {
    if (networkScanner is null)
      throw CreateCanNotPerformNetworkScanException();

    if (cancellationToken.IsCancellationRequested) {
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      return ValueTask.FromCanceled(cancellationToken);
#else
      return ValueTaskShim.FromCanceled(cancellationToken);
#endif
    }

    return FullScanAsync(cancellationToken: cancellationToken);
  }

  private async ValueTask FullScanAsync(CancellationToken cancellationToken)
  {
#if DEBUG
    if (networkScanner is null)
      throw new InvalidOperationException($"{nameof(networkScanner)} is null");
#endif

    if (!HasFullScanMinIntervalElapsed) {
      Logger?.LogInformation("Network scan was not performed since the minimum perform interval had not elapsed.");
      return;
    }

    if (!await fullScanMutex.WaitAsync(0, cancellationToken: default).ConfigureAwait(false)) {
      Logger?.LogInformation("Network scan was not performed since the another scan is currently being proceeding.");
      return;
    }

    Logger?.LogInformation("Performing network scan.");

    var sw = Logger is null ? null : Stopwatch.StartNew();

    try {
      await networkScanner!.ScanAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      invalidatedIPAddressSet.Clear();
      invalidatedMacAddressSet.Clear();

      timeStampForFullScan ??= new Stopwatch();
      timeStampForFullScan.Restart();
    }
    finally {
      Logger?.LogInformation("Network scan finished in {ElapsedMilliseconds} ms.", sw!.ElapsedMilliseconds);

      fullScanMutex.Release();
    }
  }

  protected override ValueTask RefreshInvalidatedCacheAsyncCore(
    CancellationToken cancellationToken = default
  )
  {
    if (networkScanner is null)
      throw CreateCanNotPerformNetworkScanException();

    if (cancellationToken.IsCancellationRequested) {
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      return ValueTask.FromCanceled(cancellationToken);
#else
      return ValueTaskShim.FromCanceled(cancellationToken);
#endif
    }

    return PartialScanAsync(cancellationToken: cancellationToken);
  }

  private async ValueTask PartialScanAsync(CancellationToken cancellationToken)
  {
#if DEBUG
    if (networkScanner is null)
      throw new InvalidOperationException($"{nameof(networkScanner)} is null");
#endif

    if (invalidatedIPAddressSet.IsEmpty && invalidatedMacAddressSet.IsEmpty)
      return; // nothing to do

    var invalidatedIPAddresses = invalidatedIPAddressSet.Keys;
    var invalidatedMacAddresses = invalidatedMacAddressSet.Keys;

    Logger?.LogTrace("Invalidated IP addresses: {InvalidatedIPAddresses}", string.Join(" ", invalidatedIPAddresses));
    Logger?.LogTrace("Invalidated MAC addresses: {InvalidatedMACAddresses}", string.Join(" ", invalidatedMacAddresses));

    if (!invalidatedMacAddressSet.IsEmpty) {
      // perform full scan since MAC addresses must be refreshed
      await FullScanAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      return;
    }

    await partialScanSemaphore.WaitAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

    var sw = Logger is null ? null : Stopwatch.StartNew();

    try {
      Logger?.LogInformation("Performing network scan for the invalidated {Count} IP addresses.", invalidatedIPAddresses.Count);

      await networkScanner!.ScanAsync(
        addresses: invalidatedIPAddresses,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      invalidatedIPAddressSet.Clear();
    }
    finally {
      Logger?.LogInformation("Network scan finished in {ElapsedMilliseconds} ms.", sw!.ElapsedMilliseconds);

      partialScanSemaphore.Release();
    }
  }
}
