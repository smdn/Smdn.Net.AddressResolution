// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

#pragma warning disable IDE0040
partial class MacAddressResolver {
#pragma warning restore IDE0040

  /// <summary>
  /// Enumerates the relevant address table entries from the <see cref="INeighborTable"/> associated with the current instance.
  /// </summary>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken" /> to monitor for cancellation requests.
  /// The default value is <see langword="default" />.
  /// </param>
  /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
  /// <seealso cref="INeighborTable"/>
  /// <seealso cref="NeighborTableEntry"/>
  public IAsyncEnumerable<NeighborTableEntry> EnumerateNeighborTableEntriesAsync(
    CancellationToken cancellationToken = default
  )
    => EnumerateNeighborTableEntriesAsync(
      predicate: entry => networkInterface is null || FilterNeighborTableEntryForNetworkInterface(entry),
      cancellationToken: cancellationToken
    );

  /// <inheritdoc cref="EnumerateNeighborTableEntriesAsync(CancellationToken)"/>
  /// <param name="predicate">
  /// A <see cref="Predicate{NeighborTableEntry}"/> that filters the entries enumerated from <see cref="INeighborTable"/>.
  /// </param>
  public IAsyncEnumerable<NeighborTableEntry> EnumerateNeighborTableEntriesAsync(
    Predicate<NeighborTableEntry> predicate,
    CancellationToken cancellationToken = default
  )
  {
    if (predicate is null)
      throw new ArgumentNullException(nameof(predicate));

    ThrowIfDisposed();

    return EnumerateNeighborTableEntriesAsyncCore(
      predicate: predicate,
      cancellationToken: cancellationToken
    );
  }

  private async IAsyncEnumerable<NeighborTableEntry> EnumerateNeighborTableEntriesAsyncCore(
    Predicate<NeighborTableEntry> predicate,
    [EnumeratorCancellation] CancellationToken cancellationToken
  )
  {
    await foreach (var entry in neighborTable.EnumerateEntriesAsync(
      cancellationToken
    ).ConfigureAwait(false)) {
      cancellationToken.ThrowIfCancellationRequested();

      if (predicate(entry))
        yield return entry;
    }
  }

  private bool FilterNeighborTableEntryForNetworkInterface(NeighborTableEntry entry)
  {
#if DEBUG
    if (networkInterface is null)
      throw new InvalidOperationException($"{nameof(networkInterface)} is null.");
#endif
    if (entry.IsEmpty)
      return false;

    // exclude entries that are irrelevant to the network interface
    if (
      entry.InterfaceId is not null &&
      !entry.InterfaceIdEquals(networkInterface.Id)
    ) {
      return false;
    }

#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
#pragma warning disable CS8602
#endif
    // exclude addresses of address families not supported by the network interface
    if (
      entry.IPAddress.AddressFamily == AddressFamily.InterNetwork &&
      !networkInterface.Supports(NetworkInterfaceComponent.IPv4)
    ) {
      return false;
    }

    if (
      entry.IPAddress.AddressFamily == AddressFamily.InterNetworkV6 &&
      !networkInterface.Supports(NetworkInterfaceComponent.IPv6)
    ) {
      return false;
    }
#pragma warning restore CS8602

    return true;
  }

  private bool FilterNeighborTableEntryForAddressResolution(NeighborTableEntry entry)
  {
    var include = true;

    // exclude unresolvable entries
    if (entry.PhysicalAddress is null || entry.Equals(AllZeroMacAddress)) {
      include = false;
      goto RESULT_DETERMINED;
    }

    // exclude entries that are irrelevant to or not supported by the network interface
    if (networkInterface is not null && !FilterNeighborTableEntryForNetworkInterface(entry)) {
      include = false;
      goto RESULT_DETERMINED;
    }

  RESULT_DETERMINED:
    Logger?.LogTrace(
      "{FilterResult}: {Entry}",
      include ? "Include" : "Exclude",
      entry
    );

    return include;
  }
}
