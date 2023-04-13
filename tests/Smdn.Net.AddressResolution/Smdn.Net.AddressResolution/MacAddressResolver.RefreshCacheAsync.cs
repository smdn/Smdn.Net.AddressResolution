// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.AddressTables;
using Smdn.Net.NetworkScanning;

namespace Smdn.Net.AddressResolution;

partial class MacAddressResolverTests {
  [Test]
  public void RefreshCacheAsync_AddressInvalidated()
  {
    var scanner = new PseudoNetworkScanner();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: scanner
    );

    resolver.Invalidate(TestIPAddress);
    resolver.Invalidate(TestMacAddress);

    Assert.IsTrue(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshCacheAsync());

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.IsTrue(scanner.FullScanRequested, nameof(scanner.FullScanRequested));
    Assert.IsFalse(scanner.PartialScanRequested, nameof(scanner.PartialScanRequested));
    CollectionAssert.IsEmpty(
      scanner.ScanRequestedAddresses,
      nameof(scanner.ScanRequestedAddresses)
    );
  }

  [Test]
  public void RefreshCacheAsync_NothingInvalidated()
  {
    var scanner = new PseudoNetworkScanner();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: scanner
    );

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshCacheAsync());

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.IsTrue(scanner.FullScanRequested, nameof(scanner.FullScanRequested));
    Assert.IsFalse(scanner.PartialScanRequested, nameof(scanner.PartialScanRequested));
    CollectionAssert.IsEmpty(
      scanner.ScanRequestedAddresses,
      nameof(scanner.ScanRequestedAddresses)
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_RefreshCacheAsync_NetworkScanIntervalMustNotAffect()
  {
    yield return new object[] { TimeSpan.FromTicks(1) };
    yield return new object[] { TimeSpan.MaxValue };
    yield return new object[] { Timeout.InfiniteTimeSpan };
  }

  [TestCaseSource(nameof(YieldTestCases_RefreshCacheAsync_NetworkScanIntervalMustNotAffect))]
  public void RefreshCacheAsync_NetworkScanIntervalMustNotAffect(TimeSpan networkScanInterval)
  {
    var scanner = new PseudoNetworkScanner();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: scanner
    ) {
      NetworkScanInterval = networkScanInterval,
      NetworkScanMinInterval = TimeSpan.Zero
    };

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshCacheAsync(), $"{nameof(resolver.RefreshCacheAsync)} #1");

    Assert.IsTrue(scanner.FullScanRequested, $"{nameof(scanner.FullScanRequested)} #1");

    scanner.Reset();

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshCacheAsync(), $"{nameof(resolver.RefreshCacheAsync)} #2");

    Assert.IsTrue(scanner.FullScanRequested, $"{nameof(scanner.FullScanRequested)} #2");
  }

  [Test]
  public async Task RefreshCacheAsync_MustPerformInsideOfMutexCriticalSection()
  {
    const int numberOfParallelism = 20;

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
      source: Enumerable.Range(0, numberOfParallelism),
      parallelOptions: new() {
        MaxDegreeOfParallelism = -1,
        CancellationToken = cts.Token,
      },
      body: async (i, cancellationToken) => {
        await Task.Delay(TimeSpan.FromMilliseconds(10 * i), cancellationToken);
        await resolver.RefreshCacheAsync(cancellationToken);
      }
    );

    Assert.AreEqual(1, scanner.MaxNumberOfTasksPerformedInParallel, nameof(scanner.MaxNumberOfTasksPerformedInParallel));
    Assert.Greater(scanner.NumberOfTasksPerformed, 1, nameof(scanner.NumberOfTasksPerformed));
  }
}
