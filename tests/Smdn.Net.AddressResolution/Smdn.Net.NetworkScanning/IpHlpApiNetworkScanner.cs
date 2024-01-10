// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class IpHlpApiNetworkScannerTests : NetworkScannerTestsBase {
  protected override INetworkScanner CreateNetworkScanner() =>
    new IpHlpApiNetworkScanner(
      IPNetworkProfile.Create(
        addressRangeGenerator: static () => new[] { IPAddress.Any }
      )
    );

  [Test]
  public void IsSupported()
    => Assert.DoesNotThrow(() => Assert.That(IpHlpApiNetworkScanner.IsSupported, Is.True.Or.False));

  [Test]
  public void Ctor()
    => Assert.Throws<ArgumentNullException>(() => new IpHlpApiNetworkScanner(networkProfile: null!));
}
