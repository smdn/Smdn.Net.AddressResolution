// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.AddressTables;
using Smdn.Net.NetworkScanning;

namespace Smdn.Net.AddressResolution;

/// <summary>
/// Provides a mechanism for the mutual address resolution between IP addresses and corresponding MAC addresses.
/// </summary>
/// <remarks>
///   <para>
///     This implementation uses the system's address cache mechanism (such as the ARP table) and network scan
///     mechanism (such as the network scan command) for address resolution.
///   </para>
///   <para>
///     Any <see cref="IAddressTable"/> can be specified as an implementation that references an address table.
///     Also any <see cref="INetworkScanner"/> can be specified as an implementation that performs the network scan.
///   </para>
/// </remarks>
/// <seealso cref="IAddressTable"/>
/// <seealso cref="INetworkScanner"/>
public partial class MacAddressResolver : MacAddressResolverBase {
  private IAddressTable addressTable;
  private INetworkScanner? networkScanner;
  private readonly NetworkInterface? networkInterface;

  private readonly bool shouldDisposeAddressTable;
  private readonly bool shouldDisposeNetworkScanner;

  /// <summary>
  /// Gets a value indicating whether the instance can perform network scan.
  /// </summary>
  /// <remarks>
  /// <para>
  /// To perform a network scan, an implementation of <see cref="INetworkScanner" /> must be provided to the constructor parameter.
  /// </para>
  /// <para>
  /// If <see cref="INetworkScanner" /> is not provided to the constructor parameter, i.e., <see langword="null"/> is specified,
  /// value of <see cref="CanPerformNetworkScan"/> will be <see langword="false"/> and no network scan will be performed.
  /// </para>
  /// <para>
  /// If <see cref="CanPerformNetworkScan"/> is <see langword="false"/>, calling <see cref="MacAddressResolverBase.RefreshAddressTableAsync(CancellationToken)" /> or
  /// <see cref="MacAddressResolverBase.RefreshInvalidatedAddressesAsync(CancellationToken)" /> throws <see cref="InvalidOperationException"/>.
  /// Also, automatic network scanning by calling of <see cref="MacAddressResolverBase.ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)" /> or
  /// <see cref="MacAddressResolverBase.ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)" /> will not performed.
  /// </para>
  /// </remarks>
  /// <seealso cref="NetworkScanInterval"/>
  /// <seealso cref="NetworkScanMinInterval"/>
  /// <seealso cref="MacAddressResolverBase.RefreshAddressTableAsync(CancellationToken)"/>
  /// <seealso cref="MacAddressResolverBase.RefreshInvalidatedAddressesAsync(CancellationToken)"/>
  public bool CanPerformNetworkScan => networkScanner is not null;

  private static InvalidOperationException CreateCanNotPerformNetworkScanException()
    => new($"The instance can not perform network scan. To perform a network scan, specify {nameof(INetworkScanner)} in the constructor.");

  private Stopwatch? timeStampForFullScan;
  private TimeSpan networkScanInterval = Timeout.InfiniteTimeSpan;
  private TimeSpan networkScanMinInterval = TimeSpan.FromSeconds(20.0);

  /// <summary>
  /// Gets or sets the <see cref="TimeSpan"/> which represents the interval to perform a network scan.
  /// </summary>
  /// <remarks>
  /// <para>
  /// If the period represented by this property has elapsed since the lastest network scan,
  /// the instance performs network scan automatically when the <see cref="MacAddressResolverBase.ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)" /> or
  /// <see cref="MacAddressResolverBase.ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)" /> is called.
  /// </para>
  /// <para>
  /// If <see cref="Timeout.InfiniteTimeSpan" /> is specified, the instance does not perform network scan automatically.
  /// </para>
  /// </remarks>
  /// <seealso cref="CanPerformNetworkScan"/>
  /// <seealso cref="NetworkScanMinInterval"/>
  /// <seealso cref="MacAddressResolverBase.ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)" />
  /// <seealso cref="MacAddressResolverBase.ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)" />
  public TimeSpan NetworkScanInterval {
    get => networkScanInterval;
    set {
      if (value <= TimeSpan.Zero) {
        if (value != Timeout.InfiniteTimeSpan)
          throw new ArgumentOutOfRangeException(message: $"The value must be non-zero positive {nameof(TimeSpan)} or {nameof(Timeout)}.{nameof(Timeout.InfiniteTimeSpan)}.", paramName: nameof(NetworkScanInterval));
      }

      networkScanInterval = value;
    }
  }

