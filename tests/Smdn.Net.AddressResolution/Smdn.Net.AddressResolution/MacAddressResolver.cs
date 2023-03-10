// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Smdn.Net.AddressResolution;

[TestFixture]
public partial class MacAddressResolverTests {
  private class ConcreteMacAddressResolver : MacAddressResolver {
    protected override ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(
      PhysicalAddress macAddress,
      CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    protected override ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(
      IPAddress ipAddress,
      CancellationToken cancellationToken
    ) => throw new NotImplementedException();
  }

  private static readonly IPAddress TestIPAddress = IPAddress.Parse("192.0.2.255");
  private static readonly PhysicalAddress TestMacAddress = PhysicalAddress.Parse("00:00:5E:00:53:00");
  private static readonly PhysicalAddress AllZeroMacAddress = PhysicalAddress.Parse("00:00:00:00:00:00");

  [Test]
  public void Dispose()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.DoesNotThrow(() => resolver.Dispose(), "Dispose #1");
    Assert.DoesNotThrow(() => resolver.Dispose(), "Dispose #2");

#pragma warning disable CA2012
    Assert.Throws<ObjectDisposedException>(
      () => resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress),
      nameof(resolver.ResolveIPAddressToMacAddressAsync)
    );
    Assert.ThrowsAsync<ObjectDisposedException>(
      async () => await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress),
      nameof(resolver.ResolveIPAddressToMacAddressAsync)
    );

    Assert.Throws<ObjectDisposedException>(
      () => resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress),
      nameof(resolver.ResolveMacAddressToIPAddressAsync)
    );
    Assert.ThrowsAsync<ObjectDisposedException>(
      async () => await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress),
      nameof(resolver.ResolveMacAddressToIPAddressAsync)
    );

    Assert.Throws<ObjectDisposedException>(
      () => resolver.RefreshCacheAsync(),
      nameof(resolver.RefreshCacheAsync)
    );
    Assert.ThrowsAsync<ObjectDisposedException>(
      async () => await resolver.RefreshCacheAsync(),
      nameof(resolver.RefreshCacheAsync)
    );
#pragma warning restore CA2012
  }

  [Test]
  public void ResolveIPAddressToMacAddressAsync()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.ThrowsAsync<NotImplementedException>(
      async () => await resolver.ResolveIPAddressToMacAddressAsync(ipAddress: TestIPAddress)
    );
  }

  [Test]
  public void ResolveIPAddressToMacAddressAsync_ArgumentNull()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.ThrowsAsync<ArgumentNullException>(
      async () => await resolver.ResolveIPAddressToMacAddressAsync(ipAddress: null!)
    );
  }

  [Test]
  public void ResolveIPAddressToMacAddressAsync_CancellationRequested()
  {
    using var resolver = new ConcreteMacAddressResolver();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    Assert.ThrowsAsync<TaskCanceledException>(
      async () => await resolver.ResolveIPAddressToMacAddressAsync(ipAddress: TestIPAddress, cancellationToken: cts.Token)
    );
  }

  [Test]
  public void IAddressResolver_Of_IPAddress_PhysicallAddress_ResolveAsync()
  {
    using var resolver = new ConcreteMacAddressResolver();
    var res = (IAddressResolver<IPAddress, PhysicalAddress>)resolver;

    Assert.ThrowsAsync<NotImplementedException>(
      async () => await res.ResolveAsync(address: TestIPAddress, cancellationToken: default)
    );
  }

  [Test]
  public void ResolveMacAddressToIPAddressAsync()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.ThrowsAsync<NotImplementedException>(
      async () => await resolver.ResolveMacAddressToIPAddressAsync(macAddress: TestMacAddress)
    );
  }

  [Test]
  public void ResolveMacAddressToIPAddressAsync_ArgumentNull()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.ThrowsAsync<ArgumentNullException>(
      async () => await resolver.ResolveMacAddressToIPAddressAsync(macAddress: null!)
    );
  }

  [Test]
  public async Task ResolveMacAddressToIPAddressAsync_AllZeroMacAddress()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.IsNull(await resolver.ResolveMacAddressToIPAddressAsync(macAddress: AllZeroMacAddress));
  }

  [Test]
  public void ResolveMacAddressToIPAddressAsync_CancellationRequested()
  {
    using var resolver = new ConcreteMacAddressResolver();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    Assert.ThrowsAsync<TaskCanceledException>(
      async () => await resolver.ResolveMacAddressToIPAddressAsync(macAddress: TestMacAddress, cancellationToken: cts.Token)
    );
  }

  [Test]
  public void IAddressResolver_Of_PhysicallAddress_IPAddress_ResolveAsync()
  {
    using var resolver = new ConcreteMacAddressResolver();
    var res = (IAddressResolver<PhysicalAddress, IPAddress>)resolver;

    Assert.ThrowsAsync<NotImplementedException>(
      async () => await res.ResolveAsync(address: TestMacAddress, cancellationToken: default)
    );
  }

  [Test]
  public void RefreshCacheAsync()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.DoesNotThrowAsync(
      async () => await resolver.RefreshCacheAsync()
    );
  }

  [Test]
  public void RefreshCacheAsync_CancellationRequested()
  {
    using var resolver = new ConcreteMacAddressResolver();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    Assert.ThrowsAsync<TaskCanceledException>(
      async () => await resolver.RefreshCacheAsync(cancellationToken: cts.Token)
    );
  }
}
