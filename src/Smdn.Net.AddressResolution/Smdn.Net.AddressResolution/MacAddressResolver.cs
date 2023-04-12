// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

public partial class MacAddressResolver : MacAddressResolverBase {
  private INeighborTable neighborTable;
  private INeighborDiscoverer neighborDiscoverer;
  private readonly NetworkInterface? networkInterface;

  private readonly bool shouldDisposeNeighborTable;
  private readonly bool shouldDisposeNeighborDiscoverer;

  private Stopwatch? timeStampForFullScan;
  private TimeSpan neighborDiscoveryInterval = Timeout.InfiniteTimeSpan;
  private TimeSpan neighborDiscoveryMinInterval = TimeSpan.FromSeconds(20.0);

  /// <summary>
  /// Gets or sets the <see cref="TimeSpan"/> which represents the interval to perform a neighbor discovery.
  /// </summary>
  /// <remarks>
  /// If the period represented by this property has elapsed since the lastest neighbor discovery,
  /// the instance performs neighbor discovery automatically when the <see cref="ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)" /> or
  /// <see cref="ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)" /> is called.
  /// If <see cref="Timeout.InfiniteTimeSpan" /> is specified, the instance does not perform neighbor discovery automatically.
  /// </remarks>
  /// <seealso cref="NeighborDiscoveryMinInterval"/>
  /// <seealso cref="ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)" />
  /// <seealso cref="ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)" />
  public TimeSpan NeighborDiscoveryInterval {
    get => neighborDiscoveryInterval;
    set {
      if (value <= TimeSpan.Zero) {
        if (value != Timeout.InfiniteTimeSpan)
          throw new ArgumentOutOfRangeException(message: $"The value must be non-zero positive {nameof(TimeSpan)} or {nameof(Timeout)}.{nameof(Timeout.InfiniteTimeSpan)}.", paramName: nameof(NeighborDiscoveryInterval));
      }

      neighborDiscoveryInterval = value;
    }
  }

  /// <summary>
  /// Gets or sets the <see cref="TimeSpan"/> which represents the minimum interval to perform a neighbor discovery.
  /// </summary>
  /// <remarks>
  /// If the period represented by this property has not elapsed since the lastest neighbor discovery,
  /// the instance will not performs neighbor discovery.
  /// The neighbor discovery will be performed automatically when the <see cref="ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)" /> or
  /// <see cref="ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)" /> is called, or explicitly performed by calling the
  /// <see cref="RefreshCacheAsync(CancellationToken)" />.
  /// If <see cref="Timeout.InfiniteTimeSpan" /> is specified, the instance does not perform the neighbor discovery.
  /// If <see cref="TimeSpan.Zero" /> is specified, the instance always performs the neighbor discovery as requested.
  /// </remarks>
  /// <seealso cref="NeighborDiscoveryInterval"/>
  /// <seealso cref="RefreshCacheAsync(CancellationToken)" />
  /// <seealso cref="ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)" />
  /// <seealso cref="ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)" />
  public TimeSpan NeighborDiscoveryMinInterval {
    get => neighborDiscoveryMinInterval;
    set {
      if (value < TimeSpan.Zero) {
        if (value != Timeout.InfiniteTimeSpan)
          throw new ArgumentOutOfRangeException(message: $"The value must be non-zero positive {nameof(TimeSpan)} or {nameof(Timeout)}.{nameof(Timeout.InfiniteTimeSpan)}.", paramName: nameof(NeighborDiscoveryMinInterval));
      }

      neighborDiscoveryMinInterval = value;
    }
  }

  private bool HasFullScanMinIntervalElapsed =>
    neighborDiscoveryMinInterval == TimeSpan.Zero ||
    (
      neighborDiscoveryMinInterval != Timeout.InfiniteTimeSpan &&
      (
        timeStampForFullScan is null || // not performed yet
        neighborDiscoveryMinInterval <= timeStampForFullScan.Elapsed // interval elapsed
      )
    );

  private bool ShouldPerformFullScanBeforeResolution =>
    neighborDiscoveryInterval != Timeout.InfiniteTimeSpan &&
    (
      timeStampForFullScan is null || // not performed yet
      neighborDiscoveryInterval <= timeStampForFullScan.Elapsed // interval elapsed
    );

  private readonly ConcurrentSet<IPAddress> invalidatedIPAddressSet = new();
  private readonly ConcurrentSet<PhysicalAddress> invalidatedMacAddressSet = new();

  public override bool HasInvalidated => !(invalidatedIPAddressSet.IsEmpty && invalidatedMacAddressSet.IsEmpty);

  // mutex for neighbor discovery (a.k.a full scan)
  private SemaphoreSlim fullScanMutex = new(initialCount: 1, maxCount: 1);

  // semaphore for address resolution (a.k.a partial scan)
  private const int DefaultParallelCountForRefreshInvalidatedCache = 3;
  private SemaphoreSlim partialScanSemaphore;

  /// <summary>
  /// Initializes a new instance of the <see cref="MacAddressResolver"/> class.
  /// </summary>
  public MacAddressResolver()
    : this(
      networkProfile: null,
      serviceProvider: null
    )
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="MacAddressResolver"/> class.
  /// </summary>
  /// <param name="networkProfile">
  /// The <see cref="IPNetworkProfile"/> which specifying the network interface and neighbor discovery target addresses.
  /// This is used as necessary for neighbor discovery in address resolution.
  /// </param>
  /// <param name="serviceProvider">
  /// The <see cref="IServiceProvider"/>.
  /// </param>
  public MacAddressResolver(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider = null
  )
    : this(
      neighborTable: GetOrCreateNeighborTableImplementation(networkProfile, serviceProvider),
      neighborDiscoverer: GetOrCreateNeighborDiscovererImplementation(networkProfile, serviceProvider),
      networkInterface: networkProfile?.NetworkInterface,
      maxParallelCountForRefreshInvalidatedCache: DefaultParallelCountForRefreshInvalidatedCache,
      logger: CreateLogger(serviceProvider)
    )
  {
  }

