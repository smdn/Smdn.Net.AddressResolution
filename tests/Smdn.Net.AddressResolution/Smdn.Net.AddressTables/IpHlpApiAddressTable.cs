// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;
using System.Runtime.InteropServices;

using NUnit.Framework;

namespace Smdn.Net.AddressTables;

[TestFixture]
public class IpHlpApiAddressTableTests {
  [Test]
  public void IsSupported()
    => Assert.DoesNotThrow(() => Assert.That(IpHlpApiAddressTable.IsSupported, Is.Not.Null));

  [Test]
  public void EnumerateEntriesAsync()
  {
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      return;

    if (!IpHlpApiAddressTable.IsSupported) {
      Assert.Fail($"{nameof(IpHlpApiAddressTable)} is not supported on this platform.");
      return;
    }

    using var table = new IpHlpApiAddressTable();
    var enumerated = false;

    Assert.DoesNotThrowAsync(async () => {
      await foreach (var entry in table.EnumerateEntriesAsync()) {
        enumerated = true;

        Assert.AreNotEqual(IPAddress.Any, entry.IPAddress, nameof(entry.IPAddress));

        if (!entry.IsPermanent)
          Assert.AreNotEqual(AddressTableEntryState.None, entry.State, nameof(entry.State));
      }
    });

    Assert.IsTrue(enumerated, nameof(enumerated));
  }
}
