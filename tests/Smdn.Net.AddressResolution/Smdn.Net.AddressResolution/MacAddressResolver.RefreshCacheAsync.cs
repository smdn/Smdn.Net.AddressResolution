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
  public void RefreshCacheAsync_AddressInvalidated()
  {
    var discoverer = new PseudoNeighborDiscoverer();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      neighborDiscoverer: discoverer
    );

    resolver.Invalidate(TestIPAddress);
    resolver.Invalidate(TestMacAddress);

    Assert.IsTrue(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshCacheAsync());

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.IsTrue(discoverer.FullDiscoveryRequested, nameof(discoverer.FullDiscoveryRequested));
    Assert.IsFalse(discoverer.PartialDiscoveryRequested, nameof(discoverer.PartialDiscoveryRequested));
    CollectionAssert.IsEmpty(
      discoverer.DiscoveryRequestedAddresses,
      nameof(discoverer.DiscoveryRequestedAddresses)
    );
  }

  [Test]
  public void RefreshCacheAsync_NothingInvalidated()
  {
    var discoverer = new PseudoNeighborDiscoverer();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      neighborDiscoverer: discoverer
    );

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} brefore refresh");

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshCacheAsync());

    Assert.IsFalse(resolver.HasInvalidated, $"{nameof(resolver.HasInvalidated)} after refresh");

    Assert.IsTrue(discoverer.FullDiscoveryRequested, nameof(discoverer.FullDiscoveryRequested));
    Assert.IsFalse(discoverer.PartialDiscoveryRequested, nameof(discoverer.PartialDiscoveryRequested));
    CollectionAssert.IsEmpty(
      discoverer.DiscoveryRequestedAddresses,
      nameof(discoverer.DiscoveryRequestedAddresses)
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_RefreshCacheAsync_NeighborDiscoveryIntervalMustNotAffect()
  {
    yield return new object[] { TimeSpan.FromTicks(1) };
    yield return new object[] { TimeSpan.MaxValue };
    yield return new object[] { Timeout.InfiniteTimeSpan };
  }

  [TestCaseSource(nameof(YieldTestCases_RefreshCacheAsync_NeighborDiscoveryIntervalMustNotAffect))]
  public void RefreshCacheAsync_NeighborDiscoveryIntervalMustNotAffect(TimeSpan neighborDiscoveryInterval)
  {
    var discoverer = new PseudoNeighborDiscoverer();

    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      neighborDiscoverer: discoverer
    ) {
      NeighborDiscoveryInterval = neighborDiscoveryInterval,
      NeighborDiscoveryMinInterval = TimeSpan.Zero
    };

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshCacheAsync(), $"{nameof(resolver.RefreshCacheAsync)} #1");

    Assert.IsTrue(discoverer.FullDiscoveryRequested, $"{nameof(discoverer.FullDiscoveryRequested)} #1");

    discoverer.Reset();

    Assert.DoesNotThrowAsync(async () => await resolver.RefreshCacheAsync(), $"{nameof(resolver.RefreshCacheAsync)} #2");

    Assert.IsTrue(discoverer.FullDiscoveryRequested, $"{nameof(discoverer.FullDiscoveryRequested)} #2");
  }

  [Test]
  public async Task RefreshCacheAsync_MustPerformInsideOfMutexCriticalSection()
  {
    const int numberOfParallelism = 20;

    var discoverer = new PerformDelayNeighborDiscoverer(delayOnNeighborDiscovery: TimeSpan.FromMilliseconds(100));
    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      neighborDiscoverer: discoverer
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
        await resolver.RefreshCacheAsync(cancellationToken);
      }
    );

    Assert.AreEqual(1, discoverer.MaxNumberOfTasksPerformedInParallel, nameof(discoverer.MaxNumberOfTasksPerformedInParallel));
    Assert.Greater(discoverer.NumberOfTasksPerformed, 1, nameof(discoverer.NumberOfTasksPerformed));
  }
}
