// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace Smdn.Net.AddressTables;

internal sealed class NullAddressTable : IAddressTable {
  public NullAddressTable()
  {
  }

  public void Dispose()
  {
  }

  public IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken)
    => Array.Empty<AddressTableEntry>().ToAsyncEnumerable();
}
