// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class IpHlpApiNetworkScannerTests : NetworkScannerTestsBase {
  protected override INetworkScanner CreateNetworkScanner() =>
#pragma warning disable CA1416
    new IpHlpApiNetworkScanner(
      IPNetworkProfile.Create(
        addressRangeGenerator: static () => new[] { IPAddress.Any }
      )
    );
#pragma warning restore CA1416

  [Test]
  public void IsSupported()
#pragma warning disable CA1416
    => Assert.DoesNotThrow(() => Assert.That(IpHlpApiNetworkScanner.IsSupported, Is.True.Or.False));
#pragma warning restore CA1416

  [Test]
  public void Ctor()
#pragma warning disable CA1416
    => Assert.Throws<ArgumentNullException>(() => new IpHlpApiNetworkScanner(networkProfile: null!));
#pragma warning restore CA1416
}
