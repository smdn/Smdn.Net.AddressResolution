// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.AddressTables;
using Smdn.Net.NetworkScanning;

namespace Smdn.Net.AddressResolution;

partial class MacAddressResolverTests {
  private sealed class InterceptingAddressTable : IAddressTable {
    private readonly Action actionBeforeEnumerateEntries;

    public InterceptingAddressTable(Action actionBeforeEnumerateEntries)
    {
      this.actionBeforeEnumerateEntries = actionBeforeEnumerateEntries;
    }

    public void Dispose()
    {
    }

#pragma warning disable CS1998
    public async IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsync(
      [EnumeratorCancellation] CancellationToken cancellationToken
    )
#pragma warning restore CS1998
    {
      actionBeforeEnumerateEntries();

      yield break;
    }
  }

  [Test]
  public void ResolveIPAddressToMacAddressAsync_CanNotPerformNetworkScan()
  {
    var addressTableEnumerated = false;

    using var resolver = new MacAddressResolver(
      addressTable: new InterceptingAddressTable(() => addressTableEnumerated = true),
      networkScanner: null
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = TimeSpan.FromTicks(1),
    };

    Assert.That(resolver.CanPerformNetworkScan, Is.False, nameof(resolver.CanPerformNetworkScan));

    Assert.DoesNotThrowAsync(async () => await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress));

