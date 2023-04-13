// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Threading;

namespace Smdn.Net.AddressTables;

internal sealed class PseudoAddressTable : IAddressTable {
  public bool IsDisposed { get; private set; }

  public void Dispose() => IsDisposed = true;

  public IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken)
    => throw new NotImplementedException();
}
