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
    Assert.DoesNotThrow(() => AddressTable.Null.Dispose());

    Assert.DoesNotThrowAsync(async () => {
      await foreach (var entry in AddressTable.Null.EnumerateEntriesAsync(cancellationToken: default)) {
        Assert.Fail("unexpected enumeration");
      }
    });

    Assert.DoesNotThrow(() => AddressTable.Null.Dispose(), "dispose again");
  }

  [Test]
  public void EnumerateEntriesAsync_CancellationRequested()
  {
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    var ex = Assert.CatchAsync(async () => {
      await foreach (var entry in AddressTable.Null.EnumerateEntriesAsync(cts.Token)) {
        Assert.Fail("unexpected enumeration");
      }
    });

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }
}