    Assert.That(addressTableEnumerated, Is.True, nameof(addressTableEnumerated));
  }

  [Test]
  public void ResolveMacAddressToIPAddressAsync_CanNotPerformNetworkScan()
  {
    var addressTableEnumerated = false;

    using var resolver = new MacAddressResolver(
      addressTable: new InterceptingAddressTable(() => addressTableEnumerated = true),
      networkScanner: null
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = TimeSpan.FromTicks(1),
    };

    Assert.That(resolver.CanPerformNetworkScan, Is.False, nameof(resolver.CanPerformNetworkScan));

    Assert.DoesNotThrowAsync(async () => await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress));

    Assert.That(addressTableEnumerated, Is.True, nameof(addressTableEnumerated));
  }

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
      addressTable: AddressTable.Null,
      networkScanner: new InterceptingNetworkScanner(() => performedNetworkScan = true)
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = TimeSpan.FromMilliseconds(500),
    };

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.That(performedNetworkScan, Is.True, "attempt initial resolution");

    // NetworkScanInterval not elapsed
    performedNetworkScan = false;

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.That(performedNetworkScan, Is.False, "attempt resolution at the time the interval has not elapsed yet");

    // NetworkScanInterval elapsed
    await Task.Delay(resolver.NetworkScanInterval + TimeSpan.FromMilliseconds(100));

    performedNetworkScan = false;

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.That(performedNetworkScan, Is.True, "attempt resolution at the time the interval has elapsed");
  }

  [Test]
  public async Task ResolveMacAddressToIPAddressAsync_PerformCacheRefresh()
  {
    var performedNetworkScan = false;

    using var resolver = new MacAddressResolver(
      addressTable: AddressTable.Null,
      networkScanner: new InterceptingNetworkScanner(() => performedNetworkScan = true)
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = TimeSpan.FromMilliseconds(500),
    };

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.That(performedNetworkScan, Is.True, "attempt initial resolution");

    // NetworkScanInterval not elapsed
    performedNetworkScan = false;

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.That(performedNetworkScan, Is.False, "attempt resolution at the time the interval has not elapsed yet");

    // NetworkScanInterval elapsed
    await Task.Delay(resolver.NetworkScanInterval + TimeSpan.FromMilliseconds(100));

    performedNetworkScan = false;

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.That(performedNetworkScan, Is.True, "attempt resolution at the time the interval has elapsed");
  }

  [Test]
  public async Task ResolveIPAddressToMacAddressAsync_PerformCacheRefresh_Never()
  {
    var performedNetworkScan = false;

    using var resolver = new MacAddressResolver(
      addressTable: AddressTable.Null,
      networkScanner: new InterceptingNetworkScanner(() => performedNetworkScan = true)
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = Timeout.InfiniteTimeSpan,
    };

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.That(performedNetworkScan, Is.False, "attempt initial resolution");

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.That(performedNetworkScan, Is.False, "attempt second resolution");
  }

  [Test]
  public async Task ResolveMacAddressToIPAddressAsync_PerformCacheRefresh_Never()
  {
    var performedNetworkScan = false;

    using var resolver = new MacAddressResolver(
      addressTable: AddressTable.Null,
      networkScanner: new InterceptingNetworkScanner(() => performedNetworkScan = true)
    ) {
      NetworkScanMinInterval = TimeSpan.Zero,
      NetworkScanInterval = Timeout.InfiniteTimeSpan,
    };

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.That(performedNetworkScan, Is.False, "attempt initial resolution");

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.That(performedNetworkScan, Is.False, "attempt second resolution");
  }

  [Test]
  public void ResolveIPAddressToMacAddressAsync_CancellationRequested()
  {
    using var cts = new CancellationTokenSource();
    using var resolver = new MacAddressResolver(
      addressTable: AddressTable.Null,
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
      addressTable: AddressTable.Null,
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
      networkScanner: NetworkScanner.Null
    ) {
      NetworkScanInterval = Timeout.InfiniteTimeSpan,
    };

  private static System.Collections.IEnumerable YieldTestCases_ResolveAsync_NoCandidatesEnumerated()
  {
    yield return new object[] {
      AddressTable.Null
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      )
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_NoCandidatesEnumerated))]
  public async Task ResolveIPAddressToMacAddressAsync_NoCandidatesEnumerated(IAddressTable addressTable)
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.That(await resolver.ResolveIPAddressToMacAddressAsync(IPAddress.Parse("192.0.2.255")), Is.Null);
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_NoCandidatesEnumerated))]
  public async Task ResolveMacAddressToIPAddressAsync_NoCandidatesEnumerated(IAddressTable addressTable)
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.That(await resolver.ResolveMacAddressToIPAddressAsync(PhysicalAddress.Parse("00-00-5E-00-53-FF")), Is.Null);
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveAsync_UnresolvableEntriesMustBeExcluded()
  {
    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.1"), PhysicalAddress.Parse("00-00-00-00-00-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      )
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.1"), null, true, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      )
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_UnresolvableEntriesMustBeExcluded))]
  public async Task ResolveIPAddressToMacAddressAsync_UnresolvableEntriesMustBeExcluded(IAddressTable addressTable)
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.That(
      await resolver.ResolveIPAddressToMacAddressAsync(IPAddress.Parse("192.0.2.0")),
      Is.EqualTo(PhysicalAddress.Parse("00-00-5E-00-53-00"))
    );
  }

  [TestCaseSource(nameof(YieldTestCases_ResolveAsync_UnresolvableEntriesMustBeExcluded))]
  public async Task ResolveMacAddressToIPAddressAsync_UnresolvableEntriesMustBeExcluded(IAddressTable addressTable)
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(addressTable);

    Assert.That(
      await resolver.ResolveMacAddressToIPAddressAsync(PhysicalAddress.Parse("00-00-5E-00-53-00")),
      Is.EqualTo(IPAddress.Parse("192.0.2.0"))
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveIPAddressToMacAddressAsync_InvalidatedEntriesMustBeExcluded()
  {
    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] { PhysicalAddress.Parse("00-00-5E-00-53-01") },
      IPAddress.Parse("192.0.2.0"),
      PhysicalAddress.Parse("00-00-5E-00-53-00"),
      "case1"
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] {
        PhysicalAddress.Parse("00-00-5E-00-53-00"),
        PhysicalAddress.Parse("00-00-5E-00-53-01"),
      },
      IPAddress.Parse("192.0.2.0"),
      null!,
      "case2"
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] { PhysicalAddress.Parse("00-00-5E-00-53-00") },
      IPAddress.Parse("192.0.2.0"),
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

    Assert.That(
      await resolver.ResolveIPAddressToMacAddressAsync(addressToResolve),
      Is.EqualTo(expected),
      message
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveMacAddressToIPAddressAsync_InvalidatedEntriesMustBeExcluded()
  {
    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.0.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] { IPAddress.Parse("192.0.2.1") },
      PhysicalAddress.Parse("00-00-5E-00-53-00"),
      IPAddress.Parse("192.0.2.0"),
      "case1"
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.0.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] {
        IPAddress.Parse("192.0.2.0"),
        IPAddress.Parse("192.0.2.1"),
      },
      PhysicalAddress.Parse("00-00-5E-00-53-00"),
      null!,
      "case2"
    };

    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new[] { IPAddress.Parse("192.0.2.0") },
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

    Assert.That(
      await resolver.ResolveMacAddressToIPAddressAsync(addressToResolve),
      Is.EqualTo(expected),
      message
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveAsync_EntriesIrrelevantToNetworkInterfaceMustBeExcluded()
  {
    yield return new object[] {
      "wlan1",
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, "wlan1"),
          new(IPAddress.Parse("192.0.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan1"),
        }
      ),
      new AddressTableEntry(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, null),
      new AddressTableEntry(IPAddress.Parse("192.0.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, "wlan1"),
    };

    yield return new object[] {
      null!,
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, "wlan1"),
          new(IPAddress.Parse("192.0.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan1"),
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
        }
      ),
      new AddressTableEntry(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, null),
      // last entry must be selected
      new AddressTableEntry(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, "wlan0"),
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

    Assert.That(
      await resolver.ResolveIPAddressToMacAddressAsync(entryToResolve.IPAddress!),
      Is.EqualTo(expectedEntry.PhysicalAddress)
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

    Assert.That(
      await resolver.ResolveMacAddressToIPAddressAsync(entryToResolve.PhysicalAddress!),
      Is.EqualTo(expectedEntry.IPAddress)
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveAsync_PrioritizePermanentEntry()
  {
    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-FF"), true, AddressTableEntryState.None, null),
          new(IPAddress.Parse("192.0.2.255"), PhysicalAddress.Parse("00-00-5E-00-53-00"), true, AddressTableEntryState.None, null),
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.None, null),
          new(IPAddress.Parse("192.0.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, null),
        }
      ),
      new AddressTableEntry(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, null),
      new AddressTableEntry(IPAddress.Parse("192.0.2.255"), PhysicalAddress.Parse("00-00-5E-00-53-FF"), true, AddressTableEntryState.None, null),
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

    Assert.That(
      await resolver.ResolveIPAddressToMacAddressAsync(entryToResolve.IPAddress!),
      Is.EqualTo(expectedEntry.PhysicalAddress)
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

    Assert.That(
      await resolver.ResolveMacAddressToIPAddressAsync(entryToResolve.PhysicalAddress!),
      Is.EqualTo(expectedEntry.IPAddress)
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_ResolveAsync_PrioritizeReachableEntry()
  {
    yield return new object[] {
      new StaticAddressTable(
        new AddressTableEntry[] {
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-FF"), false, AddressTableEntryState.Reachable, null),
          new(IPAddress.Parse("192.0.2.255"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.Reachable, null),
          new(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-01"), false, AddressTableEntryState.Stale, null),
          new(IPAddress.Parse("192.0.2.1"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.Stale, null),
        }
      ),
      new AddressTableEntry(IPAddress.Parse("192.0.2.0"), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.None, null),
      new AddressTableEntry(IPAddress.Parse("192.0.2.255"), PhysicalAddress.Parse("00-00-5E-00-53-FF"), false, AddressTableEntryState.Reachable, null),
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

    Assert.That(
      await resolver.ResolveIPAddressToMacAddressAsync(entryToResolve.IPAddress!),
      Is.EqualTo(expectedEntry.PhysicalAddress)
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

    Assert.That(
      await resolver.ResolveMacAddressToIPAddressAsync(entryToResolve.PhysicalAddress!),
      Is.EqualTo(expectedEntry.IPAddress)
    );
  }

  [Test]
  public async Task ResolveIPAddressToMacAddressAsync_ShouldResolveIPv4MappedIPv6Address(
    [Values("127.0.0.1", "192.0.2.0")] string ipv4AddressString,
    [Values] bool shouldResolveIPv4MappedIPv6Address
  )
  {
    using var resolver = CreateNullNetworkScannerMacAddressResolver(
      new StaticAddressTable([
        new(IPAddress.Parse(ipv4AddressString), PhysicalAddress.Parse("00-00-5E-00-53-00"), false, AddressTableEntryState.Reachable, "wlan0"),
      ])
    );

    resolver.ShouldResolveIPv4MappedIPv6Address = shouldResolveIPv4MappedIPv6Address;

    Assert.That(
      await resolver.ResolveIPAddressToMacAddressAsync(IPAddress.Parse(ipv4AddressString)),
      Is.Not.Null
    );

    var ipv4MappedIPv6AddressString = $"::ffff:{ipv4AddressString}";

    Assert.That(
      await resolver.ResolveIPAddressToMacAddressAsync(IPAddress.Parse(ipv4MappedIPv6AddressString)),
      shouldResolveIPv4MappedIPv6Address
        ? Is.Not.Null
        : Is.Null
    );
  }
}
