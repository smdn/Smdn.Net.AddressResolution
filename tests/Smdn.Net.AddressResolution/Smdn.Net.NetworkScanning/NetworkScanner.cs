// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class NetworkScannerTests : NetworkScannerTestsBase {
  protected override INetworkScanner CreateNetworkScanner()
    => new ConcreteNetworkScanner(CreatePseudoNetworkProfile());

  private class ConcreteNetworkScanner : NetworkScanner {
    public ConcreteNetworkScanner(IPNetworkProfile networkProfile)
      : base(networkProfile: networkProfile)
    {
    }

    public IPNetworkProfile GetNetworkProfile()
      => base.NetworkProfile;

    protected override ValueTask ScanAsyncCore(
      IPAddress address,
      CancellationToken cancellationToken
    ) => default;
  }

  private static IPNetworkProfile CreatePseudoNetworkProfile()
    => IPNetworkProfile.Create(
      baseAddress: IPAddress.Parse("192.0.2.0"),
      subnetMask: IPAddress.Parse("255.255.255.0"),
      networkInterface: new PseudoNetworkInterface("wlan0")
    );

  [Test]
  public void Create()
  {
    try {
      Assert.That(NetworkScanner.Create(networkProfile: null), Is.Not.Null);
    }
    catch (PlatformNotSupportedException) {
      // possible and expected exception
    }

    try {
      Assert.That(NetworkScanner.Create(networkProfile: CreatePseudoNetworkProfile()), Is.Not.Null);
    }
    catch (PlatformNotSupportedException) {
      // possible and expected exception
    }
  }

  [Test]
  public void Ctor_IPNetworkProfileNull()
    => Assert.That(() => new ConcreteNetworkScanner(networkProfile: null!), Throws.ArgumentNullException);

  [Test]
  public override void Dispose()
  {
    base.Dispose();

    using var scanner = new ConcreteNetworkScanner(CreatePseudoNetworkProfile());

    Assert.That(scanner.Dispose, Throws.Nothing);

    Assert.That(scanner.GetNetworkProfile, Throws.TypeOf<ObjectDisposedException>());
  }

  [Test]
  public void ScanAsync_CanNotGetAddressRangeFromNetworkProfile()
  {
    using var scanner = new ConcreteNetworkScanner(
      IPNetworkProfile.Create(
        addressRangeGenerator: () => null,
        networkInterface: null
      )
    );

    Assert.That(
      async () => await scanner.ScanAsync(),
      Throws.InvalidOperationException
    );
  }
}
