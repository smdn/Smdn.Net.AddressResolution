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

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

partial class MacAddressResolverTests {
  [Test]
  public void RefreshInvalidatedCacheAsync_MacAddressInvalidated()
  {
    var discoverer = new PseudoNeighborDiscoverer();

    var resolver = new MacAddressResolver(
      neighborTable: new PseudoNeighborTable(),
      neighborDiscoverer: discoverer
    );

    resolver.Invalidate(TestMacAddress);

    Assert.IsTrue(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshInvalidatedCacheAsync());

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.IsTrue(discoverer.FullDiscoveryRequested, nameof(discoverer.FullDiscoveryRequested));
    Assert.IsFalse(discoverer.PartialDiscoveryRequested, nameof(discoverer.PartialDiscoveryRequested));
    CollectionAssert.IsEmpty(
      discoverer.DiscoveryRequestedAddresses,
      nameof(discoverer.DiscoveryRequestedAddresses)
    );
  }

  [Test]
  public void RefreshInvalidatedCacheAsync_IPAddressInvalidated()
  {
    var discoverer = new PseudoNeighborDiscoverer();

    var resolver = new MacAddressResolver(
      neighborTable: new PseudoNeighborTable(),
      neighborDiscoverer: discoverer
    );

    resolver.Invalidate(TestIPAddress);

    Assert.IsTrue(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshInvalidatedCacheAsync());

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.IsFalse(discoverer.FullDiscoveryRequested, nameof(discoverer.FullDiscoveryRequested));
    Assert.IsTrue(discoverer.PartialDiscoveryRequested, nameof(discoverer.PartialDiscoveryRequested));
    CollectionAssert.AreEqual(
      new[] { TestIPAddress },
      discoverer.DiscoveryRequestedAddresses,
      nameof(discoverer.DiscoveryRequestedAddresses)
    );
  }

  [Test]
  public void RefreshInvalidatedCacheAsync_NothingInvalidated()
  {
    var discoverer = new PseudoNeighborDiscoverer();

    var resolver = new MacAddressResolver(
      neighborTable: new PseudoNeighborTable(),
      neighborDiscoverer: discoverer
    );

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshInvalidatedCacheAsync());

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.IsFalse(discoverer.FullDiscoveryRequested, nameof(discoverer.FullDiscoveryRequested));
    Assert.IsFalse(discoverer.PartialDiscoveryRequested, nameof(discoverer.PartialDiscoveryRequested));
    CollectionAssert.IsEmpty(discoverer.DiscoveryRequestedAddresses, nameof(discoverer.DiscoveryRequestedAddresses));
  }

  [TestCase(1)]
  [TestCase(2)]
  [TestCase(5)]
  [TestCase(10)]
  public async Task RefreshInvalidatedCacheAsync_MustPerformInsideOfSemaphoreCriticalSection(
    int parallelCountForRefreshInvalidatedCache
  )
  {
    const int numberOfParallelism = 20;

    var discoverer = new PerformDelayNeighborDiscoverer(delayOnNeighborDiscovery: TimeSpan.FromMilliseconds(100));
    var resolver = new MacAddressResolver(
      neighborTable: new PseudoNeighborTable(),
      neighborDiscoverer: discoverer,
      maxParallelCountForRefreshInvalidatedCache: parallelCountForRefreshInvalidatedCache
    ) {
      NeighborDiscoveryInterval = Timeout.InfiniteTimeSpan,
      NeighborDiscoveryMinInterval = TimeSpan.Zero
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

        await resolver.RefreshInvalidatedCacheAsync(cancellationToken);
      }
    );

    Assert.Greater(discoverer.NumberOfTasksPerformed, 1, nameof(discoverer.NumberOfTasksPerformed));
    Assert.LessOrEqual(
      discoverer.MaxNumberOfTasksPerformedInParallel,
      parallelCountForRefreshInvalidatedCache,
      nameof(discoverer.MaxNumberOfTasksPerformedInParallel)
    );
  }
}
