// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
// cSpell:ignore nmap
using System.Net;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class NmapCommandNetworkScannerTests : NetworkScannerTestsBase {
  protected override INetworkScanner CreateNetworkScanner() =>
    new NmapCommandNetworkScanner(
      IPNetworkProfile.Create(
        addressRangeGenerator: static () => new[] { IPAddress.Any }
      )
    );

  [Test]
  public void IsSupported()
    => Assert.That(() => _ = NmapCommandNetworkScanner.IsSupported, Throws.Nothing);

  [Test]
  public void Ctor()
    => Assert.That(() => new NmapCommandNetworkScanner(networkProfile: null!), Throws.ArgumentNullException);
}
