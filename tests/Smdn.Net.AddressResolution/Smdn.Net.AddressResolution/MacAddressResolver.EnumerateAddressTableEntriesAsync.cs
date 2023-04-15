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

using Smdn.Net.AddressTables;
using Smdn.Net.NetworkScanning;

namespace Smdn.Net.AddressResolution;

partial class MacAddressResolverTests {
  [Test]
  public void EnumerateAddressTableEntriesAsync_Disposed()
  {
    using var resolver = new MacAddressResolver(
      networkInterface: null,
      addressTable: new StaticAddressTable(new[] { default(AddressTableEntry) } ),
      networkScanner: NetworkScanner.Null
    );

    resolver.Dispose();

    Assert.Throws<ObjectDisposedException>(
      () => resolver.EnumerateAddressTableEntriesAsync()
    );
    Assert.Throws<ObjectDisposedException>(
      () => resolver.EnumerateAddressTableEntriesAsync(predicate: static _ => true)
    );

    Assert.ThrowsAsync<ObjectDisposedException>(
      async () => {
        await foreach (var entry in resolver.EnumerateAddressTableEntriesAsync()) {
          Assert.Fail("must not be enumerated");
        }
      }
    );
    Assert.ThrowsAsync<ObjectDisposedException>(
      async () => {
        await foreach (var entry in resolver.EnumerateAddressTableEntriesAsync(predicate: static _ => true)) {
          Assert.Fail("must not be enumerated");
        }
      }
    );
  }

