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
  private sealed class InterceptingNeighborDiscoverer : INeighborDiscoverer {
    private readonly Action actionBeforePerformDiscovery;

    public InterceptingNeighborDiscoverer(Action actionBeforePerformDiscovery)
    {
      this.actionBeforePerformDiscovery = actionBeforePerformDiscovery;
    }

    public void Dispose()
    {
    }

    public ValueTask DiscoverAsync(CancellationToken cancellationToken)
    {
      actionBeforePerformDiscovery();

      return default;
    }

    public ValueTask DiscoverAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken)
    {
      actionBeforePerformDiscovery();

      return default;
    }
  }

  [Test]
  public async Task ResolveIPAddressToMacAddressAsync_PerformCacheRefresh()
  {
    var performedNeighborDiscovery = false;

    using var resolver = new MacAddressResolver(
      neighborTable: new NullNeighborTable(),
      neighborDiscoverer: new InterceptingNeighborDiscoverer(() => performedNeighborDiscovery = true)
    ) {
      NeighborDiscoveryMinInterval = TimeSpan.Zero,
      NeighborDiscoveryInterval = TimeSpan.FromMilliseconds(500),
    };

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.IsTrue(performedNeighborDiscovery, "attempt initial resolution");

    // NeighborDiscoveryInterval not elapsed
    performedNeighborDiscovery = false;

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.IsFalse(performedNeighborDiscovery, "attempt resolution at the time the interval has not elapsed yet");

    // NeighborDiscoveryInterval elapsed
    await Task.Delay(resolver.NeighborDiscoveryInterval + TimeSpan.FromMilliseconds(100));

    performedNeighborDiscovery = false;

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.IsTrue(performedNeighborDiscovery, "attempt resolution at the time the interval has elapsed");
  }

  [Test]
  public async Task ResolveMacAddressToIPAddressAsync_PerformCacheRefresh()
  {
    var performedNeighborDiscovery = false;

    using var resolver = new MacAddressResolver(
      neighborTable: new NullNeighborTable(),
      neighborDiscoverer: new InterceptingNeighborDiscoverer(() => performedNeighborDiscovery = true)
    ) {
      NeighborDiscoveryMinInterval = TimeSpan.Zero,
      NeighborDiscoveryInterval = TimeSpan.FromMilliseconds(500),
    };

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.IsTrue(performedNeighborDiscovery, "attempt initial resolution");

    // NeighborDiscoveryInterval not elapsed
    performedNeighborDiscovery = false;

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.IsFalse(performedNeighborDiscovery, "attempt resolution at the time the interval has not elapsed yet");

    // NeighborDiscoveryInterval elapsed
    await Task.Delay(resolver.NeighborDiscoveryInterval + TimeSpan.FromMilliseconds(100));

    performedNeighborDiscovery = false;

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.IsTrue(performedNeighborDiscovery, "attempt resolution at the time the interval has elapsed");
  }

  [Test]
  public async Task ResolveIPAddressToMacAddressAsync_PerformCacheRefresh_Never()
  {
    var performedNeighborDiscovery = false;

    using var resolver = new MacAddressResolver(
      neighborTable: new NullNeighborTable(),
      neighborDiscoverer: new InterceptingNeighborDiscoverer(() => performedNeighborDiscovery = true)
    ) {
      NeighborDiscoveryMinInterval = TimeSpan.Zero,
      NeighborDiscoveryInterval = Timeout.InfiniteTimeSpan,
    };

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.IsFalse(performedNeighborDiscovery, "attempt initial resolution");

    await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress);

    Assert.IsFalse(performedNeighborDiscovery, "attempt second resolution");
  }

  [Test]
  public async Task ResolveMacAddressToIPAddressAsync_PerformCacheRefresh_Never()
  {
    var performedNeighborDiscovery = false;

    using var resolver = new MacAddressResolver(
      neighborTable: new NullNeighborTable(),
      neighborDiscoverer: new InterceptingNeighborDiscoverer(() => performedNeighborDiscovery = true)
    ) {
      NeighborDiscoveryMinInterval = TimeSpan.Zero,
      NeighborDiscoveryInterval = Timeout.InfiniteTimeSpan,
    };

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.IsFalse(performedNeighborDiscovery, "attempt initial resolution");

    await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress);

    Assert.IsFalse(performedNeighborDiscovery, "attempt second resolution");
  }
}
