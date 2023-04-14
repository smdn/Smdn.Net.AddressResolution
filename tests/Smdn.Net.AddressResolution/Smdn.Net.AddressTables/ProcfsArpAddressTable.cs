// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using NUnit.Framework;

namespace Smdn.Net.AddressTables;

[TestFixture]
public class ProcfsArpAddressTableTests {
  [Test]
  public void IsSupported()
    => Assert.DoesNotThrow(() => Assert.That(ProcfsArpAddressTable.IsSupported, Is.Not.Null));

  [Test]
  public void EnumerateEntriesAsync()
  {
    if (!ProcfsArpAddressTable.IsSupported) {
      Assert.Ignore($"{nameof(ProcfsArpAddressTable)} is not supported on this platform.");
      return;
    }

    using var table = new ProcfsArpAddressTable();
    var enumerated = false;

    Assert.DoesNotThrowAsync(async () => {
      await foreach (var entry in table.EnumerateEntriesAsync()) {
        enumerated = true;
      }
    });

    Assert.IsTrue(enumerated, "expect one or more entries enumerated");
  }
}
