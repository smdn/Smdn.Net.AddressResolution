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
public class MacAddressResolverBaseTests {
  private class ConcreteMacAddressResolver : MacAddressResolverBase {
    public override bool HasInvalidated => throw new NotImplementedException();

    public ConcreteMacAddressResolver()
      : base()
    {
    }

    protected override ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(
      PhysicalAddress macAddress,
      CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    protected override ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(
      IPAddress ipAddress,
      CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    protected override void InvalidateCore(IPAddress resolvedIPAddress) => throw new NotImplementedException();
    protected override void InvalidateCore(PhysicalAddress resolvedMacAddress) => throw new NotImplementedException();
  }

  private static readonly IPAddress TestIPAddress = IPAddress.Parse("192.0.2.255");
  private static readonly PhysicalAddress TestMacAddress = PhysicalAddress.Parse("00:00:5E:00:53:00");
  private static readonly PhysicalAddress AllZeroMacAddress = PhysicalAddress.Parse("00:00:00:00:00:00");

  [Test]
  public void Dispose()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.That(resolver.Dispose, Throws.Nothing, "Dispose #1");
    Assert.That(resolver.Dispose, Throws.Nothing, "Dispose #2");

    Assert.That(
      () => resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress),
      Throws.TypeOf<ObjectDisposedException>(),
      nameof(resolver.ResolveIPAddressToMacAddressAsync)
    );
    Assert.That(
      async () => await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress),
      Throws.TypeOf<ObjectDisposedException>(),
      nameof(resolver.ResolveIPAddressToMacAddressAsync)
    );

    Assert.That(
      () => resolver.Invalidate(TestIPAddress),
      Throws.TypeOf<ObjectDisposedException>(),
      nameof(resolver.Invalidate)
    );

