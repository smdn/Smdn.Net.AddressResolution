// SPDX-FileCopyrightText: 2026 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Threading;

namespace Smdn.Net.AddressTables;

partial class IpHlpApiAddressTable {
  public static partial bool IsSupported => false;

  protected override IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsyncCore(
    CancellationToken cancellationToken
  )
    => throw new PlatformNotSupportedException();
}
