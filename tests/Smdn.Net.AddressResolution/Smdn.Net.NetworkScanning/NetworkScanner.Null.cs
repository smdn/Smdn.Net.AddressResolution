// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class NetworkScannerNullObjectTests : NetworkScannerTestsBase {
  protected override INetworkScanner CreateNetworkScanner() => NetworkScanner.Null;

  [Test]
  public override void Dispose()
  {
    Assert.DoesNotThrow(() => NetworkScanner.Null.Dispose());

    Assert.DoesNotThrowAsync(async () => await NetworkScanner.Null.ScanAsync(cancellationToken: default));
    Assert.DoesNotThrowAsync(async () => await NetworkScanner.Null.ScanAsync(new[] { IPAddress.Any }, cancellationToken: default));

    Assert.DoesNotThrow(() => NetworkScanner.Null.Dispose(), "dispose again");
  }
}
