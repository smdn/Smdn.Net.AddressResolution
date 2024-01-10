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
    => Assert.DoesNotThrow(() => Assert.That(IpHlpApiAddressTable.IsSupported, Is.True.Or.False));

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

        Assert.That(entry.IPAddress, Is.Not.EqualTo(IPAddress.Any), nameof(entry.IPAddress));

        if (!entry.IsPermanent)
          Assert.That(entry.State, Is.Not.EqualTo(AddressTableEntryState.None), nameof(entry.State));
      }
    });

    Assert.That(enumerated, Is.True, nameof(enumerated));
  }
}
