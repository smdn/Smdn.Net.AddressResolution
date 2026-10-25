// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

public abstract class NetworkScannerTestsBase {
  protected abstract INetworkScanner CreateNetworkScanner();

  [Test]
  public virtual void Dispose()
  {
    using var scanner = CreateNetworkScanner();

    Assert.That(scanner.Dispose, Throws.Nothing, scanner.GetType().FullName!);

    Assert.That(
      () => scanner.ScanAsync(cancellationToken: default),
      Throws.TypeOf<ObjectDisposedException>(),
      scanner.GetType().FullName!
    );
    Assert.That(
      async () => await scanner.ScanAsync(cancellationToken: default),
      Throws.TypeOf<ObjectDisposedException>(),
      scanner.GetType().FullName!
    );

    Assert.That(
      () => scanner.ScanAsync([IPAddress.Any], cancellationToken: default),
      Throws.TypeOf<ObjectDisposedException>(),
      scanner.GetType().FullName!
    );
    Assert.That(
      async () => await scanner.ScanAsync([IPAddress.Any], cancellationToken: default),
      Throws.TypeOf<ObjectDisposedException>(),
      scanner.GetType().FullName!
    );

    Assert.DoesNotThrow(scanner.Dispose, $"{scanner.GetType().FullName} dispose again");
  }

  [Test]
  public void ScanAsync_CancellationRequested()
  {
    using var scanner = CreateNetworkScanner();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    Assert.That(
      async () => await scanner.ScanAsync(cts.Token),
      Throws.InstanceOf<OperationCanceledException>(),
      scanner.GetType().FullName!
    );
  }

  [Test]
  public void ScanAsync_WithAddresses_AddressesNull()
  {
    using var scanner = CreateNetworkScanner();

    Assert.That(
      async () => await scanner.ScanAsync(addresses: null!, cancellationToken: default),
      Throws.TypeOf<ArgumentNullException>(),
      scanner.GetType().FullName!
    );
  }

  [Test]
  public void ScanAsync_WithAddresses_CancellationRequested()
  {
    using var scanner = CreateNetworkScanner();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    Assert.That(
      async () => await scanner.ScanAsync([IPAddress.Any], cts.Token),
      Throws.InstanceOf<OperationCanceledException>(),
      scanner.GetType().FullName!
    );
  }
}
