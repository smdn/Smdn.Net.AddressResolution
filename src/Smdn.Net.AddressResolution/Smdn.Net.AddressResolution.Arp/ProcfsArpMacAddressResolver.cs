// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressResolution.Arp;

internal partial class ProcfsArpMacAddressResolver : MacAddressResolver {
  private const string PathToProcNetArp = "/proc/net/arp";

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

  public ProcfsArpMacAddressResolver(
    MacAddressResolverOptions options,
    ILogger? logger
  )
    : base(logger)
  {
    arpFullScanInterval = options.ProcfsArpFullScanInterval;
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

  protected override void InvalidateCore(IPAddress resolvedIPAddress)
    => invalidatedIPAddressSet.Add(resolvedIPAddress);

  protected override void InvalidateCore(PhysicalAddress resolvedMacAddress)
    => invalidatedMacAddressSet.Add(resolvedMacAddress);

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
    Logger?.LogDebug("Performing ARP full scan");

    await ArpFullScanAsyncCore(cancellationToken: cancellationToken).ConfigureAwait(false);

    invalidatedIPAddressSet.Clear();
    invalidatedMacAddressSet.Clear();

    lastArpFullScanAt = DateTime.Now;
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
    Logger?.LogDebug("Performing ARP scan for invalidated targets.");

    var invalidatedIPAddresses = invalidatedIPAddressSet.Keys;
    var invalidatedMacAddresses = invalidatedMacAddressSet.Keys;

    Logger?.LogTrace("Invalidated IP addresses: {InvalidatedIPAddresses}", string.Join(" ", invalidatedIPAddresses));
    Logger?.LogTrace("Invalidated MAC addresses: {InvalidatedMACAddresses}", string.Join(" ", invalidatedMacAddresses));

    await ArpScanAsyncCore(
      invalidatedIPAddresses: invalidatedIPAddresses,
      invalidatedMacAddresses: invalidatedMacAddresses,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    invalidatedIPAddressSet.Clear();
    invalidatedMacAddressSet.Clear();

    lastArpFullScanAt = DateTime.Now;
  }

  protected virtual ValueTask ArpScanAsyncCore(
    IEnumerable<IPAddress> invalidatedIPAddresses,
    IEnumerable<PhysicalAddress> invalidatedMacAddresses,
    CancellationToken cancellationToken
  )
  {
    Logger?.LogWarning("ARP scan is not supported in this class.");

    return default;
  }
}