  private static ILogger? CreateLogger(IServiceProvider? serviceProvider)
    => serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<MacAddressResolver>();

  private static (INeighborTable Implementation, bool ShouldDispose) GetOrCreateNeighborTableImplementation(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider
  )
  {
    var impl = serviceProvider?.GetService<INeighborTable>();

    return impl is null
      ? (CreateNeighborTable(networkProfile, serviceProvider), true)
      : (impl, false);
  }

  private static (INeighborDiscoverer Implementation, bool ShouldDispose) GetOrCreateNeighborDiscovererImplementation(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider
  )
  {
    var impl = serviceProvider?.GetService<INeighborDiscoverer>();

    return impl is null
      ? (CreateNeighborDiscoverer(networkProfile, serviceProvider), true)
      : (impl, false);
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="MacAddressResolver"/> class.
  /// </summary>
  public MacAddressResolver(
    INeighborTable? neighborTable,
    INeighborDiscoverer? neighborDiscoverer,
    bool shouldDisposeNeighborTable = false,
    bool shouldDisposeNeighborDiscoverer = false,
    NetworkInterface? networkInterface = null,
    int maxParallelCountForRefreshInvalidatedCache = DefaultParallelCountForRefreshInvalidatedCache,
    IServiceProvider? serviceProvider = null
  )
    : this(
      neighborTable:
        neighborTable ??
        serviceProvider?.GetRequiredService<INeighborTable>() ??
        throw new ArgumentNullException(nameof(neighborTable)),
      shouldDisposeNeighborTable: shouldDisposeNeighborTable,
      neighborDiscoverer:
        neighborDiscoverer ??
        serviceProvider?.GetRequiredService<INeighborDiscoverer>() ??
        throw new ArgumentNullException(nameof(neighborDiscoverer)),
      shouldDisposeNeighborDiscoverer: shouldDisposeNeighborDiscoverer,
      networkInterface: networkInterface,
      maxParallelCountForRefreshInvalidatedCache: maxParallelCountForRefreshInvalidatedCache,
      logger: CreateLogger(serviceProvider)
    )
  {
  }

  private MacAddressResolver(
    (INeighborTable Implementation, bool ShouldDispose) neighborTable,
    (INeighborDiscoverer Implementation, bool ShouldDispose) neighborDiscoverer,
    NetworkInterface? networkInterface,
    int maxParallelCountForRefreshInvalidatedCache,
    ILogger? logger
  )
    : this(
      neighborTable: neighborTable.Implementation,
      shouldDisposeNeighborTable: neighborTable.ShouldDispose,
      neighborDiscoverer: neighborDiscoverer.Implementation,
      shouldDisposeNeighborDiscoverer: neighborDiscoverer.ShouldDispose,
      networkInterface: networkInterface,
      maxParallelCountForRefreshInvalidatedCache: maxParallelCountForRefreshInvalidatedCache,
      logger: logger
    )
  {
  }

  protected MacAddressResolver(
    INeighborTable neighborTable,
    bool shouldDisposeNeighborTable,
    INeighborDiscoverer neighborDiscoverer,
    bool shouldDisposeNeighborDiscoverer,
    NetworkInterface? networkInterface,
    int maxParallelCountForRefreshInvalidatedCache,
    ILogger? logger
  )
    : base(
      logger: logger
    )
  {
    if (maxParallelCountForRefreshInvalidatedCache <= 0)
      throw new ArgumentOutOfRangeException(message: "must be non-zero positive number", paramName: nameof(maxParallelCountForRefreshInvalidatedCache));

    this.neighborTable = neighborTable ?? throw new ArgumentNullException(nameof(neighborTable));
    this.neighborDiscoverer = neighborDiscoverer ?? throw new ArgumentNullException(nameof(neighborDiscoverer));
    this.networkInterface = networkInterface;

    this.shouldDisposeNeighborTable = shouldDisposeNeighborTable;
    this.shouldDisposeNeighborDiscoverer = shouldDisposeNeighborDiscoverer;

    logger?.LogInformation("INeighborTable: {INeighborTable}", this.neighborTable.GetType().FullName);
    logger?.LogInformation("INeighborDiscoverer: {INeighborDiscoverer}", this.neighborDiscoverer.GetType().FullName);
    logger?.LogInformation(
      "NetworkInterface: {NetworkInterfaceId}, IPv4={IPv4}, IPv6={IPv6}",
      networkInterface?.Id ?? "(null)",
      (networkInterface?.Supports(NetworkInterfaceComponent.IPv4) ?? false) ? "yes" : "no",
      (networkInterface?.Supports(NetworkInterfaceComponent.IPv6) ?? false) ? "yes" : "no"
    );

    partialScanSemaphore = new(
      initialCount: maxParallelCountForRefreshInvalidatedCache,
      maxCount: maxParallelCountForRefreshInvalidatedCache
    );
  }

  protected override void Dispose(bool disposing)
  {
    if (!disposing)
      return;

    if (shouldDisposeNeighborTable)
      neighborTable?.Dispose();

    neighborTable = null!;

    if (shouldDisposeNeighborDiscoverer)
      neighborDiscoverer?.Dispose();

    neighborDiscoverer = null!;

    fullScanMutex?.Dispose();
    fullScanMutex = null!;

    partialScanSemaphore?.Dispose();
    partialScanSemaphore = null!;

    base.Dispose(disposing);
  }
}
