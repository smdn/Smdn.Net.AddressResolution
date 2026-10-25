// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class NetworkScannerNullObjectTests : NetworkScannerTestsBase {
  protected override INetworkScanner CreateNetworkScanner() => NetworkScanner.Null;

  [Test]
  public override void Dispose()
  {
    Assert.DoesNotThrow(NetworkScanner.Null.Dispose);

    Assert.That(
      async () => await NetworkScanner.Null.ScanAsync(cancellationToken: default),
      Throws.Nothing
    );
    Assert.That(
      async () => await NetworkScanner.Null.ScanAsync([IPAddress.Any], cancellationToken: default),
      Throws.Nothing
    );

    Assert.DoesNotThrow(NetworkScanner.Null.Dispose, "dispose again");
  }
}