    Assert.That(
      () => resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress),
      Throws.TypeOf<ObjectDisposedException>(),
      nameof(resolver.ResolveMacAddressToIPAddressAsync)
    );
    Assert.That(
      async () => await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress),
      Throws.TypeOf<ObjectDisposedException>(),
      nameof(resolver.ResolveMacAddressToIPAddressAsync)
    );

    Assert.That(
      () => resolver.Invalidate(TestMacAddress),
      Throws.TypeOf<ObjectDisposedException>(),
      nameof(resolver.Invalidate)
    );

    Assert.That(
      () => resolver.RefreshAddressTableAsync(),
      Throws.TypeOf<ObjectDisposedException>(),
      nameof(resolver.RefreshAddressTableAsync)
    );
    Assert.That(
      async () => await resolver.RefreshAddressTableAsync(),
      Throws.TypeOf<ObjectDisposedException>(),
      nameof(resolver.RefreshAddressTableAsync)
    );

    Assert.That(
      () => resolver.RefreshInvalidatedAddressesAsync(),
      Throws.TypeOf<ObjectDisposedException>(),
      nameof(resolver.RefreshInvalidatedAddressesAsync)
    );
    Assert.That(
      async () => await resolver.RefreshInvalidatedAddressesAsync(),
      Throws.TypeOf<ObjectDisposedException>(),
      nameof(resolver.RefreshInvalidatedAddressesAsync)
    );
  }

  [Test]
  public void ResolveIPAddressToMacAddressAsync()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.That(
      async () => await resolver.ResolveIPAddressToMacAddressAsync(ipAddress: TestIPAddress),
      Throws.TypeOf<NotImplementedException>()
    );
  }

  [Test]
  public void ResolveIPAddressToMacAddressAsync_ArgumentNull()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.That(
      async () => await resolver.ResolveIPAddressToMacAddressAsync(ipAddress: null!),
      Throws.ArgumentNullException
    );
  }

  [Test]
  public void ResolveIPAddressToMacAddressAsync_CancellationRequested()
  {
    using var resolver = new ConcreteMacAddressResolver();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    Assert.That(
      async () => await resolver.ResolveIPAddressToMacAddressAsync(ipAddress: TestIPAddress, cancellationToken: cts.Token),
      Throws.InstanceOf<OperationCanceledException>()
    );
  }

  [Test]
  public void IAddressResolver_Of_IPAddress_PhysicalAddress_ResolveAsync()
  {
    using var resolver = new ConcreteMacAddressResolver();
    var res = (IAddressResolver<IPAddress, PhysicalAddress>)resolver;

    Assert.That(
      async () => await res.ResolveAsync(address: TestIPAddress, cancellationToken: default),
      Throws.TypeOf<NotImplementedException>()
    );
  }

  [Test]
  public void Invalidate_IPAddress_ArgumentNull()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.That(() => resolver.Invalidate(ipAddress: null!), Throws.ArgumentNullException);
  }

  [Test]
  public void IAddressResolver_Of_IPAddress_PhysicalAddress_Invalidate()
  {
    using var resolver = new ConcreteMacAddressResolver();
    var res = (IAddressResolver<IPAddress, PhysicalAddress>)resolver;

    Assert.That(() => res.Invalidate(address: TestIPAddress), Throws.TypeOf<NotImplementedException>());
  }

  [Test]
  public void ResolveMacAddressToIPAddressAsync()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.That(
      async () => await resolver.ResolveMacAddressToIPAddressAsync(macAddress: TestMacAddress),
      Throws.TypeOf<NotImplementedException>()
    );
  }

  [Test]
  public void ResolveMacAddressToIPAddressAsync_ArgumentNull()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.That(
      async () => await resolver.ResolveMacAddressToIPAddressAsync(macAddress: null!),
      Throws.ArgumentNullException
    );
  }

  [Test]
  public async Task ResolveMacAddressToIPAddressAsync_AllZeroMacAddress()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.That(await resolver.ResolveMacAddressToIPAddressAsync(macAddress: AllZeroMacAddress), Is.Null);
  }

  [Test]
  public void ResolveMacAddressToIPAddressAsync_CancellationRequested()
  {
    using var resolver = new ConcreteMacAddressResolver();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    Assert.That(
      async () => await resolver.ResolveMacAddressToIPAddressAsync(macAddress: TestMacAddress, cancellationToken: cts.Token),
      Throws.InstanceOf<OperationCanceledException>()
    );
  }

  [Test]
  public void IAddressResolver_Of_PhysicalAddress_IPAddress_ResolveAsync()
  {
    using var resolver = new ConcreteMacAddressResolver();
    var res = (IAddressResolver<PhysicalAddress, IPAddress>)resolver;

    Assert.That(
      async () => await res.ResolveAsync(address: TestMacAddress, cancellationToken: default),
      Throws.TypeOf<NotImplementedException>()
    );
  }

  [Test]
  public void Invalidate_MacAddress_ArgumentNull()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.That(() => resolver.Invalidate(macAddress: null!), Throws.ArgumentNullException);
  }

  [Test]
  public void IAddressResolver_Of_PhysicalAddress_IPAddress_Invalidate()
  {
    using var resolver = new ConcreteMacAddressResolver();
    var res = (IAddressResolver<PhysicalAddress, IPAddress>)resolver;

    Assert.That(() => res.Invalidate(address: TestMacAddress), Throws.TypeOf<NotImplementedException>());
  }

  [Test]
  public void RefreshAddressTableAsync()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.That(
      async () => await resolver.RefreshAddressTableAsync(),
      Throws.Nothing
    );
  }

  [Test]
  public void RefreshAddressTableAsync_CancellationRequested()
  {
    using var resolver = new ConcreteMacAddressResolver();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    Assert.That(
      async () => await resolver.RefreshAddressTableAsync(cancellationToken: cts.Token),
      Throws.InstanceOf<OperationCanceledException>()
    );
  }

  [Test]
  public void RefreshInvalidatedAddressesAsync()
  {
    using var resolver = new ConcreteMacAddressResolver();

    Assert.That(
      async () => await resolver.RefreshInvalidatedAddressesAsync(),
      Throws.Nothing
    );
  }

  [Test]
  public void RefreshInvalidatedAddressesAsync_CancellationRequested()
  {
    using var resolver = new ConcreteMacAddressResolver();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    Assert.That(
      async () => await resolver.RefreshInvalidatedAddressesAsync(cancellationToken: cts.Token),
      Throws.InstanceOf<OperationCanceledException>()
    );
  }
}
