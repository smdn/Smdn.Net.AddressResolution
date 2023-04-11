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
  public void EnumerateNeighborTableEntriesAsync_Disposed()
  {
    using var resolver = new MacAddressResolver(
      networkInterface: null,
      neighborTable: new StaticNeighborTable(new[] { default(NeighborTableEntry) } ),
      neighborDiscoverer: new NullNeighborDiscoverer()
    );

    resolver.Dispose();

    Assert.Throws<ObjectDisposedException>(
      () => resolver.EnumerateNeighborTableEntriesAsync()
    );
    Assert.Throws<ObjectDisposedException>(
      () => resolver.EnumerateNeighborTableEntriesAsync(predicate: static _ => true)
    );

    Assert.ThrowsAsync<ObjectDisposedException>(
      async () => {
        await foreach (var entry in resolver.EnumerateNeighborTableEntriesAsync()) {
          Assert.Fail("must not be enumerated");
        }
      }
    );
    Assert.ThrowsAsync<ObjectDisposedException>(
      async () => {
        await foreach (var entry in resolver.EnumerateNeighborTableEntriesAsync(predicate: static _ => true)) {
          Assert.Fail("must not be enumerated");
        }
      }
    );
  }

  [Test]
  public async Task EnumerateNeighborTableEntriesAsync_NetworkInterfaceNotSpecified()
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

    CollectionAssert.AreEqual(
      neighborTableEntries,
      await resolver.EnumerateNeighborTableEntriesAsync().ToListAsync()
    );
  }

  [Test]
  public async Task EnumerateNeighborTableEntriesAsync_NetworkInterfaceSpecified()
  {
    var neighborTableEntries = new NeighborTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, NeighborTableEntryState.None, "wlan0"),
      new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, NeighborTableEntryState.None, "wlan1"),
      new(IPAddress.Parse("192.168.2.2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, NeighborTableEntryState.None, "wlan2")
    };

    using var resolver = new MacAddressResolver(
      networkInterface: new PseudoNetworkInterface("wlan1", supportsIPv4: true, supportsIPv6: true),
      neighborTable: new StaticNeighborTable(neighborTableEntries),
      neighborDiscoverer: new NullNeighborDiscoverer()
    );

    CollectionAssert.AreEqual(
      neighborTableEntries.Skip(1).Take(1),
      await resolver.EnumerateNeighborTableEntriesAsync().ToListAsync()
    );
  }

  [Test]
  public async Task EnumerateNeighborTableEntriesAsync_NetworkInterfaceSpecified_CaseSensitivityOfInterfaceId()
  {
    var neighborTableEntries = new NeighborTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, NeighborTableEntryState.None, "wlan0"),
      new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, NeighborTableEntryState.None, "WLan0"),
      new(IPAddress.Parse("192.168.2.2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, NeighborTableEntryState.None, "WLAN0")
    };

    using var resolver = new MacAddressResolver(
      networkInterface: new PseudoNetworkInterface("wlan0", supportsIPv4: true, supportsIPv6: true),
      neighborTable: new StaticNeighborTable(neighborTableEntries),
      neighborDiscoverer: new NullNeighborDiscoverer()
    );

    CollectionAssert.AreEqual(
      RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? neighborTableEntries
        : neighborTableEntries.Take(1),
      await resolver.EnumerateNeighborTableEntriesAsync().ToListAsync()
    );
  }

  [Test]
  public async Task EnumerateNeighborTableEntriesAsync_NetworkInterfaceSupportsIPv4Only()
  {
    const string iface = "wlan0";

    var neighborTableEntries = new NeighborTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, NeighborTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, NeighborTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, NeighborTableEntryState.None, iface)
    };

    using var resolver = new MacAddressResolver(
      networkInterface: new PseudoNetworkInterface(iface, supportsIPv4: true, supportsIPv6: false),
      neighborTable: new StaticNeighborTable(neighborTableEntries),
      neighborDiscoverer: new NullNeighborDiscoverer()
    );

    CollectionAssert.AreEqual(
      neighborTableEntries.Where(static entry => entry.IPAddress!.AddressFamily == AddressFamily.InterNetwork),
      await resolver.EnumerateNeighborTableEntriesAsync().ToListAsync()
    );
  }

  [Test]
  public async Task EnumerateNeighborTableEntriesAsync_NetworkInterfaceSupportsIPv6Only()
  {
    const string iface = "wlan0";

    var neighborTableEntries = new NeighborTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, NeighborTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, NeighborTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, NeighborTableEntryState.None, iface)
    };

    using var resolver = new MacAddressResolver(
      networkInterface: new PseudoNetworkInterface(iface, supportsIPv4: false, supportsIPv6: true),
      neighborTable: new StaticNeighborTable(neighborTableEntries),
      neighborDiscoverer: new NullNeighborDiscoverer()
    );

    CollectionAssert.AreEqual(
      neighborTableEntries.Where(static entry => entry.IPAddress!.AddressFamily == AddressFamily.InterNetworkV6),
      await resolver.EnumerateNeighborTableEntriesAsync().ToListAsync()
    );
  }

  [Test]
  public async Task EnumerateNeighborTableEntriesAsync_NetworkInterfaceSupportsBothIPv4AndIPv6()
  {
    const string iface = "wlan0";

    var neighborTableEntries = new NeighborTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, NeighborTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, NeighborTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, NeighborTableEntryState.None, iface)
    };

    using var resolver = new MacAddressResolver(
      networkInterface: new PseudoNetworkInterface(iface, supportsIPv4: true, supportsIPv6: true),
      neighborTable: new StaticNeighborTable(neighborTableEntries),
      neighborDiscoverer: new NullNeighborDiscoverer()
    );

    CollectionAssert.AreEqual(
      neighborTableEntries,
      await resolver.EnumerateNeighborTableEntriesAsync().ToListAsync()
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_EnumerateNeighborTableEntriesAsync_Empty()
  {
    yield return new object[] { null! };
    yield return new object[] { new PseudoNetworkInterface("wlan0") };
  }

  [TestCaseSource(nameof(YieldTestCases_EnumerateNeighborTableEntriesAsync_Empty))]
  public async Task EnumerateNeighborTableEntriesAsync_Empty(NetworkInterface iface)
  {
    using var resolver = new MacAddressResolver(
      networkInterface: iface,
      neighborTable: new StaticNeighborTable(Array.Empty<NeighborTableEntry>()),
      neighborDiscoverer: new NullNeighborDiscoverer()
    );

    CollectionAssert.IsEmpty(
      await resolver.EnumerateNeighborTableEntriesAsync().ToListAsync()
    );
  }

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

  private static System.Collections.IEnumerable YieldTestCases_EnumerateNeighborTableEntriesAsync_Predicate()
  {
    var entry0 = new NeighborTableEntry(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, NeighborTableEntryState.None, "wlan0");
    var entry1 = new NeighborTableEntry(IPAddress.Parse("2001:db8::1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, NeighborTableEntryState.Reachable, "wlan1");
    var entry2 = new NeighborTableEntry(IPAddress.Parse("2001:db8::2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), false, NeighborTableEntryState.Delay, "wlan2");
    var entries = new[] {
      entry0,
      entry1,
      entry2,
    };
    var iface = new PseudoNetworkInterface("wlan0");

    yield return new object[] {
      null!,
      entries,
      new Predicate<NeighborTableEntry>(static e => true),
      entries,
      "all true",
    };

    yield return new object[] {
      iface,
      entries,
      new Predicate<NeighborTableEntry>(static e => true),
      entries,
      "all true (NetworkInterface must not affect)",
    };

    yield return new object[] {
      null!,
      entries,
      new Predicate<NeighborTableEntry>(static e => false),
      Array.Empty<NeighborTableEntry>(),
      "all false",
    };

    yield return new object[] {
      null!,
      entries,
      new Predicate<NeighborTableEntry>(static e => e.IsPermanent == true),
      new[] { entry0 },
      "IsPermanent",
    };

    yield return new object[] {
      null!,
      entries,
      new Predicate<NeighborTableEntry>(static e => e.State != NeighborTableEntryState.None),
      new[] { entry1, entry2 },
      "State",
    };
  }

  [TestCaseSource(nameof(YieldTestCases_EnumerateNeighborTableEntriesAsync_Predicate))]
  public async Task EnumerateNeighborTableEntriesAsync_Predicate(
    NetworkInterface iface,
    IList<NeighborTableEntry> entries,
    Predicate<NeighborTableEntry> predicate,
    IEnumerable<NeighborTableEntry> expected,
    string message
  )
  {
    using var resolver = new MacAddressResolver(
      networkInterface: iface,
      neighborTable: new StaticNeighborTable(entries),
      neighborDiscoverer: new NullNeighborDiscoverer()
    );

    CollectionAssert.AreEqual(
      expected,
      await resolver.EnumerateNeighborTableEntriesAsync(predicate: predicate).ToListAsync(),
      message
    );
  }

  [Test]
  public void EnumerateNeighborTableEntriesAsync_Predicate_Null()
  {
    using var resolver = new MacAddressResolver(
      networkInterface: null,
      neighborTable: new StaticNeighborTable(new[] { default(NeighborTableEntry) } ),
      neighborDiscoverer: new NullNeighborDiscoverer()
    );

    Assert.Throws<ArgumentNullException>(() => resolver.EnumerateNeighborTableEntriesAsync(predicate: null!));

    Assert.ThrowsAsync<ArgumentNullException>(
      async () => {
        await foreach (var entry in resolver.EnumerateNeighborTableEntriesAsync(predicate: null!)) {
          Assert.Fail("must not be enumerated");
        }
      }
    );
  }
}
