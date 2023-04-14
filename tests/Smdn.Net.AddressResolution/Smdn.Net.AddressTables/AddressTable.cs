// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.AddressTables;

[TestFixture]
public class AddressTableTests {
  private class ConcreteAddressTable : AddressTable {
    public ConcreteAddressTable()
      : base()
    {
    }

    protected override IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsyncCore(
      CancellationToken cancellationToken
    )
      => Array.Empty<AddressTableEntry>().ToAsyncEnumerable();
  }

  [Test]
  public void Dispose()
  {
    using var table = new ConcreteAddressTable();

    Assert.DoesNotThrow(() => table.Dispose());

#pragma warning disable CA2012
    Assert.Throws<ObjectDisposedException>(() => table.EnumerateEntriesAsync());
    Assert.ThrowsAsync<ObjectDisposedException>(async () => {
      await foreach (var entry in table.EnumerateEntriesAsync()) {
        Assert.Fail("unexpected enumeration");
      }
    });
#pragma warning restore CA2012

    Assert.DoesNotThrow(() => table.Dispose(), "dispose again");
  }

  [Test]
  public void ScanAsync_CancellationRequested()
  {
    using var table = new ConcreteAddressTable();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    var ex = Assert.CatchAsync(async () => {
      await foreach (var entry in table.EnumerateEntriesAsync(cts.Token)) {
        Assert.Fail("unexpected enumeration");
      }
    });

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }
}
