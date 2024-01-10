// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;
using System.Runtime.InteropServices;

using NUnit.Framework;

namespace Smdn.Net.AddressTables;

[TestFixture]
public class ProcfsArpAddressTableTests {
  [Test]
  public void IsSupported()
    => Assert.DoesNotThrow(() => Assert.That(ProcfsArpAddressTable.IsSupported, Is.True.Or.False));

  [Test]
  public void EnumerateEntriesAsync()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      return;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      return;

    if (!ProcfsArpAddressTable.IsSupported) {
      Assert.Ignore($"{nameof(ProcfsArpAddressTable)} is not supported on this platform.");
      return;
    }

    using var table = new ProcfsArpAddressTable();
    var enumerated = false;

    Assert.DoesNotThrowAsync(async () => {
      await foreach (var entry in table.EnumerateEntriesAsync()) {
        enumerated = true;

        Assert.That(entry.IPAddress, Is.Not.EqualTo(IPAddress.Any), nameof(entry.IPAddress));

        if (!entry.IsPermanent)
          Assert.That(entry.State, Is.Not.EqualTo(AddressTableEntryState.None), nameof(entry.State));
      }
    });

    Assert.That(enumerated, Is.True, "expect one or more entries enumerated");
  }
}
