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

    Assert.That(resolver.CanPerformNetworkScan, Is.False, nameof(resolver.CanPerformNetworkScan));
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

    Assert.That(resolver.HasInvalidated, Is.True, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshInvalidatedAddressesAsync());

    Assert.That(resolver.HasInvalidated, Is.False, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.That(scanner.FullScanRequested, Is.True, nameof(scanner.FullScanRequested));
    Assert.That(scanner.PartialScanRequested, Is.False, nameof(scanner.PartialScanRequested));
    Assert.That(scanner.ScanRequestedAddresses, Is.Empty, nameof(scanner.ScanRequestedAddresses));
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

    Assert.That(resolver.HasInvalidated, Is.True, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshInvalidatedAddressesAsync());

    Assert.That(resolver.HasInvalidated, Is.False, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.That(scanner.FullScanRequested, Is.False, nameof(scanner.FullScanRequested));
    Assert.That(scanner.PartialScanRequested, Is.True, nameof(scanner.PartialScanRequested));
    Assert.That(
      scanner.ScanRequestedAddresses, Is.EqualTo(new[] { TestIPAddress }).AsCollection,
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

    Assert.That(resolver.HasInvalidated, Is.False, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshInvalidatedAddressesAsync());

    Assert.That(resolver.HasInvalidated, Is.False, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.That(scanner.FullScanRequested, Is.False, nameof(scanner.FullScanRequested));
    Assert.That(scanner.PartialScanRequested, Is.False, nameof(scanner.PartialScanRequested));
    Assert.That(scanner.ScanRequestedAddresses, Is.Empty, nameof(scanner.ScanRequestedAddresses));
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

    Assert.That(scanner.NumberOfTasksPerformed, Is.GreaterThan(1), nameof(scanner.NumberOfTasksPerformed));
    Assert.That(
      scanner.MaxNumberOfTasksPerformedInParallel,
      Is.LessThanOrEqualTo(parallelCountForRefreshInvalidatedAddresses),
      nameof(scanner.MaxNumberOfTasksPerformedInParallel)
    );
  }
}
