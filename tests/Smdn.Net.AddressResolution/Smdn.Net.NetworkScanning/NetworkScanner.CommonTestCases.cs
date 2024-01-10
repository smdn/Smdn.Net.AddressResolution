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

    Assert.DoesNotThrow(scanner.Dispose, scanner.GetType().FullName!);

#pragma warning disable CA2012
    Assert.Throws<ObjectDisposedException>(() => scanner.ScanAsync(cancellationToken: default), scanner.GetType().FullName!);
    Assert.ThrowsAsync<ObjectDisposedException>(async () => await scanner.ScanAsync(cancellationToken: default), scanner.GetType().FullName!);

    Assert.Throws<ObjectDisposedException>(() => scanner.ScanAsync(new[] { IPAddress.Any }, cancellationToken: default), scanner.GetType().FullName!);
    Assert.ThrowsAsync<ObjectDisposedException>(async () => await scanner.ScanAsync(new[] { IPAddress.Any }, cancellationToken: default), scanner.GetType().FullName!);
#pragma warning restore CA2012

    Assert.DoesNotThrow(scanner.Dispose, $"{scanner.GetType().FullName} dispose again");
  }

  [Test]
  public void ScanAsync_CancellationRequested()
  {
    using var scanner = CreateNetworkScanner();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    var ex = Assert.CatchAsync(
      async () => await scanner.ScanAsync(cts.Token),
      scanner.GetType().FullName!
    );

    Assert.That(
      ex,
      Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>(),
      scanner.GetType().FullName
    );
  }

  [Test]
  public void ScanAsync_WithAddresses_AddressesNull()
  {
    using var scanner = CreateNetworkScanner();

    Assert.ThrowsAsync<ArgumentNullException>(
      async () => await scanner.ScanAsync(addresses: null!, cancellationToken: default),
      scanner.GetType().FullName!
    );
  }

  [Test]
  public void ScanAsync_WithAddresses_CancellationRequested()
  {
    using var scanner = CreateNetworkScanner();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    var ex = Assert.CatchAsync(
      async () => await scanner.ScanAsync(new[] { IPAddress.Any }, cts.Token),
      scanner.GetType().FullName!
    );

    Assert.That(
      ex,
      Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>(),
      scanner.GetType().FullName
    );
  }
}
