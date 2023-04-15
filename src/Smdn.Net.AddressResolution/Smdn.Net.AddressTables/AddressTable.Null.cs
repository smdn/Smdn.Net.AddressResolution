// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Smdn.Net.AddressTables;

#pragma warning disable IDE0040
partial class AddressTable {
#pragma warning restore IDE0040
  public static IAddressTable Null { get; } = new NullAddressTable();

  private sealed class NullAddressTable : IAddressTable {
    internal NullAddressTable()
    {
    }

    void IDisposable.Dispose()
    {
      // do nothing
    }

#pragma warning disable CS1998
    public async IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsync(
      [EnumeratorCancellation] CancellationToken cancellationToken
    )
#pragma warning restore CS1998
    {
      cancellationToken.ThrowIfCancellationRequested();

      yield break;
    }
  }
}
