// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

#pragma warning disable IDE0040
partial class MacAddressResolver {
#pragma warning restore IDE0040
  protected override async ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(
    IPAddress ipAddress,
    CancellationToken cancellationToken
  )
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (ShouldPerformFullScanBeforeResolution)
      await RefreshCacheAsyncCore(cancellationToken: cancellationToken).ConfigureAwait(false);

    cancellationToken.ThrowIfCancellationRequested();

    var selectedEntry = await SelectNeighborTableEntryAsync(
      predicate: entry => {
        if (!FilterNeighborTableEntryForAddressResolution(entry))
          return false;

        if (!entry.Equals(ipAddress))
          return false;

        // ignore the entry that is marked as invalidated
        if (invalidatedMacAddressSet.ContainsKey(entry.PhysicalAddress!)) {
          Logger?.LogDebug("Invalidated: {Entry}", entry);
          return false;
        }

        return true;
      },
      cancellationToken
    ).ConfigureAwait(false);

    return selectedEntry.PhysicalAddress;
  }

  protected override async ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(
    PhysicalAddress macAddress,
    CancellationToken cancellationToken
  )
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (ShouldPerformFullScanBeforeResolution)
      await RefreshCacheAsyncCore(cancellationToken: cancellationToken).ConfigureAwait(false);

    cancellationToken.ThrowIfCancellationRequested();

    var selectedEntry = await SelectNeighborTableEntryAsync(
      predicate: entry => {
        if (!FilterNeighborTableEntryForAddressResolution(entry))
          return false;

        if (!entry.Equals(macAddress))
          return false;

        // ignore the entry that is marked as invalidated
        if (invalidatedIPAddressSet.ContainsKey(entry.IPAddress!)) {
          Logger?.LogDebug("Invalidated: {Entry}", entry);
          return false;
        }

        return true;
      },
      cancellationToken
    ).ConfigureAwait(false);

    return selectedEntry.IPAddress;
  }

  protected virtual async ValueTask<NeighborTableEntry> SelectNeighborTableEntryAsync(
    Predicate<NeighborTableEntry> predicate,
    CancellationToken cancellationToken
  )
  {
    NeighborTableEntry priorCandidate = default;
    NeighborTableEntry candidate = default;

    await foreach (var entry in EnumerateNeighborTableEntriesAsyncCore(
      predicate: predicate,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false)) {
      if (entry.IsPermanent || entry.State == NeighborTableEntryState.Reachable) {
        // prefer permanent or reachable entry
        priorCandidate = entry;
        break;
      }

      candidate = entry; // select the last entry found

      Logger?.LogTrace("Candidate: {Entry}", candidate);
    }

    if (!priorCandidate.IsEmpty)
      candidate = priorCandidate;

    Logger?.LogDebug("Resolved: {Entry}", candidate.IsEmpty ? "(none)" : candidate.ToString());

    return candidate;
  }
}
