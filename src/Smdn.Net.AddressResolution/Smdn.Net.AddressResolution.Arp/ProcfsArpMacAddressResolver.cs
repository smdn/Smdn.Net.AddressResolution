// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressResolution.Arp;

internal partial class ProcfsArpMacAddressResolver : MacAddressResolver {
  private const string PathToProcNetArp = "/proc/net/arp";
  private const int ArpScanParallelMax = 3;

  public static bool IsSupported => File.Exists(PathToProcNetArp);

  internal static new ProcfsArpMacAddressResolver Create(
    MacAddressResolverOptions options,
    IServiceProvider? serviceProvider
  )
  {
    if (ProcfsArpNmapScanMacAddressResolver.IsSupported) {
      return new ProcfsArpNmapScanMacAddressResolver(
        options: options,
        logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ProcfsArpNmapScanMacAddressResolver>()
      );
    }

    return new ProcfsArpMacAddressResolver(
      options: options,
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ProcfsArpMacAddressResolver>()
    );
  }

  private readonly struct None { }

  private class ConcurrentSet<T> : ConcurrentDictionary<T, None>
    where T : notnull
  {
    public ConcurrentSet()
    {
    }

    public void Add(T key)
      => AddOrUpdate(key: key, addValue: default, updateValueFactory: static (key, old) => default);
  }

  /*
   * instance members
   */
  private DateTime lastArpFullScanAt = DateTime.MinValue;
  private readonly TimeSpan arpFullScanInterval;

  private bool HasArpFullScanIntervalElapsed => lastArpFullScanAt + arpFullScanInterval <= DateTime.Now;

  private readonly ConcurrentSet<IPAddress> invalidatedIPAddressSet = new();
  private readonly ConcurrentSet<PhysicalAddress> invalidatedMacAddressSet = new();

  public override bool HasInvalidated => !(invalidatedIPAddressSet.IsEmpty && invalidatedMacAddressSet.IsEmpty);

  private SemaphoreSlim arpFullScanMutex = new(initialCount: 1, maxCount: 1);
  private SemaphoreSlim arpPartialScanMutex = new(initialCount: ArpScanParallelMax, maxCount: ArpScanParallelMax);

  public ProcfsArpMacAddressResolver(
    MacAddressResolverOptions options,
    ILogger? logger
  )
    : base(logger)
  {
    arpFullScanInterval = options.ProcfsArpFullScanInterval;
  }

  protected override void Dispose(bool disposing)
  {
    arpFullScanMutex?.Dispose();
    arpFullScanMutex = null!;

    arpPartialScanMutex?.Dispose();
    arpPartialScanMutex = null!;

    base.Dispose(disposing);
  }

  protected override async ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(
    IPAddress ipAddress,
    CancellationToken cancellationToken
  )
  {
    if (HasArpFullScanIntervalElapsed)
      await ArpFullScanAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

    ArpTableEntry priorCandidate = default;
    ArpTableEntry candidate = default;

    await foreach (var entry in ArpTableEntry.EnumerateArpTableEntriesAsync(
      e => e.Equals(ipAddress),
      Logger,
      cancellationToken
    ).ConfigureAwait(false)) {
      if (invalidatedMacAddressSet.ContainsKey(entry.HardwareAddress!))
        continue; // ignore the entry that is marked as invalidated

      if (entry.IsPermanentOrComplete) {
        // prefer permanent or complete entry
        priorCandidate = entry;
        break;
      }

      candidate = entry; // select the last entry found
    }

    return priorCandidate.IsEmpty
      ? candidate.IsEmpty
        ? null // not found
        : candidate.HardwareAddress
      : priorCandidate.HardwareAddress;
  }

