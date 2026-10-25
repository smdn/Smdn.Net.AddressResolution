// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Smdn.Net.AddressTables;

partial class AddressTable {
  public static IAddressTable Null { get; } = new NullAddressTable();

  private sealed class NullAddressTable : IAddressTable {
    internal NullAddressTable()
    {
    }

    void IDisposable.Dispose()
    {
      // do nothing
    }

#pragma warning disable IDE0391, CS1998
    public async IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsync(
      [EnumeratorCancellation] CancellationToken cancellationToken
    )
#pragma warning restore IDE0931, CS1998
    {
      cancellationToken.ThrowIfCancellationRequested();

      yield break;
    }
  }
}