  /// <summary>
  /// Gets or sets the <see cref="TimeSpan"/> which represents the minimum interval to perform a network scan.
  /// </summary>
  /// <remarks>
  /// <para>
  /// If the period represented by this property has not elapsed since the lastest network scan,
  /// the instance will not performs network scan.
  /// </para>
  /// <para>
  /// The network scan will be performed automatically when the <see cref="MacAddressResolverBase.ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)" /> or
  /// <see cref="MacAddressResolverBase.ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)" /> is called, or explicitly performed by calling the
  /// <see cref="MacAddressResolverBase.RefreshAddressTableAsync(CancellationToken)" />.
  /// </para>
  /// <para>
  /// If <see cref="Timeout.InfiniteTimeSpan" /> is specified, the instance does not perform the network scan.
  /// If <see cref="TimeSpan.Zero" /> is specified, the instance always performs the network scan as requested.
  /// </para>
  /// </remarks>
  /// <seealso cref="CanPerformNetworkScan"/>
  /// <seealso cref="NetworkScanInterval"/>
  /// <seealso cref="MacAddressResolverBase.RefreshAddressTableAsync(CancellationToken)" />
  /// <seealso cref="MacAddressResolverBase.ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)" />
  /// <seealso cref="MacAddressResolverBase.ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)" />
  public TimeSpan NetworkScanMinInterval {
    get => networkScanMinInterval;
    set {
      if (value < TimeSpan.Zero) {
        if (value != Timeout.InfiniteTimeSpan)
          throw new ArgumentOutOfRangeException(message: $"The value must be non-zero positive {nameof(TimeSpan)} or {nameof(Timeout)}.{nameof(Timeout.InfiniteTimeSpan)}.", paramName: nameof(NetworkScanMinInterval));
      }

      networkScanMinInterval = value;
    }
  }

  private bool HasFullScanMinIntervalElapsed =>
    networkScanMinInterval == TimeSpan.Zero ||
    (
      networkScanMinInterval != Timeout.InfiniteTimeSpan &&
      (
        timeStampForFullScan is null || // not performed yet
        networkScanMinInterval <= timeStampForFullScan.Elapsed // interval elapsed
      )
    );

  private bool ShouldPerformFullScanBeforeResolution =>
    networkScanInterval != Timeout.InfiniteTimeSpan &&
    (
      timeStampForFullScan is null || // not performed yet
      networkScanInterval <= timeStampForFullScan.Elapsed // interval elapsed
    );

  private readonly ConcurrentSet<IPAddress> invalidatedIPAddressSet = new();
  private readonly ConcurrentSet<PhysicalAddress> invalidatedMacAddressSet = new();

  public override bool HasInvalidated => !(invalidatedIPAddressSet.IsEmpty && invalidatedMacAddressSet.IsEmpty);

  // mutex for network scan (a.k.a full scan)
  private SemaphoreSlim fullScanMutex = new(initialCount: 1, maxCount: 1);

  // semaphore for address resolution (a.k.a partial scan)
  private const int DefaultParallelCountForRefreshInvalidatedAddresses = 3;
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
  /// The <see cref="IPNetworkProfile"/> which specifying the network interface and network scan target addresses.
  /// This is used as necessary for network scan in address resolution.
  /// </param>
  /// <param name="serviceProvider">
  /// The <see cref="IServiceProvider"/>.
  /// </param>
  public MacAddressResolver(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider = null
  )
    : this(
      addressTable: GetOrCreateAddressTableImplementation(networkProfile, serviceProvider),
      networkScanner: GetOrCreateNetworkScannerImplementation(networkProfile, serviceProvider),
      networkInterface: networkProfile?.NetworkInterface,
      maxParallelCountForRefreshInvalidatedAddresses: DefaultParallelCountForRefreshInvalidatedAddresses,
      logger: CreateLogger(serviceProvider)
    )
  {
  }

