// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.AddressResolution;

[TestFixture]
public partial class NullMacAddressResolverTests {
  private static readonly IPAddress TestIPAddress = IPAddress.Parse("192.0.2.255");
  private static readonly PhysicalAddress TestMacAddress = PhysicalAddress.Parse("00:00:5E:00:53:00");

  [Test]
  public void Dispose()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.That(resolver.Dispose, Throws.Nothing, "Dispose #1");
    Assert.That(resolver.Dispose, Throws.Nothing, "Dispose #2");

    // object must not be disposed

    Assert.That(
      () => _ = resolver.HasInvalidated,
      Throws.Nothing,
      nameof(resolver.HasInvalidated)
    );

    Assert.That(
      async () => await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress),
      Throws.Nothing,
      nameof(resolver.ResolveIPAddressToMacAddressAsync)
    );
    Assert.That(
      () => resolver.Invalidate(TestIPAddress),
      Throws.Nothing,
      nameof(resolver.Invalidate)
    );

    Assert.That(
      async () => await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress),
      Throws.Nothing,
      nameof(resolver.ResolveMacAddressToIPAddressAsync)
    );
    Assert.That(
      () => resolver.Invalidate(TestMacAddress),
      Throws.Nothing,
      nameof(resolver.Invalidate)
    );

    Assert.That(
      async () => await resolver.RefreshAddressTableAsync(),
      Throws.Nothing,
      nameof(resolver.RefreshAddressTableAsync)
    );
    Assert.That(
      async () => await resolver.RefreshInvalidatedAddressesAsync(),
      Throws.Nothing,
      nameof(resolver.RefreshInvalidatedAddressesAsync)
    );
  }

  [Test]
  public void HasInvalidated()
  {
    Assert.That(MacAddressResolver.Null.HasInvalidated, Is.False, nameof(MacAddressResolver.Null.HasInvalidated));
    Assert.That(MacAddressResolver.Null.HasInvalidated, Is.False, nameof(MacAddressResolver.Null.HasInvalidated) + " must always be false");
  }

  [Test]
  public async Task ResolveIPAddressToMacAddressAsync()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.That(await resolver.ResolveIPAddressToMacAddressAsync(ipAddress: TestIPAddress), Is.Null);
  }

  [Test]
  public async Task ResolveMacAddressToIPAddressAsync()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.That(await resolver.ResolveMacAddressToIPAddressAsync(macAddress: TestMacAddress), Is.Null);
  }

  [Test]
  public void Invalidate_IPAddress()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.That(() => resolver.Invalidate(TestIPAddress), Throws.Nothing);
  }

  [Test]
  public void Invalidate_MacAddress()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.That(() => resolver.Invalidate(TestMacAddress), Throws.Nothing);
  }

  [Test]
  public void RefreshAddressTableAsync()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.That(
      async () => await resolver.RefreshAddressTableAsync(),
      Throws.Nothing
    );
  }

  [Test]
  public void RefreshInvalidatedAddressesAsync()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.That(
      async () => await resolver.RefreshInvalidatedAddressesAsync(),
      Throws.Nothing
    );
  }
}
