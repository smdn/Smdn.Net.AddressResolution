// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressTables;

/// <summary>
/// Provides a mechanism for referencing address tables such as ARP table.
/// </summary>
public abstract partial class AddressTable : IAddressTable {
  private bool isDisposed;

  protected ILogger? Logger { get; }

  protected AddressTable(
    ILogger? logger = null
  )
  {
    Logger = logger;
  }

  /// <inheritdoc cref="IDisposable.Dispose" />
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    isDisposed = true;
  }

  protected void ThrowIfDisposed()
  {
    if (isDisposed)
      throw new ObjectDisposedException(GetType().FullName);
  }

  /// <summary>
  /// Refers to and enumerates the list of entries from the address table.
  /// </summary>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  public IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsync(
    CancellationToken cancellationToken = default
  )
  {
    cancellationToken.ThrowIfCancellationRequested();

    ThrowIfDisposed();

    return EnumerateEntriesAsyncCore(cancellationToken);
  }

  protected abstract IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsyncCore(CancellationToken cancellationToken);
}
