// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution.Arp;

internal partial class ProcfsArpMacAddressResolver : MacAddressResolver {
  private const int ArpScanParallelMax = 3;

  public static bool IsSupported => ProcfsArpNeighborTable.IsSupported;

  internal static new ProcfsArpMacAddressResolver Create(
    MacAddressResolverOptions options,
    IServiceProvider? serviceProvider
  )
  {
    if (ProcfsArpWithArpScanCommandMacAddressResolver.IsSupported) {
      return new ProcfsArpWithArpScanCommandMacAddressResolver(
        options: options,
        serviceProvider: serviceProvider
      );
    }

    if (ProcfsArpWithNmapCommandMacAddressResolver.IsSupported) {
      return new ProcfsArpWithNmapCommandMacAddressResolver(
        options: options,
        serviceProvider: serviceProvider
      );
    }

    return new ProcfsArpMacAddressResolver(
      options: options,
      serviceProvider: serviceProvider
    );
  }

  private readonly struct None { }

  private sealed class ConcurrentSet<T> : ConcurrentDictionary<T, None>
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
  private readonly INeighborTable neighborTable;
  private readonly INeighborDiscoverer neighborDiscoverer;
  private DateTime lastArpFullScanAt = DateTime.MinValue;
  private readonly TimeSpan arpFullScanInterval;

  private bool HasArpFullScanIntervalElapsed =>
    arpFullScanInterval != Timeout.InfiniteTimeSpan &&
    lastArpFullScanAt + arpFullScanInterval <= DateTime.Now;

  private readonly ConcurrentSet<IPAddress> invalidatedIPAddressSet = new();
  private readonly ConcurrentSet<PhysicalAddress> invalidatedMacAddressSet = new();

  public override bool HasInvalidated => !(invalidatedIPAddressSet.IsEmpty && invalidatedMacAddressSet.IsEmpty);

  private SemaphoreSlim arpFullScanMutex = new(initialCount: 1, maxCount: 1);
  private SemaphoreSlim arpPartialScanMutex = new(initialCount: ArpScanParallelMax, maxCount: ArpScanParallelMax);

  public ProcfsArpMacAddressResolver(
    MacAddressResolverOptions options,
    IServiceProvider? serviceProvider
  )
    : this(
      options: options,
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ProcfsArpMacAddressResolver>(),
      serviceProvider: serviceProvider
    )
  {
  }

  protected ProcfsArpMacAddressResolver(
    MacAddressResolverOptions options,
    ILogger? logger,
    IServiceProvider? serviceProvider
  )
    : base(
      logger: logger
    )
  {
    if (options.ProcfsArpFullScanInterval <= TimeSpan.Zero) {
      if (options.ProcfsArpFullScanInterval != Timeout.InfiniteTimeSpan)
        throw new InvalidOperationException("invalid interval value");
    }

    arpFullScanInterval = options.ProcfsArpFullScanInterval;

    this.neighborDiscoverer = neighborDiscoverer ?? throw new ArgumentNullException(nameof(neighborDiscoverer));
    neighborTable = new ProcfsArpNeighborTable(serviceProvider);
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

    NeighborTableEntry? priorCandidate = default;
    NeighborTableEntry? candidate = default;

    await foreach (var entry in neighborTable.EnumerateEntriesAsync(
      cancellationToken
    ).ConfigureAwait(false)) {
      if (!entry.Equals(ipAddress))
        continue;
      if (entry.PhysicalAddress is null || entry.Equals(AllZeroMacAddress))
        continue;

      if (invalidatedMacAddressSet.ContainsKey(entry.PhysicalAddress!))
        continue; // ignore the entry that is marked as invalidated

      if (entry.IsPermanent || entry.State == NeighborTableEntryState.Reachable) {
        // prefer permanent or reachable entry
        priorCandidate = entry;
        break;
      }

      candidate = entry; // select the last entry found
    }

    return priorCandidate?.PhysicalAddress ?? candidate?.PhysicalAddress;
  }

  protected override async ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(
    PhysicalAddress macAddress,
    CancellationToken cancellationToken
  )
  {
    if (HasArpFullScanIntervalElapsed)
      await ArpFullScanAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

    NeighborTableEntry? priorCandidate = default;
    NeighborTableEntry? candidate = default;

    await foreach (var entry in neighborTable.EnumerateEntriesAsync(
      cancellationToken
    ).ConfigureAwait(false)) {
      if (!entry.Equals(macAddress))
        continue;

      if (invalidatedIPAddressSet.ContainsKey(entry.IPAddress!))
        continue; // ignore the entry that is marked as invalidated

      if (entry.IsPermanent || entry.State == NeighborTableEntryState.Reachable) {
        // prefer permanent or reachable entry
        priorCandidate = entry;
        break;
      }

      candidate = entry; // select the last entry found
    }

    return priorCandidate?.IPAddress ?? candidate?.IPAddress;
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

    Logger?.LogInformation("Performing ARP full scan.");

    var sw = Logger is null ? null : Stopwatch.StartNew();

    try {
      await ArpFullScanAsyncCore(cancellationToken: cancellationToken).ConfigureAwait(false);

      invalidatedIPAddressSet.Clear();
      invalidatedMacAddressSet.Clear();

      lastArpFullScanAt = DateTime.Now;
    }
    finally {
      Logger?.LogInformation("ARP full scan finished in {ElapsedMilliseconds} ms.", sw!.ElapsedMilliseconds);

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

    var sw = Logger is null ? null : Stopwatch.StartNew();

    try {
      Logger?.LogInformation("Performing ARP scan for the invalidated {Count} IP addresses.", invalidatedIPAddresses.Count);

      await ArpScanAsyncCore(
        invalidatedIPAddresses: invalidatedIPAddresses,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      invalidatedIPAddressSet.Clear();
    }
    finally {
      Logger?.LogInformation("ARP scan finished in {ElapsedMilliseconds} ms.", sw!.ElapsedMilliseconds);

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
