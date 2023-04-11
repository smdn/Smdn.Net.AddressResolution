// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

[TestFixture]
public partial class MacAddressResolverTests {
  private sealed class PerformDelayNeighborDiscoverer : INeighborDiscoverer {
    private readonly TimeSpan delayOnNeighborDiscovery;
    private readonly object lockObject = new();
    private volatile int numberOfTasksPerformed;
    private volatile int numberOfTasksInProgress;
    private volatile int maxNumberOfTasksPerformedInParallel;

    public int NumberOfTasksPerformed => numberOfTasksPerformed;
    public int MaxNumberOfTasksPerformedInParallel => maxNumberOfTasksPerformedInParallel;

    public PerformDelayNeighborDiscoverer(TimeSpan delayOnNeighborDiscovery)
    {
      this.delayOnNeighborDiscovery = delayOnNeighborDiscovery;
    }

    public void Dispose()
    {
    }

    public async ValueTask DiscoverAsync(CancellationToken cancellationToken)
    {
      Interlocked.Increment(ref numberOfTasksPerformed);
      Interlocked.Increment(ref numberOfTasksInProgress);

      lock (lockObject) {
        if (maxNumberOfTasksPerformedInParallel < numberOfTasksInProgress)
          Interlocked.Exchange(ref maxNumberOfTasksPerformedInParallel, numberOfTasksInProgress);
      }
      await Task.Delay(delayOnNeighborDiscovery, cancellationToken);

      Interlocked.Decrement(ref numberOfTasksInProgress);
    }

