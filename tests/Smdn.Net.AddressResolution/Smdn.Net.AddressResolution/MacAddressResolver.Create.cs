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
        catch (InvalidOperationException ex) when (IsMandatoryParameterNullException(ex)) {
          // expected
        }
      });
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      Assert.DoesNotThrow(() => {
        try {
          new MacAddressResolver();
        }
        catch (InvalidOperationException ex) when (IsMandatoryParameterNullException(ex)) {
          // expected
        }
      });
    }
    else {
      Assert.Throws<PlatformNotSupportedException>(() => new MacAddressResolver());
    }

    static bool IsMandatoryParameterNullException(InvalidOperationException ex)
      => ex.InnerException is ArgumentNullException exInner &&
        (exInner.ParamName == "networkProfile" || exInner.ParamName == "serviceProvider");
  }
}
