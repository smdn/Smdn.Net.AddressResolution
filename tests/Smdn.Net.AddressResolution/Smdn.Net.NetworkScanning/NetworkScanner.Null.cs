// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class NetworkScannerNullObjectTests {
  [Test]
  public void Dispose()
  {
    Assert.DoesNotThrow(() => NetworkScanner.Null.Dispose());

    Assert.DoesNotThrowAsync(async () => await NetworkScanner.Null.ScanAsync(cancellationToken: default));
    Assert.DoesNotThrowAsync(async () => await NetworkScanner.Null.ScanAsync(new[] { IPAddress.Any }, cancellationToken: default));

    Assert.DoesNotThrow(() => NetworkScanner.Null.Dispose(), "dispose again");
  }

  [Test]
  public void ScanAsync_CancellationRequested()
  {
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    var ex = Assert.CatchAsync(async () => await NetworkScanner.Null.ScanAsync(cts.Token));

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }

  [Test]
  public void ScanAsync_WithAddresses_AddressesNull()
  {
    Assert.ThrowsAsync<ArgumentNullException>(
      async () => await NetworkScanner.Null.ScanAsync(addresses: null!, cancellationToken: default)
    );
  }

  [Test]
  public void ScanAsync_WithAddresses_CancellationRequested()
  {
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    var ex = Assert.CatchAsync(async () => await NetworkScanner.Null.ScanAsync(new[] { IPAddress.Any }, cts.Token));

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }
}