  private static ILogger? CreateLogger(IServiceProvider? serviceProvider)
    => serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<MacAddressResolver>();

#pragma warning disable IDE0060
  private static (IAddressTable Implementation, bool ShouldDispose) GetOrCreateAddressTableImplementation(
    IPNetworkProfile? networkProfile, // intended to the posibility to implement address tables that need to specify networks in the future
    IServiceProvider? serviceProvider
  )
#pragma warning restore IDE0060
  {
    var impl = serviceProvider?.GetService<IAddressTable>();

    return impl is null
      ? (AddressTable.Create(serviceProvider), true)
      : (impl, false);
  }

  private static (INetworkScanner Implementation, bool ShouldDispose) GetOrCreateNetworkScannerImplementation(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider
  )
  {
    var impl = serviceProvider?.GetService<INetworkScanner>();

    return impl is null
      ? (NetworkScanner.Create(networkProfile, serviceProvider), true)
      : (impl, false);
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="MacAddressResolver"/> class.
  /// </summary>
  /// <param name="addressTable">
  ///   An <see cref="IAddressTable"/> that implements a mechanism to refer address table.
  ///   If <see langword="null" />, attempts to retrieve <see cref="IAddressTable"/> from <paramref name="serviceProvider"/>.
  /// </param>
  /// <param name="networkScanner">
  ///   An <see cref="INetworkScanner"/> that implements a mechanism to perform network scan.
  ///   If <see langword="null" />, attempts to retrieve <see cref="INetworkScanner"/> from <paramref name="serviceProvider"/>.
  /// </param>
  /// <param name="shouldDisposeAddressTable">
  ///   A value that indicates whether the <see cref="IAddressTable"/> passed from the <paramref name="addressTable"/> should also be disposed when the instance is disposed.
  /// </param>
  /// <param name="shouldDisposeNetworkScanner">
  ///   A value that indicates whether the <see cref="INetworkScanner"/> passed from the <paramref name="networkScanner"/> should also be disposed when the instance is disposed.
  /// </param>
  /// <param name="networkInterface">
  ///   A <see cref="NetworkInterface"/> on which the entry should be referenced from the <see cref="IAddressTable"/>.
  ///   If <see langword="null" />, all entries that can be referenced from the <see cref="IAddressTable"/> are used to address resolution.
  /// </param>
  /// <param name="maxParallelCountForRefreshInvalidatedAddresses">
  ///   A value that specifies the maximum number of parallel executions allowed when <paramref name="addressTable"/> updates the invalidated addresses.
  /// </param>
  /// <param name="serviceProvider">
  ///   A <see cref="IServiceProvider"/>.
  ///   This constructor overload attempts to retrieve the <see cref="IAddressTable"/>, <see cref="INetworkScanner"/>,
  ///   and <see cref="ILogger"/> if not explicitly specified.
  /// </param>
  /// <exception cref="ArgumentNullException">
  ///   <para>Both <paramref name="serviceProvider"/> and <paramref name="addressTable"/> are <see langword="null" />.</para>
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  ///   <paramref name="maxParallelCountForRefreshInvalidatedAddresses"/> is zero or negative number.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  ///   <para><paramref name="addressTable"/> is <see langword="null" /> and cannot retrieve <see cref="IAddressTable"/> from <paramref name="serviceProvider"/>.</para>
  /// </exception>
  public MacAddressResolver(
    IAddressTable? addressTable,
    INetworkScanner? networkScanner,
    bool shouldDisposeAddressTable = false,
    bool shouldDisposeNetworkScanner = false,
    NetworkInterface? networkInterface = null,
    int maxParallelCountForRefreshInvalidatedAddresses = DefaultParallelCountForRefreshInvalidatedAddresses,
    IServiceProvider? serviceProvider = null
  )
    : this(
      addressTable:
        addressTable ??
        serviceProvider?.GetRequiredService<IAddressTable>() ??
        throw new ArgumentNullException(nameof(addressTable)),
      shouldDisposeAddressTable: shouldDisposeAddressTable,
      networkScanner:
        networkScanner ??
        serviceProvider?.GetService<INetworkScanner>(),
      shouldDisposeNetworkScanner: shouldDisposeNetworkScanner,
      networkInterface: networkInterface,
      maxParallelCountForRefreshInvalidatedAddresses: maxParallelCountForRefreshInvalidatedAddresses,
      logger: CreateLogger(serviceProvider)
    )
  {
  }

  private MacAddressResolver(
    (IAddressTable Implementation, bool ShouldDispose) addressTable,
    (INetworkScanner Implementation, bool ShouldDispose) networkScanner,
    NetworkInterface? networkInterface,
    int maxParallelCountForRefreshInvalidatedAddresses,
    ILogger? logger
  )
    : this(
      addressTable: addressTable.Implementation,
      shouldDisposeAddressTable: addressTable.ShouldDispose,
      networkScanner: networkScanner.Implementation,
      shouldDisposeNetworkScanner: networkScanner.ShouldDispose,
      networkInterface: networkInterface,
      maxParallelCountForRefreshInvalidatedAddresses: maxParallelCountForRefreshInvalidatedAddresses,
      logger: logger
    )
  {
  }

  protected MacAddressResolver(
    IAddressTable addressTable,
    bool shouldDisposeAddressTable,
    INetworkScanner? networkScanner,
    bool shouldDisposeNetworkScanner,
    NetworkInterface? networkInterface,
    int maxParallelCountForRefreshInvalidatedAddresses,
    ILogger? logger
  )
    : base(
      logger: logger
    )
  {
    if (maxParallelCountForRefreshInvalidatedAddresses <= 0)
      throw new ArgumentOutOfRangeException(message: "must be non-zero positive number", paramName: nameof(maxParallelCountForRefreshInvalidatedAddresses));

    this.addressTable = addressTable ?? throw new ArgumentNullException(nameof(addressTable));
    this.networkScanner = networkScanner;
    this.networkInterface = networkInterface;

    this.shouldDisposeAddressTable = shouldDisposeAddressTable;
    this.shouldDisposeNetworkScanner = shouldDisposeNetworkScanner;

    logger?.LogInformation("IAddressTable: {IAddressTable}", this.addressTable.GetType().FullName);
    logger?.LogInformation("INetworkScanner: {INetworkScanner}", this.networkScanner?.GetType()?.FullName ?? "(null)");
    logger?.LogInformation(
      "NetworkInterface: {NetworkInterfaceId}, IPv4={IPv4}, IPv6={IPv6}",
      networkInterface?.Id ?? "(null)",
      (networkInterface?.Supports(NetworkInterfaceComponent.IPv4) ?? false) ? "yes" : "no",
      (networkInterface?.Supports(NetworkInterfaceComponent.IPv6) ?? false) ? "yes" : "no"
    );

    partialScanSemaphore = new(
      initialCount: maxParallelCountForRefreshInvalidatedAddresses,
      maxCount: maxParallelCountForRefreshInvalidatedAddresses
    );
  }

  protected override void Dispose(bool disposing)
  {
    if (!disposing)
      return;

    if (shouldDisposeAddressTable)
      addressTable?.Dispose();

    addressTable = null!;

    if (shouldDisposeNetworkScanner)
      networkScanner?.Dispose();

    networkScanner = null!;

    fullScanMutex?.Dispose();
    fullScanMutex = null!;

    partialScanSemaphore?.Dispose();
    partialScanSemaphore = null!;

    base.Dispose(disposing);
  }
}
