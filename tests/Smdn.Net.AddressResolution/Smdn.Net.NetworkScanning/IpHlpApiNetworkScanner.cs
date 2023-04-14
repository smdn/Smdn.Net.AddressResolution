// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class IpHlpApiNetworkScannerTests {
  [Test]
  public void Ctor()
    => Assert.Throws<ArgumentNullException>(() => new IpHlpApiNetworkScanner(networkProfile: null!));
}
