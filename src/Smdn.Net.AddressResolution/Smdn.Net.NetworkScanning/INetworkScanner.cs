// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.NetworkScanning;

/// <summary>
/// Provides a mechanism for performing network scan.
/// </summary>
public interface INetworkScanner : IDisposable {
  /// <summary>
  /// Performs network scan for all targets.
  /// </summary>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  ValueTask ScanAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Performs network scan for the targets specified by <paramref name="addresses"/>.
  /// </summary>
  /// <param name="addresses">The target addresses to perform network scan.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  ValueTask ScanAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken);
}
