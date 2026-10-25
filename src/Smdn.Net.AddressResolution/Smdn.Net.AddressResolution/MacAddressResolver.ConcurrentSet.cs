// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Concurrent;

namespace Smdn.Net.AddressResolution;

partial class MacAddressResolver {
  private readonly struct None { }

  private sealed class ConcurrentSet<T> : ConcurrentDictionary<T, None> where T : notnull {
    public ConcurrentSet()
    {
    }

    public void Add(T key)
      => AddOrUpdate(key: key, addValue: default, updateValueFactory: static (key, old) => default);
  }
}
