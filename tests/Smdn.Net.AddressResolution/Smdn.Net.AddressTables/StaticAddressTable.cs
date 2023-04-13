// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace Smdn.Net.AddressTables;

internal sealed class StaticAddressTable : IAddressTable {
  private readonly IList<AddressTableEntry> staticEntries;

  public StaticAddressTable(IList<AddressTableEntry> staticEntries)
  {
    this.staticEntries = staticEntries;
  }

  public void Dispose()
  {
  }

  public IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken)
    => staticEntries.ToAsyncEnumerable();
}
