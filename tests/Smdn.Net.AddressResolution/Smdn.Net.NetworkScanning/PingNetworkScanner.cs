// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class PingNetworkScannerTests : NetworkScannerTestsBase {
  protected override INetworkScanner CreateNetworkScanner() =>
    new PingNetworkScanner(
      IPNetworkProfile.Create(
        addressRangeGenerator: static () => new[] { IPAddress.Any }
      )
    );

  [Test]
  public void IsSupported()
    => Assert.IsTrue(PingNetworkScanner.IsSupported, $"{nameof(PingNetworkScanner.IsSupported)} must be always true");

  [Test]
  public void Ctor()
    => Assert.Throws<ArgumentNullException>(() => new PingNetworkScanner(networkProfile: null!));
}
