// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace Smdn.Net.AddressResolution;

partial class MacAddressResolverTests {
  [Test]
  public void Create()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/proc/net/arp")) {
      var options = new MacAddressResolverOptions() {
        NmapTargetSpecification = "127.0.0.1"
      };
      var resolver = MacAddressResolver.Create(options);

      Assert.IsNotNull(resolver);
    }
    else {
      Assert.Throws<PlatformNotSupportedException>(() => MacAddressResolver.Create());
    }
  }
}
