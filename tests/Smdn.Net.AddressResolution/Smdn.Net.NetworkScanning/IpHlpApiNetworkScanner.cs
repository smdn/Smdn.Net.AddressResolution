// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class IpHlpApiNetworkScannerTests : NetworkScannerTestsBase {
  protected override INetworkScanner CreateNetworkScanner() =>
    new IpHlpApiNetworkScanner(
      IPNetworkProfile.Create(
        addressRangeGenerator: static () => [IPAddress.Any]
      )
    );

  [Test]
  public void IsSupported()
    => Assert.That(() => IpHlpApiNetworkScanner.IsSupported, Throws.Nothing);

  [Test]
  public void Ctor()
    => Assert.That(() => new IpHlpApiNetworkScanner(networkProfile: null!), Throws.ArgumentNullException);
}
