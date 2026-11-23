// SPDX-FileCopyrightText: 2026 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.NetworkScanning;

partial class IpHlpApiNetworkScanner {
  public static partial bool IsSupported => false;

  protected override ValueTask ScanAsyncCore(
    IPAddress address,
    CancellationToken cancellationToken = default
  )
    => throw new PlatformNotSupportedException();
}
