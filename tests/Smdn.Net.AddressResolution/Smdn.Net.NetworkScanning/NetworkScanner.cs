// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class NetworkScannerTests {
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
      baseAddress: IPAddress.Parse("192.168.2.0"),
      subnetMask: IPAddress.Parse("255.255.255.0"),
      networkInterface: new PseudoNetworkInterface("wlan0")
    );

  [Test]
  public void Create()
  {
    try {
      Assert.IsNotNull(NetworkScanner.Create(networkProfile: null));
    }
    catch (PlatformNotSupportedException) {
      // possible and expected exception
    }

    try {
      Assert.IsNotNull(NetworkScanner.Create(networkProfile: CreatePseudoNetworkProfile()));
    }
    catch (PlatformNotSupportedException) {
      // possible and expected exception
    }
  }

  [Test]
  public void Ctor_IPNetworkProfileNull()
    => Assert.Throws<ArgumentNullException>(() => new ConcreteNetworkScanner(networkProfile: null!));

  [Test]
  public void Dispose()
  {
    using var scanner = new ConcreteNetworkScanner(CreatePseudoNetworkProfile());

    Assert.DoesNotThrow(() => scanner.Dispose());

    Assert.Throws<ObjectDisposedException>(() => scanner.GetNetworkProfile());

#pragma warning disable CA2012
    Assert.Throws<ObjectDisposedException>(() => scanner.ScanAsync());
    Assert.ThrowsAsync<ObjectDisposedException>(async () => await scanner.ScanAsync());

    Assert.Throws<ObjectDisposedException>(() => scanner.ScanAsync(new[] { IPAddress.Any }));
    Assert.ThrowsAsync<ObjectDisposedException>(async () => await scanner.ScanAsync(new[] { IPAddress.Any }));
#pragma warning restore CA2012

    Assert.DoesNotThrow(() => scanner.Dispose(), "dispose again");
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

    Assert.ThrowsAsync<InvalidOperationException>(async () => await scanner.ScanAsync());
  }

  [Test]
  public void ScanAsync_CancellationRequested()
  {
    using var scanner = new ConcreteNetworkScanner(CreatePseudoNetworkProfile());
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    var ex = Assert.CatchAsync(async () => await scanner.ScanAsync(cts.Token));

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }

  [Test]
  public void ScanAsync_WithAddresses_AddressesNull()
  {
    using var scanner = new ConcreteNetworkScanner(CreatePseudoNetworkProfile());

    Assert.ThrowsAsync<ArgumentNullException>(
      async () => await scanner.ScanAsync(addresses: null!)
    );
  }

  [Test]
  public void ScanAsync_WithAddresses_CancellationRequested()
  {
    using var scanner = new ConcreteNetworkScanner(CreatePseudoNetworkProfile());
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    var ex = Assert.CatchAsync(async () => await scanner.ScanAsync(new[] { IPAddress.Any }, cts.Token));

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }
}
