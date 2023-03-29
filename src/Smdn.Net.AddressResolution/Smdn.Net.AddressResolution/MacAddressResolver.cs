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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

public class MacAddressResolver : MacAddressResolverBase {
  private const int PartialScanParallelMax = 3;

#pragma warning disable IDE0060
  private static INeighborTable CreateNeighborTable(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider
  )
#pragma warning restore IDE0060
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      if (IpHlpApiNeighborTable.IsSupported)
        return new IpHlpApiNeighborTable(serviceProvider);
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
      if (ProcfsArpNeighborTable.IsSupported)
        return new ProcfsArpNeighborTable(serviceProvider);
    }

    throw new PlatformNotSupportedException();
  }

  private static INeighborDiscoverer CreateNeighborDiscoverer(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider
  )
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
      if (NmapCommandNeighborDiscoverer.IsSupported) {
        return new NmapCommandNeighborDiscoverer(
          networkProfile: networkProfile ?? throw new ArgumentNullException(nameof(networkProfile)),
          serviceProvider: serviceProvider
        );
      }

      if (ArpScanCommandNeighborDiscoverer.IsSupported) {
        return new ArpScanCommandNeighborDiscoverer(
          networkProfile: networkProfile, // nullable
          serviceProvider: serviceProvider
        );
      }
    }

    throw new PlatformNotSupportedException();
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
  private DateTime lastFullScanPerformedAt = DateTime.MinValue;
  private readonly TimeSpan neighborDiscoveryInterval;

  private bool HasFullScanIntervalElapsed =>
    neighborDiscoveryInterval != Timeout.InfiniteTimeSpan &&
    lastFullScanPerformedAt + neighborDiscoveryInterval <= DateTime.Now;

  private readonly ConcurrentSet<IPAddress> invalidatedIPAddressSet = new();
  private readonly ConcurrentSet<PhysicalAddress> invalidatedMacAddressSet = new();

  public override bool HasInvalidated => !(invalidatedIPAddressSet.IsEmpty && invalidatedMacAddressSet.IsEmpty);

  // mutex for neighbor discovery (a.k.a full scan)
  private SemaphoreSlim fullScanMutex = new(initialCount: 1, maxCount: 1);

  // semaphore for address resolition (a.k.a partial scan)
  private SemaphoreSlim partialScanSemaphore = new(initialCount: PartialScanParallelMax, maxCount: PartialScanParallelMax);

  /// <inheritdoc cref="MacAddressResolver(IPNetworkProfile?, TimeSpan, IServiceProvider?)" />
  public MacAddressResolver(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider = null
  )
    : this(
      networkProfile: networkProfile,
      neighborDiscoveryInterval: Timeout.InfiniteTimeSpan,
      serviceProvider: serviceProvider
    )
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="KasaDevice"/> class with specifying the device endpoint by IP address.
  /// </summary>
  /// <param name="networkProfile">
  /// The <see cref="IPNetworkProfile"/> which specifying the network interface and neighbor discovery target addresses.
  /// This is used as necessary for neighbor discovery in address resolution.
  /// </param>
  /// <param name="neighborDiscoveryInterval">
  /// The <see cref="TimeSpan"/> which represents the interval to perform a neighbor discovery.
  /// If this period has elapsed since the lastest neighbor discovery,
  /// the instance performs neighbor discovery automatically when the <see cref="ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)" /> or
  /// <see cref="ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)"> is called.
  /// If <see cref="Timeout.InfiniteTimeSpan" /> is specified, the instance does not perform neighbor discovery automatically.
  /// </param>
  /// <param name="serviceProvider">
  /// The <see cref="IServiceProvider"/>.
  /// </param>
  public MacAddressResolver(
    IPNetworkProfile? networkProfile,
    TimeSpan neighborDiscoveryInterval,
    IServiceProvider? serviceProvider = null
  )
    : this(
      neighborTable: CreateNeighborTable(networkProfile, serviceProvider),
      neighborDiscoverer: CreateNeighborDiscoverer(networkProfile, serviceProvider),
      neighborDiscoveryInterval: neighborDiscoveryInterval,
      serviceProvider: serviceProvider
    )
  {
  }

  public MacAddressResolver(
    INeighborTable? neighborTable = null,
    INeighborDiscoverer? neighborDiscoverer = null,
    int neighborDiscoveryIntervalMilliseconds = Timeout.Infinite,
    IServiceProvider? serviceProvider = null
  )
    : this(
      neighborTable: neighborTable,
      neighborDiscoverer: neighborDiscoverer,
      neighborDiscoveryInterval: TimeSpan.FromMilliseconds(neighborDiscoveryIntervalMilliseconds),
      serviceProvider: serviceProvider
    )
  {
  }

  public MacAddressResolver(
    TimeSpan neighborDiscoveryInterval,
    INeighborTable? neighborTable = null,
    INeighborDiscoverer? neighborDiscoverer = null,
    IServiceProvider? serviceProvider = null
  )
    : this(
      neighborTable:
        neighborTable ??
        serviceProvider?.GetRequiredService<INeighborTable>() ??
        throw new ArgumentNullException(nameof(neighborTable)),
      neighborDiscoverer:
        neighborDiscoverer ??
        serviceProvider?.GetRequiredService<INeighborDiscoverer>() ??
        throw new ArgumentNullException(nameof(neighborDiscoverer)),
      neighborDiscoveryInterval: neighborDiscoveryInterval,
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<MacAddressResolver>()
    )
  {
  }

  protected MacAddressResolver(
    INeighborTable neighborTable,
    INeighborDiscoverer neighborDiscoverer,
    TimeSpan neighborDiscoveryInterval,
    ILogger? logger
  )
    : base(
      logger: logger
    )
  {
    if (neighborDiscoveryInterval <= TimeSpan.Zero) {
      if (neighborDiscoveryInterval != Timeout.InfiniteTimeSpan)
        throw new InvalidOperationException("invalid interval value");
    }

    this.neighborDiscoveryInterval = neighborDiscoveryInterval;

    this.neighborTable = neighborTable ?? throw new ArgumentNullException(nameof(neighborTable));
    this.neighborDiscoverer = neighborDiscoverer ?? throw new ArgumentNullException(nameof(neighborDiscoverer));
  }

  protected override void Dispose(bool disposing)
  {
    fullScanMutex?.Dispose();
    fullScanMutex = null!;

    partialScanSemaphore?.Dispose();
    partialScanSemaphore = null!;

    base.Dispose(disposing);
  }

  protected override async ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(
    IPAddress ipAddress,
    CancellationToken cancellationToken
  )
  {
    Logger?.LogDebug("Resolving {IPAddress}", ipAddress);

    if (HasFullScanIntervalElapsed)
      await FullScanAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

    NeighborTableEntry? priorCandidate = default;
    NeighborTableEntry? candidate = default;

    await foreach (var entry in neighborTable.EnumerateEntriesAsync(
      cancellationToken
    ).ConfigureAwait(false)) {
      if (!entry.Equals(ipAddress))
        continue;

      Logger?.LogDebug(
        "Entry: IP={IPAddress}, MAC={MacAddress}, IsPermanent={IsPermanent}, State={State}",
        entry.IPAddress,
        entry.PhysicalAddress?.ToMacAddressString(),
        entry.IsPermanent,
        entry.State
      );

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
    if (HasFullScanIntervalElapsed)
      await FullScanAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

    NeighborTableEntry? priorCandidate = default;
    NeighborTableEntry? candidate = default;

    await foreach (var entry in neighborTable.EnumerateEntriesAsync(
      cancellationToken
    ).ConfigureAwait(false)) {
      if (!entry.Equals(macAddress))
        continue;

      Logger?.LogDebug(
        "Entry: IP={IPAddress}, MAC={MacAddress}, IsPermanent={IsPermanent}, State={State}",
        entry.IPAddress,
        entry.PhysicalAddress?.ToMacAddressString(),
        entry.IsPermanent,
        entry.State
      );

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
      : FullScanAsync(cancellationToken: cancellationToken);

  private async ValueTask FullScanAsync(CancellationToken cancellationToken)
  {
    if (!await fullScanMutex.WaitAsync(0, cancellationToken: default).ConfigureAwait(false)) {
      Logger?.LogInformation("Neighbor discovery is currently being performed.");
      return;
    }

    Logger?.LogInformation("Performing neighbor discovery.");

    var sw = Logger is null ? null : Stopwatch.StartNew();

    try {
      await neighborDiscoverer.DiscoverAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      invalidatedIPAddressSet.Clear();
      invalidatedMacAddressSet.Clear();

      lastFullScanPerformedAt = DateTime.Now;
    }
    finally {
      Logger?.LogInformation("Neighbor discovery finished in {ElapsedMilliseconds} ms.", sw!.ElapsedMilliseconds);

      fullScanMutex.Release();
    }
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
      : PartialScanAsync(cancellationToken: cancellationToken);

  private async ValueTask PartialScanAsync(CancellationToken cancellationToken)
  {
    var invalidatedIPAddresses = invalidatedIPAddressSet.Keys;
    var invalidatedMacAddresses = invalidatedMacAddressSet.Keys;

    Logger?.LogTrace("Invalidated IP addresses: {InvalidatedIPAddresses}", string.Join(" ", invalidatedIPAddresses));
    Logger?.LogTrace("Invalidated MAC addresses: {InvalidatedMACAddresses}", string.Join(" ", invalidatedMacAddresses));

    if (invalidatedMacAddresses.Any()) {
      // perform full scan
      await neighborDiscoverer.DiscoverAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      return;
    }

    await partialScanSemaphore.WaitAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

    var sw = Logger is null ? null : Stopwatch.StartNew();

    try {
      Logger?.LogInformation("Performing address resolution for the invalidated {Count} IP addresses.", invalidatedIPAddresses.Count);

      await neighborDiscoverer.DiscoverAsync(
        addresses: invalidatedIPAddresses,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      invalidatedIPAddressSet.Clear();
    }
    finally {
      Logger?.LogInformation("Address resolution finished in {ElapsedMilliseconds} ms.", sw!.ElapsedMilliseconds);

      partialScanSemaphore.Release();
    }
  }
}
