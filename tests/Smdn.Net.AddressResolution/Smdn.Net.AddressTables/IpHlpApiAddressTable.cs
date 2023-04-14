// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
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
    if (!IpHlpApiAddressTable.IsSupported) {
      Assert.Ignore($"{nameof(IpHlpApiAddressTable)} is not supported on this platform.");
      return;
    }

    using var table = new IpHlpApiAddressTable();
    var enumerated = false;

    Assert.DoesNotThrowAsync(async () => {
      await foreach (var entry in table.EnumerateEntriesAsync()) {
        enumerated = true;
      }
    });

    Assert.IsTrue(enumerated, nameof(enumerated));
  }
}
