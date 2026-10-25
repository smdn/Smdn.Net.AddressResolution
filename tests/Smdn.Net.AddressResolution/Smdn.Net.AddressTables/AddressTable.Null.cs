// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.AddressTables;

[TestFixture]
public class AddressTableNullObjectTests {
  [Test]
  public void Dispose()
  {
    Assert.DoesNotThrow(AddressTable.Null.Dispose);

    Assert.That(
      async () => {
        await foreach (var entry in AddressTable.Null.EnumerateEntriesAsync(cancellationToken: default)) {
          Assert.Fail("unexpected enumeration");
        }
      },
      Throws.Nothing
    );

    Assert.DoesNotThrow(AddressTable.Null.Dispose, "dispose again");
  }

  [Test]
  public void EnumerateEntriesAsync_CancellationRequested()
  {
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    Assert.That(
      async () => {
        await foreach (var entry in AddressTable.Null.EnumerateEntriesAsync(cts.Token)) {
          Assert.Fail("unexpected enumeration");
        }
      },
      Throws.InstanceOf<OperationCanceledException>()
    );
  }
}
