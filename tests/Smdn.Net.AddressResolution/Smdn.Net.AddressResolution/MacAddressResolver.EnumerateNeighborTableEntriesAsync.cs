// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

partial class MacAddressResolverTests {
  [Test]
  public void EnumerateNeighborTableEntriesAsync_CancellationRequested()
  {
    var neighborTableEntries = new NeighborTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, NeighborTableEntryState.None, "wlan0"),
      new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, NeighborTableEntryState.None, "wlan1"),
      new(IPAddress.Parse("192.168.2.2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, NeighborTableEntryState.None, "wlan2")
    };

    using var resolver = new MacAddressResolver(
      networkInterface: null,
      neighborTable: new StaticNeighborTable(neighborTableEntries),
      neighborDiscoverer: new NullNeighborDiscoverer()
    );
    using var cts = new CancellationTokenSource();
    Predicate<NeighborTableEntry> predicate = e => {
      Assert.IsFalse(cts.IsCancellationRequested);

      if (e.InterfaceId == "wlan0")
        cts.Cancel();

      return true;
    };
    int count = 0;

    var ex = Assert.CatchAsync(
      async () => {
        await foreach (var entry in resolver.EnumerateNeighborTableEntriesAsync(predicate, cts.Token)) {
          count++;
        }
      }
    );

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
    Assert.AreEqual(1, count, nameof(count));
  }
}
