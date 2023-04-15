// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
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
    => Assert.DoesNotThrow(() => Assert.That(NmapCommandNetworkScanner.IsSupported, Is.Not.Null));

  [Test]
  public void Ctor()
    => Assert.Throws<ArgumentNullException>(() => new NmapCommandNetworkScanner(networkProfile: null!));
}
