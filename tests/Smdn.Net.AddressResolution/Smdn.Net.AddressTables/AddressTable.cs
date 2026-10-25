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
  public void Create()
  {
    try {
      Assert.That(AddressTable.Create(), Is.Not.Null);
    }
    catch (PlatformNotSupportedException) {
      // possible and expected exception
    }
  }

  [Test]
  public void Dispose()
  {
    using var table = new ConcreteAddressTable();

    Assert.That(table.Dispose, Throws.Nothing);

    Assert.That(
      () => table.EnumerateEntriesAsync(),
      Throws.TypeOf<ObjectDisposedException>()
    );
    Assert.That(
      async () => {
        await foreach (var entry in table.EnumerateEntriesAsync()) {
          Assert.Fail("unexpected enumeration");
        }
      },
      Throws.TypeOf<ObjectDisposedException>()
    );

    Assert.That(table.Dispose, Throws.Nothing, "dispose again");
  }

  [Test]
  public void EnumerateEntriesAsync_CancellationRequested()
  {
    using var table = new ConcreteAddressTable();
    using var cts = new CancellationTokenSource();

    cts.Cancel();

    Assert.That(
      async () => {
        await foreach (var entry in table.EnumerateEntriesAsync(cts.Token)) {
          Assert.Fail("unexpected enumeration");
        }
      },
      Throws.InstanceOf<OperationCanceledException>()
    );
  }
}
