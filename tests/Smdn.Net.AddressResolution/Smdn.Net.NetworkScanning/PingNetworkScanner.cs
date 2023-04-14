// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class PingNetworkScannerTests {
  [Test]
  public void IsSupported()
    => Assert.IsTrue(PingNetworkScanner.IsSupported, $"{nameof(PingNetworkScanner.IsSupported)} must be always true");

  [Test]
  public void Ctor()
    => Assert.Throws<ArgumentNullException>(() => new PingNetworkScanner(networkProfile: null!));
}