    public async ValueTask DiscoverAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken)
    {
      Interlocked.Increment(ref numberOfTasksPerformed);
      Interlocked.Increment(ref numberOfTasksInProgress);

      lock (lockObject) {
        if (maxNumberOfTasksPerformedInParallel < numberOfTasksInProgress)
          Interlocked.Exchange(ref maxNumberOfTasksPerformedInParallel, numberOfTasksInProgress);
      }

      await Task.Delay(delayOnNeighborDiscovery, cancellationToken);

      Interlocked.Decrement(ref numberOfTasksInProgress);
    }
  }

  private static readonly IPAddress TestIPAddress = IPAddress.Parse("192.0.2.255");
  private static readonly PhysicalAddress TestMacAddress = PhysicalAddress.Parse("00:00:5E:00:53:00");

  [Test]
  public void Ctor_NeighborTable_ArgumentNull()
  {
    Assert.Throws<ArgumentNullException>(
      () => new MacAddressResolver(
        neighborTable: null,
        neighborDiscoverer: new PseudoNeighborDiscoverer(),
        serviceProvider: null
      ),
      "#1"
    );

    Assert.Throws<InvalidOperationException>(
      () => new MacAddressResolver(
        neighborTable: null,
        neighborDiscoverer: new PseudoNeighborDiscoverer(),
        serviceProvider: new ServiceCollection().BuildServiceProvider()
      ),
      "#2"
    );
  }

  [Test]
  public void Ctor_NeighborDiscoverer_ArgumentNull()
  {
    Assert.Throws<ArgumentNullException>(
      () => new MacAddressResolver(
        neighborTable: new PseudoNeighborTable(),
        neighborDiscoverer: null,
        serviceProvider: null
      ),
      "#1"
    );

    Assert.Throws<InvalidOperationException>(
      () => new MacAddressResolver(
        neighborTable: new PseudoNeighborTable(),
        neighborDiscoverer: null,
        serviceProvider: new ServiceCollection().BuildServiceProvider()
      ),
      "#2"
    );
  }

  [TestCase(0)]
  [TestCase(-1)]
  [TestCase(int.MinValue)]
  public void Ctor_MaxParallelCountForRefreshInvalidatedCache_ArgumentOutOfRange(int maxParallelCount)
    => Assert.Throws<ArgumentOutOfRangeException>(
      () => new MacAddressResolver(
        neighborTable: new PseudoNeighborTable(),
        neighborDiscoverer: new PseudoNeighborDiscoverer(),
        maxParallelCountForRefreshInvalidatedCache: maxParallelCount
      )
    );

  private static System.Collections.IEnumerable YieldTestCases_NeighborDiscoveryInterval()
  {
    yield return new object?[] { TimeSpan.Zero, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { TimeSpan.MinValue, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { TimeSpan.FromTicks(-1), typeof(ArgumentOutOfRangeException) };

    yield return new object?[] { TimeSpan.FromMilliseconds(1), null };
    yield return new object?[] { TimeSpan.FromTicks(1), null };
    yield return new object?[] { TimeSpan.MaxValue, null };
    yield return new object?[] { TimeSpan.FromMilliseconds(-1), null }; // == Timeout.InfiniteTimeSpan
    yield return new object?[] { Timeout.InfiniteTimeSpan, null };
  }

  [TestCaseSource(nameof(YieldTestCases_NeighborDiscoveryInterval))]
  public void NeighborDiscoveryInterval(
    TimeSpan neighborDiscoveryInterval,
    Type? typeOfExpectedException
  )
  {
    var resolver = new MacAddressResolver(
      neighborTable: new PseudoNeighborTable(),
      neighborDiscoverer: new PseudoNeighborDiscoverer()
    );

    if (typeOfExpectedException is null) {
      Assert.DoesNotThrow(() => resolver.NeighborDiscoveryInterval = neighborDiscoveryInterval);
      Assert.AreEqual(neighborDiscoveryInterval, resolver.NeighborDiscoveryInterval, nameof(resolver.NeighborDiscoveryInterval));
    }
    else {
      var initialNeighborDiscoveryInterval = resolver.NeighborDiscoveryInterval;

      Assert.Throws(typeOfExpectedException, () => resolver.NeighborDiscoveryInterval = neighborDiscoveryInterval);
      Assert.AreEqual(initialNeighborDiscoveryInterval, resolver.NeighborDiscoveryInterval, nameof(resolver.NeighborDiscoveryInterval));
    }
  }

  private static System.Collections.IEnumerable YieldTestCases_NeighborDiscoveryMinInterval()
  {
    yield return new object?[] { TimeSpan.MinValue, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { TimeSpan.FromTicks(-1), typeof(ArgumentOutOfRangeException) };

    yield return new object?[] { TimeSpan.Zero, null };
    yield return new object?[] { TimeSpan.FromMilliseconds(1), null };
    yield return new object?[] { TimeSpan.FromTicks(1), null };
    yield return new object?[] { TimeSpan.MaxValue, null };
    yield return new object?[] { TimeSpan.FromMilliseconds(-1), null }; // == Timeout.InfiniteTimeSpan
    yield return new object?[] { Timeout.InfiniteTimeSpan, null };
  }

  [TestCaseSource(nameof(YieldTestCases_NeighborDiscoveryMinInterval))]
  public void NeighborDiscoveryMinInterval(
    TimeSpan neighborDiscoveryMinInterval,
    Type? typeOfExpectedException
  )
  {
    var resolver = new MacAddressResolver(
      neighborTable: new PseudoNeighborTable(),
      neighborDiscoverer: new PseudoNeighborDiscoverer()
    );

    if (typeOfExpectedException is null) {
      Assert.DoesNotThrow(() => resolver.NeighborDiscoveryMinInterval = neighborDiscoveryMinInterval);
      Assert.AreEqual(neighborDiscoveryMinInterval, resolver.NeighborDiscoveryMinInterval, nameof(resolver.NeighborDiscoveryMinInterval));
    }
    else {
      var initialNeighborDiscoveryMinInterval = resolver.NeighborDiscoveryMinInterval;

      Assert.Throws(typeOfExpectedException, () => resolver.NeighborDiscoveryMinInterval = neighborDiscoveryMinInterval);
      Assert.AreEqual(initialNeighborDiscoveryMinInterval, resolver.NeighborDiscoveryMinInterval, nameof(resolver.NeighborDiscoveryMinInterval));
    }
  }

  [Test]
  public void HasInvalidated_IPAddressInvalidated()
  {
    var resolver = new MacAddressResolver(
      neighborTable: new PseudoNeighborTable(),
      neighborDiscoverer: new PseudoNeighborDiscoverer()
    );

    Assert.IsFalse(resolver.HasInvalidated, "initial state");

    resolver.Invalidate(TestIPAddress);

    Assert.IsTrue(resolver.HasInvalidated, "after invalidate IP address");

    resolver.Invalidate(TestMacAddress);

    Assert.IsTrue(resolver.HasInvalidated, "after invalidate MAC address");
  }

  [Test]
  public void HasInvalidated_MacAddressInvalidated()
  {
    var resolver = new MacAddressResolver(
      neighborTable: new PseudoNeighborTable(),
      neighborDiscoverer: new PseudoNeighborDiscoverer()
    );

    Assert.IsFalse(resolver.HasInvalidated, "initial state");

    resolver.Invalidate(TestMacAddress);

    Assert.IsTrue(resolver.HasInvalidated, "after invalidate MAC address");

    resolver.Invalidate(TestIPAddress);

    Assert.IsTrue(resolver.HasInvalidated, "after invalidate IP address");
  }

  [Test]
  public void Dispose_NeighborTableAndNeighborDiscovererAreSuppliedViaServiceProvider()
  {
    var neighborTable = new PseudoNeighborTable();
    var neighborDiscoverer = new PseudoNeighborDiscoverer();
    var services = new ServiceCollection();

    services.AddSingleton<INeighborTable>(neighborTable);
    services.AddSingleton<INeighborDiscoverer>(neighborDiscoverer);

    var resolver = new MacAddressResolver(
      networkProfile: null,
      serviceProvider: services.BuildServiceProvider()
    );

    resolver.Dispose();

    Assert.IsFalse(neighborTable.IsDisposed, $"{nameof(INeighborTable)} should not be disposed by default.");
    Assert.IsFalse(neighborDiscoverer.IsDisposed, $"{nameof(INeighborDiscoverer)} should not be disposed by default.");
  }

  [Test]
  public void Dispose_NeighborTableAndNeighborDiscovererAreSuppliedAsCtorParameter()
  {
    var neighborTable = new PseudoNeighborTable();
    var neighborDiscoverer = new PseudoNeighborDiscoverer();
    var resolver = new MacAddressResolver(
      neighborTable: neighborTable,
      neighborDiscoverer: neighborDiscoverer
    );

    resolver.Dispose();

    Assert.IsFalse(neighborTable.IsDisposed, $"{nameof(INeighborTable)} should not be disposed by default.");
    Assert.IsFalse(neighborDiscoverer.IsDisposed, $"{nameof(INeighborDiscoverer)} should not be disposed by default.");
  }

  [TestCase(true, true)]
  [TestCase(false, true)]
  [TestCase(true, false)]
  [TestCase(false, false)]
  public void Dispose_ShouldDisposeNeighborTableAndNeighborDiscoverer(
    bool shouldDisposeNeighborTable,
    bool shouldDisposeNeighborDiscoverer
  )
  {
    var neighborTable = new PseudoNeighborTable();
    var neighborDiscoverer = new PseudoNeighborDiscoverer();
    var resolver = new MacAddressResolver(
      neighborTable: neighborTable,
      shouldDisposeNeighborTable: shouldDisposeNeighborTable,
      neighborDiscoverer: neighborDiscoverer,
      shouldDisposeNeighborDiscoverer: shouldDisposeNeighborDiscoverer
    );

    resolver.Dispose();

    Assert.AreEqual(neighborTable.IsDisposed, shouldDisposeNeighborTable);
    Assert.AreEqual(neighborDiscoverer.IsDisposed, shouldDisposeNeighborDiscoverer);

    Assert.DoesNotThrow(() => resolver.Dispose(), "dispose again");

    Assert.AreEqual(neighborTable.IsDisposed, shouldDisposeNeighborTable);
    Assert.AreEqual(neighborDiscoverer.IsDisposed, shouldDisposeNeighborDiscoverer);
  }
}
