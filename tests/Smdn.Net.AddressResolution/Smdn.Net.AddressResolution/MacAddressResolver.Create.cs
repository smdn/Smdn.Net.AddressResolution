// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Runtime.InteropServices;

using NUnit.Framework;

namespace Smdn.Net.AddressResolution;

partial class MacAddressResolverTests {
  [Test]
  public void Create()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
      Assert.DoesNotThrow(() => {
        try {
          new MacAddressResolver();
        }
        catch (PlatformNotSupportedException) {
          // expected
        }
      });
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      Assert.DoesNotThrow(() => {
        try {
          new MacAddressResolver();
        }
        catch (PlatformNotSupportedException) {
          // expected
        }
      });
    }
    else {
      Assert.Throws<PlatformNotSupportedException>(() => new MacAddressResolver());
    }
  }
}
