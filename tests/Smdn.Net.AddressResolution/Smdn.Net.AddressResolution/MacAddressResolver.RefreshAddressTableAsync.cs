// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.AddressTables;
using Smdn.Net.NetworkScanning;

namespace Smdn.Net.AddressResolution;

partial class MacAddressResolverTests {
  [Test]
  public void RefreshAddressTableAsync_NoNetworkScannerProvided()
  {
    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: null
    );

    Assert.That(resolver.CanPerformNetworkScan, Is.False, nameof(resolver.CanPerformNetworkScan));
    Assert.ThrowsAsync<InvalidOperationException>(async () => await resolver.RefreshAddressTableAsync());
  }

  [Test]
  public void RefreshAddressTableAsync_AddressInvalidated()
  {
    var scanner = new PseudoNetworkScanner();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: scanner
    );

    resolver.Invalidate(TestIPAddress);
    resolver.Invalidate(TestMacAddress);

    Assert.That(resolver.HasInvalidated, Is.True, $"{nameof(resolver.HasInvalidated)} before refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshAddressTableAsync());

    Assert.That(resolver.HasInvalidated, Is.False, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.That(scanner.FullScanRequested, Is.True, nameof(scanner.FullScanRequested));
    Assert.That(scanner.PartialScanRequested, Is.False, nameof(scanner.PartialScanRequested));
    Assert.That(scanner.ScanRequestedAddresses, Is.Empty, nameof(scanner.ScanRequestedAddresses));
  }

  [Test]
  public void RefreshAddressTableAsync_NothingInvalidated()
  {
    var scanner = new PseudoNetworkScanner();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: scanner
    );

    Assert.That(resolver.HasInvalidated, Is.False, $"{nameof(resolver.HasInvalidated)} before refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshAddressTableAsync());

    Assert.That(resolver.HasInvalidated, Is.False, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.That(scanner.FullScanRequested, Is.True, nameof(scanner.FullScanRequested));
    Assert.That(scanner.PartialScanRequested, Is.False, nameof(scanner.PartialScanRequested));
    Assert.That(
      scanner.ScanRequestedAddresses, Is.Empty,
      nameof(scanner.ScanRequestedAddresses)
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_RefreshAddressTableAsync_NetworkScanIntervalMustNotAffect()
  {
    yield return new object[] { TimeSpan.FromTicks(1) };
    yield return new object[] { TimeSpan.MaxValue };
    yield return new object[] { Timeout.InfiniteTimeSpan };
  }

  [TestCaseSource(nameof(YieldTestCases_RefreshAddressTableAsync_NetworkScanIntervalMustNotAffect))]
  public void RefreshAddressTableAsync_NetworkScanIntervalMustNotAffect(TimeSpan networkScanInterval)
  {
    var scanner = new PseudoNetworkScanner();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: scanner
    ) {
      NetworkScanInterval = networkScanInterval,
      NetworkScanMinInterval = TimeSpan.Zero
    };

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshAddressTableAsync(), $"{nameof(resolver.RefreshAddressTableAsync)} #1");

    Assert.That(scanner.FullScanRequested, Is.True, $"{nameof(scanner.FullScanRequested)} #1");

    scanner.Reset();

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshAddressTableAsync(), $"{nameof(resolver.RefreshAddressTableAsync)} #2");

    Assert.That(scanner.FullScanRequested, Is.True, $"{nameof(scanner.FullScanRequested)} #2");
  }

  [Test]
  public async Task RefreshAddressTableAsync_MustPerformInsideOfMutexCriticalSection()
  {
    const int NumberOfParallelism = 20;

    var scanner = new PerformDelayNetworkScanner(delayOnNetworkScan: TimeSpan.FromMilliseconds(100));
    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: scanner
    ) {
      NetworkScanInterval = Timeout.InfiniteTimeSpan,
      NetworkScanMinInterval = TimeSpan.Zero
    };

    using var cts = new CancellationTokenSource(delay: TimeSpan.FromSeconds(10.0));

    await Parallel.ForEachAsync(
      source: Enumerable.Range(0, NumberOfParallelism),
      parallelOptions: new() {
        MaxDegreeOfParallelism = -1,
        CancellationToken = cts.Token,
      },
      body: async (i, cancellationToken) => {
        await Task.Delay(TimeSpan.FromMilliseconds(10 * i), cancellationToken);
        await resolver.RefreshAddressTableAsync(cancellationToken);
      }
    );

    Assert.That(scanner.MaxNumberOfTasksPerformedInParallel, Is.EqualTo(1), nameof(scanner.MaxNumberOfTasksPerformedInParallel));
    Assert.That(scanner.NumberOfTasksPerformed, Is.GreaterThan(1), nameof(scanner.NumberOfTasksPerformed));
  }
}