  protected override async ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(
    PhysicalAddress macAddress,
    CancellationToken cancellationToken
  )
  {
    if (HasArpFullScanIntervalElapsed)
      await ArpFullScanAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

    ArpTableEntry priorCandidate = default;
    ArpTableEntry candidate = default;

    await foreach (var entry in ArpTableEntry.EnumerateArpTableEntriesAsync(
      e => e.Equals(macAddress),
      Logger,
      cancellationToken
    ).ConfigureAwait(false)) {
      if (invalidatedIPAddressSet.ContainsKey(entry.IPAddress!))
        continue; // ignore the entry that is marked as invalidated

      if (entry.IsPermanentOrComplete) {
        // prefer permanent or complete entry
        priorCandidate = entry;
        break;
      }

      candidate = entry; // select the last entry found
    }

    return priorCandidate.IsEmpty
      ? candidate.IsEmpty
        ? null // not found
        : candidate.IPAddress
      : priorCandidate.IPAddress;
  }

  protected override void InvalidateCore(IPAddress ipAddress)
    => invalidatedIPAddressSet.Add(ipAddress);

  protected override void InvalidateCore(PhysicalAddress macAddress)
    => invalidatedMacAddressSet.Add(macAddress);

  protected override ValueTask RefreshCacheAsyncCore(
    CancellationToken cancellationToken = default
  )
    => cancellationToken.IsCancellationRequested
      ?
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
        ValueTask.FromCanceled(cancellationToken)
#else
        ValueTaskShim.FromCanceled(cancellationToken)
#endif
      : ArpFullScanAsync(cancellationToken: cancellationToken);

  private async ValueTask ArpFullScanAsync(CancellationToken cancellationToken)
  {
    if (!await arpFullScanMutex.WaitAsync(0, cancellationToken: default).ConfigureAwait(false)) {
      Logger?.LogInformation("ARP full scan is currently being performed.");
      return;
    }

    Logger?.LogDebug("Performing ARP full scan");

    try {
      await ArpFullScanAsyncCore(cancellationToken: cancellationToken).ConfigureAwait(false);

      invalidatedIPAddressSet.Clear();
      invalidatedMacAddressSet.Clear();

      lastArpFullScanAt = DateTime.Now;
    }
    finally {
      arpFullScanMutex.Release();
    }
  }

  protected virtual ValueTask ArpFullScanAsyncCore(CancellationToken cancellationToken)
  {
    Logger?.LogWarning("ARP scan is not supported in this class.");

    return default;
  }

  protected override ValueTask RefreshInvalidatedCacheAsyncCore(
    CancellationToken cancellationToken = default
  )
    => cancellationToken.IsCancellationRequested
      ?
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
        ValueTask.FromCanceled(cancellationToken)
#else
        ValueTaskShim.FromCanceled(cancellationToken)
#endif
      : ArpScanAsync(cancellationToken: cancellationToken);

  private async ValueTask ArpScanAsync(CancellationToken cancellationToken)
  {
    var invalidatedIPAddresses = invalidatedIPAddressSet.Keys;
    var invalidatedMacAddresses = invalidatedMacAddressSet.Keys;

    Logger?.LogTrace("Invalidated IP addresses: {InvalidatedIPAddresses}", string.Join(" ", invalidatedIPAddresses));
    Logger?.LogTrace("Invalidated MAC addresses: {InvalidatedMACAddresses}", string.Join(" ", invalidatedMacAddresses));

    if (invalidatedMacAddresses.Any()) {
      // perform full scan
      await ArpFullScanAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      return;
    }

    await arpPartialScanMutex.WaitAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

    try {
      Logger?.LogDebug("Performing ARP scan for invalidated targets.");

      await ArpScanAsyncCore(
        invalidatedIPAddresses: invalidatedIPAddresses,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      invalidatedIPAddressSet.Clear();
    }
    finally {
      arpPartialScanMutex.Release();
    }
  }

  protected virtual ValueTask ArpScanAsyncCore(
    IEnumerable<IPAddress> invalidatedIPAddresses,
    CancellationToken cancellationToken
  )
  {
    Logger?.LogWarning("ARP scan is not supported in this class.");

    return default;
  }
}
