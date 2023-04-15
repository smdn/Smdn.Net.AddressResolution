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

    Assert.DoesNotThrow(resolver.Dispose, "Dispose #1");
    Assert.DoesNotThrow(resolver.Dispose, "Dispose #2");

    // object must not be disposed

    Assert.DoesNotThrow(
      () => Assert.IsFalse(resolver.HasInvalidated),
      nameof(resolver.HasInvalidated)
    );

    Assert.DoesNotThrowAsync(
      async () => await resolver.ResolveIPAddressToMacAddressAsync(TestIPAddress),
      nameof(resolver.ResolveIPAddressToMacAddressAsync)
    );
    Assert.DoesNotThrow(
      () => resolver.Invalidate(TestIPAddress),
      nameof(resolver.Invalidate)
    );

    Assert.DoesNotThrowAsync(
      async () => await resolver.ResolveMacAddressToIPAddressAsync(TestMacAddress),
      nameof(resolver.ResolveMacAddressToIPAddressAsync)
    );
    Assert.DoesNotThrow(
      () => resolver.Invalidate(TestMacAddress),
      nameof(resolver.Invalidate)
    );

    Assert.DoesNotThrowAsync(
      async () => await resolver.RefreshAddressTableAsync(),
      nameof(resolver.RefreshAddressTableAsync)
    );
    Assert.DoesNotThrowAsync(
      async () => await resolver.RefreshInvalidatedAddressesAsync(),
      nameof(resolver.RefreshInvalidatedAddressesAsync)
    );
  }

  [Test]
  public void HasInvalidated()
  {
    Assert.IsFalse(MacAddressResolver.Null.HasInvalidated, nameof(MacAddressResolver.Null.HasInvalidated));
    Assert.IsFalse(MacAddressResolver.Null.HasInvalidated, nameof(MacAddressResolver.Null.HasInvalidated) + " must always be false");
  }

  [Test]
  public async Task ResolveIPAddressToMacAddressAsync()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.IsNull(await resolver.ResolveIPAddressToMacAddressAsync(ipAddress: TestIPAddress));
  }

  [Test]
  public async Task ResolveMacAddressToIPAddressAsync()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.IsNull(await resolver.ResolveMacAddressToIPAddressAsync(macAddress: TestMacAddress));
  }

  [Test]
  public void Invalidate_IPAddress()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.DoesNotThrow(() => resolver.Invalidate(TestIPAddress));
  }

  [Test]
  public void Invalidate_MacAddress()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.DoesNotThrow(() => resolver.Invalidate(TestMacAddress));
  }

  [Test]
  public void RefreshAddressTableAsync()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.DoesNotThrowAsync(
      async () => await resolver.RefreshAddressTableAsync()
    );
  }

  [Test]
  public void RefreshInvalidatedAddressesAsync()
  {
    using var resolver = MacAddressResolver.Null;

    Assert.DoesNotThrowAsync(
      async () => await resolver.RefreshInvalidatedAddressesAsync()
    );
  }
}
