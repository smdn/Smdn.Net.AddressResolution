// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.AddressResolution;

public interface IAddressResolver<TAddress, TResolvedAddress> {
  /// <returns>An resolved address. <see langword="null"/> if address could not be resolved.</returns>
  ValueTask<TResolvedAddress?> ResolveAsync(TAddress address, CancellationToken cancellationToken);
  void Invalidate(TAddress address);
}
