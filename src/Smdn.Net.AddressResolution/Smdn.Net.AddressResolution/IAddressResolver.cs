// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.AddressResolution;

/// <summary>
/// Provides a mechanism for resolving address of <typeparamref name="TAddress"/> to the corresponding address of <typeparamref name="TResolvedAddress"/>.
/// </summary>
/// <typeparam name="TAddress">The address type to be resolved to the corresponding address type <typeparamref name="TResolvedAddress"/>.</typeparam>
/// <typeparam name="TResolvedAddress">The address type that is resolved from and corresponds to the address type <typeparamref name="TAddress"/>.</typeparam>
public interface IAddressResolver<TAddress, TResolvedAddress>
  where TAddress : notnull
  where TResolvedAddress : notnull
{
  /// <summary>
  /// Resolves from a address of <typeparamref name="TAddress"/> to its corresponding address of <typeparamref name="TResolvedAddress"/>.
  /// </summary>
  /// <param name="address">The address of <typeparamref name="TAddress" /> to be resolved.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  /// <returns>
  /// A <see cref="ValueTask{TResolvedAddress}"/> representing the result of address resolution.
  /// If the address is successfully resolved, <typeparamref name="TResolvedAddress"/> representing the resolved address is set. If not, <see langword="null" /> is set.
  /// </returns>
  /// <seealso cref="Invalidate(TAddress)"/>
  ValueTask<TResolvedAddress?> ResolveAsync(TAddress address, CancellationToken cancellationToken);

  /// <summary>
  /// Marks the <paramref name="address"/> as 'invalidated', for example, if the resolved <typeparamref name="TResolvedAddress"/>
  /// corresponding to the <paramref name="address"/> is unreachable or expired.
  /// </summary>
  /// <param name="address">The <typeparamref name="TAddress"/> to mark as 'invalidated'.</param>
  /// <seealso cref="ResolveAsync(TAddress, CancellationToken)"/>
  void Invalidate(TAddress address);
}
