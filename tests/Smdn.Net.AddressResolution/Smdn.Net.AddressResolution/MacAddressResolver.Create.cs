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
      Assert.That(
        () => {
          try {
            new MacAddressResolver();
          }
          catch (PlatformNotSupportedException) {
            // expected
          }
        },
        Throws.Nothing
      );
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      Assert.That(
        () => {
          try {
            new MacAddressResolver();
          }
          catch (PlatformNotSupportedException) {
            // expected
          }
        },
        Throws.Nothing
      );
    }
    else {
      Assert.That(() => new MacAddressResolver(), Throws.TypeOf<PlatformNotSupportedException>());
    }
  }
}
