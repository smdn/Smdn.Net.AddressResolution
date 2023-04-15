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
  public void RefreshInvalidatedAddressesAsync_NoNetworkScannerProvided()
  {
    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: null
    );

    Assert.IsFalse(resolver.CanPerformNetworkScan, nameof(resolver.CanPerformNetworkScan));
    Assert.ThrowsAsync<InvalidOperationException>(async () => await resolver.RefreshInvalidatedAddressesAsync());
  }

  [Test]
  public void RefreshInvalidatedAddressesAsync_MacAddressInvalidated()
  {
    var scanner = new PseudoNetworkScanner();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: scanner
    );

    resolver.Invalidate(TestMacAddress);

    Assert.IsTrue(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshInvalidatedAddressesAsync());

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.IsTrue(scanner.FullScanRequested, nameof(scanner.FullScanRequested));
    Assert.IsFalse(scanner.PartialScanRequested, nameof(scanner.PartialScanRequested));
    CollectionAssert.IsEmpty(
      scanner.ScanRequestedAddresses,
      nameof(scanner.ScanRequestedAddresses)
    );
  }

  [Test]
  public void RefreshInvalidatedAddressesAsync_IPAddressInvalidated()
  {
    var scanner = new PseudoNetworkScanner();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: scanner
    );

    resolver.Invalidate(TestIPAddress);

    Assert.IsTrue(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshInvalidatedAddressesAsync());

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.IsFalse(scanner.FullScanRequested, nameof(scanner.FullScanRequested));
    Assert.IsTrue(scanner.PartialScanRequested, nameof(scanner.PartialScanRequested));
    CollectionAssert.AreEqual(
      new[] { TestIPAddress },
      scanner.ScanRequestedAddresses,
      nameof(scanner.ScanRequestedAddresses)
    );
  }

  [Test]
  public void RefreshInvalidatedAddressesAsync_NothingInvalidated()
  {
    var scanner = new PseudoNetworkScanner();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: scanner
    );

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshInvalidatedAddressesAsync());

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.IsFalse(scanner.FullScanRequested, nameof(scanner.FullScanRequested));
    Assert.IsFalse(scanner.PartialScanRequested, nameof(scanner.PartialScanRequested));
    CollectionAssert.IsEmpty(scanner.ScanRequestedAddresses, nameof(scanner.ScanRequestedAddresses));
  }

  [TestCase(1)]
  [TestCase(2)]
  [TestCase(5)]
  [TestCase(10)]
  public async Task RefreshInvalidatedAddressesAsync_MustPerformInsideOfSemaphoreCriticalSection(
    int parallelCountForRefreshInvalidatedAddresses
  )
  {
    const int numberOfParallelism = 20;

    var scanner = new PerformDelayNetworkScanner(delayOnNetworkScan: TimeSpan.FromMilliseconds(100));
    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: scanner,
      maxParallelCountForRefreshInvalidatedAddresses: parallelCountForRefreshInvalidatedAddresses
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

        resolver.Invalidate(TestIPAddress);

        await resolver.RefreshInvalidatedAddressesAsync(cancellationToken);
      }
    );

    Assert.Greater(scanner.NumberOfTasksPerformed, 1, nameof(scanner.NumberOfTasksPerformed));
    Assert.LessOrEqual(
      scanner.MaxNumberOfTasksPerformedInParallel,
      parallelCountForRefreshInvalidatedAddresses,
      nameof(scanner.MaxNumberOfTasksPerformedInParallel)
    );
  }
}
