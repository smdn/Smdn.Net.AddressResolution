// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

partial class MacAddressResolverTests {
  private sealed class InterceptingNetworkScanner : INetworkScanner {
    private readonly Action actionBeforePerformScan;

    public InterceptingNetworkScanner(Action actionBeforePerformScan)
    {
      this.actionBeforePerformScan = actionBeforePerformScan;
    }

    public void Dispose()
    {
    }

    public ValueTask ScanAsync(CancellationToken cancellationToken)
    {
      actionBeforePerformScan();

      return default;
    }

    public ValueTask ScanAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken)
    {
      actionBeforePerformScan();

      return default;
    }
  }

  [Test]
  public async Task ResolveIPAddressToMacAddressAsync_PerformCacheRefresh()
  {
    var performedNetworkScan = false;

    using var resolver = new MacAddressResolver(
      addressTable: new NullAddressTable(),
      networkScanner: new InterceptingNetworkScanner(() => performedNetworkScan = true)
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = TimeSpan.FromMilliseconds(500),
    };

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.IsTrue(performedNetworkScan, "attempt initial resolution");

    // NetworkScanInterval not elapsed
    performedNetworkScan = false;

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.IsFalse(performedNetworkScan, "attempt resolution at the time the interval has not elapsed yet");

    // NetworkScanInterval elapsed
    await Task.Delay(resolver.NetworkScanInterval + TimeSpan.FromMilliseconds(100));

    performedNetworkScan = false;

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.IsTrue(performedNetworkScan, "attempt resolution at the time the interval has elapsed");
  }

  [Test]
  public async Task ResolveMacAddressToIPAddressAsync_PerformCacheRefresh()
  {
    var performedNetworkScan = false;

    using var resolver = new MacAddressResolver(
      addressTable: new NullAddressTable(),
      networkScanner: new InterceptingNetworkScanner(() => performedNetworkScan = true)
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = TimeSpan.FromMilliseconds(500),
    };

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.IsTrue(performedNetworkScan, "attempt initial resolution");

    // NetworkScanInterval not elapsed
    performedNetworkScan = false;

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.IsFalse(performedNetworkScan, "attempt resolution at the time the interval has not elapsed yet");

    // NetworkScanInterval elapsed
    await Task.Delay(resolver.NetworkScanInterval + TimeSpan.FromMilliseconds(100));

    performedNetworkScan = false;

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.IsTrue(performedNetworkScan, "attempt resolution at the time the interval has elapsed");
  }

  [Test]
  public async Task ResolveIPAddressToMacAddressAsync_PerformCacheRefresh_Never()
  {
    var performedNetworkScan = false;

    using var resolver = new MacAddressResolver(
      addressTable: new NullAddressTable(),
      networkScanner: new InterceptingNetworkScanner(() => performedNetworkScan = true)
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = Timeout.InfiniteTimeSpan,
    };

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.IsFalse(performedNetworkScan, "attempt initial resolution");

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.IsFalse(performedNetworkScan, "attempt second resolution");
  }

  [Test]
  public async Task ResolveMacAddressToIPAddressAsync_PerformCacheRefresh_Never()
  {
    var performedNetworkScan = false;

    using var resolver = new MacAddressResolver(
      addressTable: new NullAddressTable(),
      networkScanner: new InterceptingNetworkScanner(() => performedNetworkScan = true)
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = Timeout.InfiniteTimeSpan,
    };

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.IsFalse(performedNetworkScan, "attempt initial resolution");

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.IsFalse(performedNetworkScan, "attempt second resolution");
  }

  [Test]
  public void ResolveIPAddressToMacAddressAsync_CancellationRequested()
  {
    using var cts = new CancellationTokenSource();
    using var resolver = new MacAddressResolver(
      addressTable: new NullAddressTable(),
      networkScanner: new InterceptingNetworkScanner(() => cts.Cancel())
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = TimeSpan.FromTicks(1),
    };

    var ex = Assert.CatchAsync(
      async () => await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress, cts.Token)
    );

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }

  [Test]
  public void ResolveMacAddressToIPAddressAsync_CancellationRequested()
  {
    using var cts = new CancellationTokenSource();
    using var resolver = new MacAddressResolver(
      addressTable: new NullAddressTable(),
      networkScanner: new InterceptingNetworkScanner(() => cts.Cancel())
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = TimeSpan.FromTicks(1),
    };

    var ex = Assert.CatchAsync(
      async () => await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress, cts.Token)
    );

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }

  private static MacAddressResolver CreateNullNetworkScannerMacAddressResolver(
    IAddressTable addressTable,
    string? networkInterfaceId = null
  )
    => new(
      networkInterface: networkInterfaceId is null ? null : new PseudoNetworkInterface(networkInterfaceId, true, true),
      addressTable: addressTable,
      networkScanner: new NullNetworkScanner()
    ) {
      NetworkScanInterval = Timeout.InfiniteTimeSpan,
    };

  private static System.Collections.IEnumerable YieldTestCases_ResolveAsync_NoCandidatesEnumerated()
  {
    yield return new object[] {
      new NullAddressTable()
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      )
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_NoCandidatesEnumerated))]
  public async Task ResolveIPAddressToMacAddressAsync_NoCandidatesEnumerated(IAddressTable addressTable)
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.IsNull(await resolver.ResolveIPAddressToMacAddressAsync(IPAddress.Parse("192.168.2.255")));
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_NoCandidatesEnumerated))]
  public async Task ResolveMacAddressToIPAddressAsync_NoCandidatesEnumerated(IAddressTable addressTable)
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.IsNull(await resolver.ResolveMacAddressToIPAddressAsync(PhysicalAddress.Parse("00-00-5E-00-53-FF")));
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveAsync_UnresolvableEntriesMustBeExcluded()
  {
    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-00-00-00-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      )
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.1"), null, true, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      )
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_UnresolvableEntriesMustBeExcluded))]
  public async Task ResolveIPAddressToMacAddressAsync_UnresolvableEntriesMustBeExcluded(IAddressTable addressTable)
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.AreEqual(
      PhysicalAddress.Parse("00-00-5E-00-53-00"),
      await resolver.ResolveIPAddressToMacAddressAsync(IPAddress.Parse("192.168.2.0"))
    );
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_UnresolvableEntriesMustBeExcluded))]
  public async Task ResolveMacAddressToIPAddressAsync_UnresolvableEntriesMustBeExcluded(IAddressTable addressTable)
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.AreEqual(
      IPAddress.Parse("192.168.2.0"),
      await resolver.ResolveMacAddressToIPAddressAsync(PhysicalAddress.Parse("00-00-5E-00-53-00"))
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveIPAddressToMacAddressAsync_InvalidatedEntriesMustBeExcluded()
  {
    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] { PhysicalAddress.Parse("00-00-5E-00-53-01") },
      IPAddress.Parse("192.168.2.0"),
      PhysicalAddress.Parse("00-00-5E-00-53-00"),
      "case1"
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] {
        PhysicalAddress.Parse("00-00-5E-00-53-00"),
        PhysicalAddress.Parse("00-00-5E-00-53-01"),
      },
      IPAddress.Parse("192.168.2.0"),
      null!,
      "case2"
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] { PhysicalAddress.Parse("00-00-5E-00-53-00") },
      IPAddress.Parse("192.168.2.0"),
      null!,
      "case3"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveIPAddressToMacAddressAsync_InvalidatedEntriesMustBeExcluded))]
  public async Task ResolveIPAddressToMacAddressAsync_InvalidatedEntriesMustBeExcluded(
    IAddressTable addressTable,
    PhysicalAddress[] addressesToBeInvalidated,
    IPAddress addressToResolve,
    PhysicalAddress? expected,
    string message
  )
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    foreach (var addressToBeInvalidated in addressesToBeInvalidated) {
      resolver.Invalidate(addressToBeInvalidated);
    }

    Assert.AreEqual(
      expected,
      await resolver.ResolveIPAddressToMacAddressAsync(addressToResolve),
      message
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveMacAddressToIPAddressAsync_InvalidatedEntriesMustBeExcluded()
  {
    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] { IPAddress.Parse("192.168.2.1") },
      PhysicalAddress.Parse("00-00-5E-00-53-00"),
      IPAddress.Parse("192.168.2.0"),
      "case1"
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] {
        IPAddress.Parse("192.168.2.0"),
        IPAddress.Parse("192.168.2.1"),
      },
      PhysicalAddress.Parse("00-00-5E-00-53-00"),
      null!,
      "case2"
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] { IPAddress.Parse("192.168.2.0") },
      PhysicalAddress.Parse("00-00-5E-00-53-00"),
      null!,
      "case3"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveMacAddressToIPAddressAsync_InvalidatedEntriesMustBeExcluded))]
  public async Task ResolveMacAddressToIPAddressAsync_InvalidatedEntriesMustBeExcluded(
    IAddressTable addressTable,
    IPAddress[] addressesToBeInvalidated,
    PhysicalAddress addressToResolve,
    IPAddress? expected,
    string message
  )
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    foreach (var addressToBeInvalidated in addressesToBeInvalidated) {
      resolver.Invalidate(addressToBeInvalidated);
    }

    Assert.AreEqual(
      expected,
      await resolver.ResolveMacAddressToIPAddressAsync(addressToResolve),
      message
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveAsync_EntriesIrrelevantToNetworkInterfaceMustBeExcluded()
  {
    yield return new object[] {
      "wlan1",
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, "wlan1"),
          new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan1"),
        }
      ),
      new AddressTableEntry(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, null),
      new AddressTableEntry(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, "wlan1"),
    };

    yield return new object[] {
      null!,
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, "wlan1"),
          new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan1"),
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new AddressTableEntry(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, null),
      // last entry must be selected
      new AddressTableEntry(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_EntriesIrrelevantToNetworkInterfaceMustBeExcluded))]
  public async Task ResolveIPAddressToMacAddressAsync_EntriesIrrelevantToNetworkInterfaceMustBeExcluded(
    string? networkInterfaceId,
    IAddressTable addressTable,
    AddressTableEntry entryToResolve,
    AddressTableEntry expectedEntry
  )
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable, networkInterfaceId);

    Assert.AreEqual(
      expectedEntry.PhysicalAddress,
      await resolver.ResolveIPAddressToMacAddressAsync(entryToResolve.IPAddress!)
    );
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_EntriesIrrelevantToNetworkInterfaceMustBeExcluded))]
  public async Task ResolveMacAddressToIPAddressAsync_EntriesIrrelevantToNetworkInterfaceMustBeExcluded(
    string? networkInterfaceId,
    IAddressTable addressTable,
    AddressTableEntry entryToResolve,
    AddressTableEntry expectedEntry
  )
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable, networkInterfaceId);

    Assert.AreEqual(
      expectedEntry.IPAddress,
      await resolver.ResolveMacAddressToIPAddressAsync(entryToResolve.PhysicalAddress!)
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveAsync_PrioritizePermanentEntry()
  {
    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-FF"), true, AddressTableEntryState.None, null),
          new(IPAddress.Parse("192.168.2.255"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, AddressTableEntryState.None, null),
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, null),
          new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, null),
        }
      ),
      new AddressTableEntry(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, null),
      new AddressTableEntry(IPAddress.Parse("192.168.2.255"), PhysicalAddress.Parse("00-00-5E-00-53-FF"), true, AddressTableEntryState.None, null),
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_PrioritizePermanentEntry))]
  public async Task ResolveIPAddressToMacAddressAsync_PrioritizePermanentEntry(
    IAddressTable addressTable,
    AddressTableEntry entryToResolve,
    AddressTableEntry expectedEntry
  )
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.AreEqual(
      expectedEntry.PhysicalAddress,
      await resolver.ResolveIPAddressToMacAddressAsync(entryToResolve.IPAddress!)
    );
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_PrioritizePermanentEntry))]
  public async Task ResolveMacAddressToIPAddressAsync_PrioritizePermanentEntry(
    IAddressTable addressTable,
    AddressTableEntry entryToResolve,
    AddressTableEntry expectedEntry
  )
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.AreEqual(
      expectedEntry.IPAddress,
      await resolver.ResolveMacAddressToIPAddressAsync(entryToResolve.PhysicalAddress!)
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveAsync_PrioritizeReachableEntry()
  {
    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-FF"), false, AddressTableEntryState.Reachable, null),
          new(IPAddress.Parse("192.168.2.255"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.Reachable, null),
          new(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.Stale, null),
          new(IPAddress.Parse("192.168.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.Stale, null),
        }
      ),
      new AddressTableEntry(IPAddress.Parse("192.168.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, null),
      new AddressTableEntry(IPAddress.Parse("192.168.2.255"), PhysicalAddress.Parse("00-00-5E-00-53-FF"), false, AddressTableEntryState.Reachable, null),
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_PrioritizeReachableEntry))]
  public async Task ResolveIPAddressToMacAddressAsync_PrioritizeReachableEntry(
    IAddressTable addressTable,
    AddressTableEntry entryToResolve,
    AddressTableEntry expectedEntry
  )
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.AreEqual(
      expectedEntry.PhysicalAddress,
      await resolver.ResolveIPAddressToMacAddressAsync(entryToResolve.IPAddress!)
    );
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_PrioritizeReachableEntry))]
  public async Task ResolveMacAddressToIPAddressAsync_PrioritizeReachableEntry(
    IAddressTable addressTable,
    AddressTableEntry entryToResolve,
    AddressTableEntry expectedEntry
  )
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.AreEqual(
      expectedEntry.IPAddress,
      await resolver.ResolveMacAddressToIPAddressAsync(entryToResolve.PhysicalAddress!)
    );
  }
}
