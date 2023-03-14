// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
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

  /*
   * instance members
   */
  private DateTime lastArpScanAt = DateTime.MinValue;
  private readonly TimeSpan arpScanInterval;

  private bool HasArpScanIntervalElapsed => lastArpScanAt + arpScanInterval <= DateTime.Now;

  public ProcfsArpMacAddressResolver(
    MacAddressResolverOptions options,
    ILogger? logger
  )
    : base(logger)
  {
    arpScanInterval = options.ProcfsArpScanInterval;
  }

  protected override async ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(
    IPAddress ipAddress,
    CancellationToken cancellationToken
  )
  {
    if (HasArpScanIntervalElapsed)
      await ArpScanAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

    ArpTableEntry priorCandidate = default;
    ArpTableEntry candidate = default;

    await foreach (var entry in ArpTableEntry.EnumerateArpTableEntriesAsync(
      e => e.Equals(ipAddress),
      Logger,
      cancellationToken
    ).ConfigureAwait(false)) {
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
    if (HasArpScanIntervalElapsed)
      await ArpScanAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

    ArpTableEntry priorCandidate = default;
    ArpTableEntry candidate = default;

    await foreach (var entry in ArpTableEntry.EnumerateArpTableEntriesAsync(
      e => e.Equals(macAddress),
      Logger,
      cancellationToken
    ).ConfigureAwait(false)) {
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
      : ArpScanAsync(cancellationToken: cancellationToken);

  private async ValueTask ArpScanAsync(CancellationToken cancellationToken)
  {
    Logger?.LogDebug("Performing ARP scan");

    await ArpScanAsyncCore(cancellationToken: cancellationToken).ConfigureAwait(false);

    lastArpScanAt = DateTime.Now;
  }

  protected virtual ValueTask ArpScanAsyncCore(CancellationToken cancellationToken)
  {
    Logger?.LogWarning("ARP scan is not supported in this class.");

    return default;
  }
}
