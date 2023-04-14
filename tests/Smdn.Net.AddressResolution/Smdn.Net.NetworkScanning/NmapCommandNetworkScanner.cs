// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using NUnit.Framework;

namespace Smdn.Net.NetworkScanning;

[TestFixture]
public class NmapCommandNetworkScannerTests {
  [Test]
  public void IsSupported()
    => Assert.DoesNotThrow(() => Assert.That(NmapCommandNetworkScanner.IsSupported, Is.Not.Null));

  [Test]
  public void Ctor()
    => Assert.Throws<ArgumentNullException>(() => new NmapCommandNetworkScanner(networkProfile: null!));
}