  [Test]
  public async Task EnumerateAddressTableEntriesAsync_NetworkInterfaceNotSpecified()
  {
    var addressTableEntries = new AddressTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, AddressTableEntryState.None, "wlan0"),
      new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, AddressTableEntryState.None, "wlan1"),
      new(IPAddress.Parse("192.168.2.2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, AddressTableEntryState.None, "wlan2")
    };

    using var resolver = new MacAddressResolver(
      networkInterface: null,
      addressTable: new StaticAddressTable(addressTableEntries),
      networkScanner: NetworkScanner.Null
    );

    CollectionAssert.AreEqual(
      addressTableEntries,
      await resolver.EnumerateAddressTableEntriesAsync().ToListAsync()
    );
  }

  [Test]
  public async Task EnumerateAddressTableEntriesAsync_NetworkInterfaceSpecified()
  {
    var addressTableEntries = new AddressTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, AddressTableEntryState.None, "wlan0"),
      new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, AddressTableEntryState.None, "wlan1"),
      new(IPAddress.Parse("192.168.2.2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, AddressTableEntryState.None, "wlan2")
    };

    using var resolver = new MacAddressResolver(
      networkInterface: new PseudoNetworkInterface("wlan1", supportsIPv4: true, supportsIPv6: true),
      addressTable: new StaticAddressTable(addressTableEntries),
      networkScanner: NetworkScanner.Null
    );

    CollectionAssert.AreEqual(
      addressTableEntries.Skip(1).Take(1),
      await resolver.EnumerateAddressTableEntriesAsync().ToListAsync()
    );
  }

  [Test]
  public async Task EnumerateAddressTableEntriesAsync_NetworkInterfaceSpecified_CaseSensitivityOfInterfaceId()
  {
    var addressTableEntries = new AddressTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, AddressTableEntryState.None, "wlan0"),
      new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, AddressTableEntryState.None, "WLan0"),
      new(IPAddress.Parse("192.168.2.2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, AddressTableEntryState.None, "WLAN0")
    };

    using var resolver = new MacAddressResolver(
      networkInterface: new PseudoNetworkInterface("wlan0", supportsIPv4: true, supportsIPv6: true),
      addressTable: new StaticAddressTable(addressTableEntries),
      networkScanner: NetworkScanner.Null
    );

    CollectionAssert.AreEqual(
      RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? addressTableEntries
        : addressTableEntries.Take(1),
      await resolver.EnumerateAddressTableEntriesAsync().ToListAsync()
    );
  }

  [Test]
  public async Task EnumerateAddressTableEntriesAsync_NetworkInterfaceSupportsIPv4Only()
  {
    const string iface = "wlan0";

    var addressTableEntries = new AddressTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, AddressTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, AddressTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, AddressTableEntryState.None, iface)
    };

    using var resolver = new MacAddressResolver(
      networkInterface: new PseudoNetworkInterface(iface, supportsIPv4: true, supportsIPv6: false),
      addressTable: new StaticAddressTable(addressTableEntries),
      networkScanner: NetworkScanner.Null
    );

    CollectionAssert.AreEqual(
      addressTableEntries.Where(static entry => entry.IPAddress!.AddressFamily == AddressFamily.InterNetwork),
      await resolver.EnumerateAddressTableEntriesAsync().ToListAsync()
    );
  }

  [Test]
  public async Task EnumerateAddressTableEntriesAsync_NetworkInterfaceSupportsIPv6Only()
  {
    const string iface = "wlan0";

    var addressTableEntries = new AddressTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, AddressTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, AddressTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, AddressTableEntryState.None, iface)
    };

    using var resolver = new MacAddressResolver(
      networkInterface: new PseudoNetworkInterface(iface, supportsIPv4: false, supportsIPv6: true),
      addressTable: new StaticAddressTable(addressTableEntries),
      networkScanner: NetworkScanner.Null
    );

    CollectionAssert.AreEqual(
      addressTableEntries.Where(static entry => entry.IPAddress!.AddressFamily == AddressFamily.InterNetworkV6),
      await resolver.EnumerateAddressTableEntriesAsync().ToListAsync()
    );
  }

  [Test]
  public async Task EnumerateAddressTableEntriesAsync_NetworkInterfaceSupportsBothIPv4AndIPv6()
  {
    const string iface = "wlan0";

    var addressTableEntries = new AddressTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, AddressTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, AddressTableEntryState.None, iface),
      new(IPAddress.Parse("2001:db8::2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, AddressTableEntryState.None, iface)
    };

    using var resolver = new MacAddressResolver(
      networkInterface: new PseudoNetworkInterface(iface, supportsIPv4: true, supportsIPv6: true),
      addressTable: new StaticAddressTable(addressTableEntries),
      networkScanner: NetworkScanner.Null
    );

    CollectionAssert.AreEqual(
      addressTableEntries,
      await resolver.EnumerateAddressTableEntriesAsync().ToListAsync()
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_EnumerateAddressTableEntriesAsync_Empty()
  {
    yield return new object[] { null! };
    yield return new object[] { new PseudoNetworkInterface("wlan0") };
  }

  [TestCaseSource(nameof(YieldTestCases_EnumerateAddressTableEntriesAsync_Empty))]
  public async Task EnumerateAddressTableEntriesAsync_Empty(NetworkInterface iface)
  {
    using var resolver = new MacAddressResolver(
      networkInterface: iface,
      addressTable: new StaticAddressTable(Array.Empty<AddressTableEntry>()),
      networkScanner: NetworkScanner.Null
    );

    CollectionAssert.IsEmpty(
      await resolver.EnumerateAddressTableEntriesAsync().ToListAsync()
    );
  }

  [Test]
  public void EnumerateAddressTableEntriesAsync_CancellationRequested()
  {
    var addressTableEntries = new AddressTableEntry[] {
      new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, AddressTableEntryState.None, "wlan0"),
      new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), true, AddressTableEntryState.None, "wlan1"),
      new(IPAddress.Parse("192.168.2.2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), true, AddressTableEntryState.None, "wlan2")
    };

    using var resolver = new MacAddressResolver(
      networkInterface: null,
      addressTable: new StaticAddressTable(addressTableEntries),
      networkScanner: NetworkScanner.Null
    );
    using var cts = new CancellationTokenSource();
    Predicate<AddressTableEntry> predicate = e => {
      Assert.IsFalse(cts.IsCancellationRequested);

      if (e.InterfaceId == "wlan0")
        cts.Cancel();

      return true;
    };
    int count = 0;

    var ex = Assert.CatchAsync(
      async () => {
        await foreach (var entry in resolver.EnumerateAddressTableEntriesAsync(predicate, cts.Token)) {
          count++;
        }
      }
    );

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
    Assert.AreEqual(1, count, nameof(count));
  }

  private static System.Collections.IEnumerable YieldTestCases_EnumerateAddressTableEntriesAsync_Predicate()
  {
    var entry0 = new AddressTableEntry(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, AddressTableEntryState.None, "wlan0");
    var entry1 = new AddressTableEntry(IPAddress.Parse("2001:db8::1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.Reachable, "wlan1");
    var entry2 = new AddressTableEntry(IPAddress.Parse("2001:db8::2"), PhysicalAddress.Parse("00-00-5E-00-53-02"), false, AddressTableEntryState.Delay, "wlan2");
    var entries = new[] {
      entry0,
      entry1,
      entry2,
    };
    var iface = new PseudoNetworkInterface("wlan0");

    yield return new object[] {
      null!,
      entries,
      new Predicate<AddressTableEntry>(static e => true),
      entries,
      "all true",
    };

    yield return new object[] {
      iface,
      entries,
      new Predicate<AddressTableEntry>(static e => true),
      entries,
      "all true (NetworkInterface must not affect)",
    };

    yield return new object[] {
      null!,
      entries,
      new Predicate<AddressTableEntry>(static e => false),
      Array.Empty<AddressTableEntry>(),
      "all false",
    };

    yield return new object[] {
      null!,
      entries,
      new Predicate<AddressTableEntry>(static e => e.IsPermanent == true),
      new[] { entry0 },
      "IsPermanent",
    };

    yield return new object[] {
      null!,
      entries,
      new Predicate<AddressTableEntry>(static e => e.State != AddressTableEntryState.None),
      new[] { entry1, entry2 },
      "State",
    };
  }

  [TestCaseSource(nameof(YieldTestCases_EnumerateAddressTableEntriesAsync_Predicate))]
  public async Task EnumerateAddressTableEntriesAsync_Predicate(
    NetworkInterface iface,
    IList<AddressTableEntry> entries,
    Predicate<AddressTableEntry> predicate,
    IEnumerable<AddressTableEntry> expected,
    string message
  )
  {
    using var resolver = new MacAddressResolver(
      networkInterface: iface,
      addressTable: new StaticAddressTable(entries),
      networkScanner: NetworkScanner.Null
    );

    CollectionAssert.AreEqual(
      expected,
      await resolver.EnumerateAddressTableEntriesAsync(predicate: predicate).ToListAsync(),
      message
    );
  }

  [Test]
  public void EnumerateAddressTableEntriesAsync_Predicate_Null()
  {
    using var resolver = new MacAddressResolver(
      networkInterface: null,
      addressTable: new StaticAddressTable(new[] { default(AddressTableEntry) } ),
      networkScanner: NetworkScanner.Null
    );

    Assert.Throws<ArgumentNullException>(() => resolver.EnumerateAddressTableEntriesAsync(predicate: null!));

    Assert.ThrowsAsync<ArgumentNullException>(
      async () => {
        await foreach (var entry in resolver.EnumerateAddressTableEntriesAsync(predicate: null!)) {
          Assert.Fail("must not be enumerated");
        }
      }
    );
  }
}
