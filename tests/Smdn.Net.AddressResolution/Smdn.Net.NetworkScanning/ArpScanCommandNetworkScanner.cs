// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class ArpScanCommandNetworkScannerTests : NetworkScannerTestsBase {
  protected override INetworkScanner CreateNetworkScanner() =>
    new ArpScanCommandNetworkScanner(
      IPNetworkProfile.Create(
        addressRangeGenerator: static () => new[] { IPAddress.Any }
      )
    );

  [Test]
  public void IsSupported()
    => Assert.DoesNotThrow(() => Assert.That(ArpScanCommandNetworkScanner.IsSupported, Is.Not.Null));

  [Test]
  public void Ctor()
    => Assert.DoesNotThrow(() => new ArpScanCommandNetworkScanner(networkProfile: null));
}
