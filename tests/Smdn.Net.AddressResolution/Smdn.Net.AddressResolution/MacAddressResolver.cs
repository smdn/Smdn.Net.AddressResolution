// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net.AddressResolution;

[TestFixture]
public partial class MacAddressResolverTests {
  private sealed class PseudoNeighborTable : INeighborTable {
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;

    public IAsyncEnumerable<NeighborTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken)
      => throw new NotImplementedException();
  }

  private sealed class PseudoNeighborDiscoverer : INeighborDiscoverer {
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;

    public ValueTask DiscoverAsync(CancellationToken cancellationToken)
      => default;

    public ValueTask DiscoverAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken)
      => default;
  }

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
