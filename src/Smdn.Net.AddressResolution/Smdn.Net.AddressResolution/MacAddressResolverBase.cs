// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressResolution;

/// <summary>
/// Provides an abstract mechanism for the mutual address resolution between IP addresses and corresponding MAC addresses.
/// </summary>
public abstract class MacAddressResolverBase :
  IDisposable,
  IAddressResolver<PhysicalAddress, IPAddress>,
  IAddressResolver<IPAddress, PhysicalAddress>
{
  protected static PhysicalAddress AllZeroMacAddress => PhysicalAddressExtensions.AllZeroMacAddress;

  /// <summary>
  /// Gets an empty implementation of <see cref="MacAddressResolverBase"/> that returns all addresses as unresolvable (<see langworkd="null"/>).
  /// </summary>
  public static MacAddressResolverBase Null { get; } = new NullMacAddressResolver();

  /*
   * instance members
   */

  /// <summary>
  /// Gets a value indicating whether any of the addresses have been invalidated.
  /// </summary>
  /// <remarks>
  /// <para>
  /// If <see langword="true" />, updating for the invalidated addresses by invoking <see cref="RefreshInvalidatedAddressesAsync(CancellationToken)" />
  /// will be triggered by a call to <see cref="ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)" /> or
  /// <see cref="ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)" />.
  /// </para>
  /// <para>
  /// Calling <see cref="Invalidate(IPAddress)" /> or <see cref="Invalidate(PhysicalAddress)" /> method sets this property to <see langword="true" />.
  /// </para>
  /// </remarks>
  /// <seealso cref="Invalidate(IPAddress)"/>
  /// <seealso cref="Invalidate(PhysicalAddress)"/>
  /// <seealso cref="RefreshInvalidatedAddressesAsync(CancellationToken)"/>
  /// <seealso cref="ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)"/>
  /// <seealso cref="ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)"/>
  public abstract bool HasInvalidated { get; }

  protected ILogger? Logger { get; }

  protected MacAddressResolverBase(
    ILogger? logger = null
  )
  {
    Logger = logger;
  }

  /*
   * IDisposable
   */
  private bool disposed;

  /// <inheritdoc cref="IDisposable.Dispose" />
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    disposed = true;
  }

  /// <summary>
  /// Throws <see cref="ObjectDisposedException"/> if instance has been marked as disposed.
  /// </summary>
  /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
  protected void ThrowIfDisposed()
  {
    if (disposed)
      throw new ObjectDisposedException(GetType().FullName);
  }

  /*
   * IPAddress -> PhysicalAddress
   */
  ValueTask<PhysicalAddress?> IAddressResolver<IPAddress, PhysicalAddress>.ResolveAsync(
    IPAddress address,
    CancellationToken cancellationToken
  )
    => ResolveIPAddressToMacAddressAsync(
      ipAddress: address,
      cancellationToken: cancellationToken
    );

  void IAddressResolver<IPAddress, PhysicalAddress>.Invalidate(
    IPAddress address
  )
    => Invalidate(ipAddress: address);

  /// <summary>
  /// Resolves from an IP address to its corresponding MAC address.
  /// </summary>
  /// <param name="ipAddress">The <see cref="IPAddress" /> to be resolved.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests. The default value is <see langword="default" />.</param>
  /// <returns>
  /// A <see cref="ValueTask{PhysicalAddress}"/> representing the result of address resolution.
  /// If the address is successfully resolved, <see cref="PhysicalAddress"/> representing the resolved address is set. If not, <see langword="null" /> is set.
  /// </returns>
  /// <seealso cref="Invalidate(IPAddress)"/>
  /// <seealso cref="ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)"/>
  /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
  public ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsync(
    IPAddress ipAddress,
    CancellationToken cancellationToken = default
  )
  {
    if (cancellationToken.IsCancellationRequested)
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      return ValueTask.FromCanceled<PhysicalAddress?>(cancellationToken);
#else
      return ValueTaskShim.FromCanceled<PhysicalAddress?>(cancellationToken);
#endif

    ThrowIfDisposed();

    if (ipAddress is null)
      throw new ArgumentNullException(nameof(ipAddress));

    return ResolveAsync();

    async ValueTask<PhysicalAddress?> ResolveAsync()
    {
      Logger?.LogDebug("Resolving {IPAddress}", ipAddress);

      var resolvedMacAddress = await ResolveIPAddressToMacAddressAsyncCore(
        ipAddress: ipAddress,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      if (resolvedMacAddress is null)
        Logger?.LogDebug("Could not resolved {IPAddress}", ipAddress);
      else
        Logger?.LogDebug("Resolved {IPAddress} to {MacAddress}", ipAddress, resolvedMacAddress.ToMacAddressString());

      return resolvedMacAddress;
    }
  }

  protected abstract ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(
    IPAddress ipAddress,
    CancellationToken cancellationToken
  );

  /// <summary>
  /// Marks the <paramref name="ipAddress"/> as 'invalidated', for example, if the resolved <see cref="PhysicalAddress"/>
  /// corresponding to the <paramref name="ipAddress"/> was unreachable or expired.
  /// </summary>
  /// <remarks>
  /// Invalidated addresses will not be used in subsequent address resolution or will be automatically updated before resolution.
  /// To explicitly update the invalidated addresses, invoke <see cref="RefreshInvalidatedAddressesAsync(CancellationToken)"/>.
  /// </remarks>
  /// <param name="ipAddress">The <see cref="IPAddress"/> to mark as 'invalidated'.</param>
  /// <seealso cref="ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)"/>
  /// <seealso cref="RefreshInvalidatedAddressesAsync(CancellationToken)"/>
  /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
  public void Invalidate(IPAddress ipAddress)
  {
    if (ipAddress is null)
      throw new ArgumentNullException(nameof(ipAddress));

    ThrowIfDisposed();

    Logger?.LogDebug("Invalidating {IPAddress}", ipAddress);

    InvalidateCore(ipAddress);
  }

  protected abstract void InvalidateCore(IPAddress ipAddress);

  /*
   * PhysicalAddress -> IPAddress
   */
  ValueTask<IPAddress?> IAddressResolver<PhysicalAddress, IPAddress>.ResolveAsync(
    PhysicalAddress address,
    CancellationToken cancellationToken
  )
    => ResolveMacAddressToIPAddressAsync(
      macAddress: address,
      cancellationToken: cancellationToken
    );

  void IAddressResolver<PhysicalAddress, IPAddress>.Invalidate(
    PhysicalAddress address
  )
    => Invalidate(macAddress: address);

  /// <summary>
  /// Resolves from a MAC address to its corresponding IP address.
  /// </summary>
  /// <param name="macAddress">The <see cref="PhysicalAddress" /> to be resolved.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests. The default value is <see langword="default" />.</param>
  /// <returns>
  /// A <see cref="ValueTask{IPAddress}"/> representing the result of address resolution.
  /// If the address is successfully resolved, <see cref="IPAddress"/> representing the resolved address is set. If not, <see langword="null" /> is set.
  /// </returns>
  /// <seealso cref="Invalidate(PhysicalAddress)"/>
  /// <seealso cref="ResolveIPAddressToMacAddressAsync(IPAddress, CancellationToken)"/>
  /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
  public ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsync(
    PhysicalAddress macAddress,
    CancellationToken cancellationToken = default
  )
  {
    if (cancellationToken.IsCancellationRequested)
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      return ValueTask.FromCanceled<IPAddress?>(cancellationToken);
#else
      return ValueTaskShim.FromCanceled<IPAddress?>(cancellationToken);
#endif

    ThrowIfDisposed();

    if (macAddress is null)
      throw new ArgumentNullException(nameof(macAddress));

    if (AllZeroMacAddress.Equals(macAddress))
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      return ValueTask.FromResult<IPAddress?>(null);
#else
      return ValueTaskShim.FromResult<IPAddress?>(null);
#endif

    return ResolveAsync();

    async ValueTask<IPAddress?> ResolveAsync()
    {
      Logger?.LogDebug("Resolving {MacAddress}", macAddress.ToMacAddressString());

      var resolvedIPAddress = await ResolveMacAddressToIPAddressAsyncCore(
        macAddress: macAddress,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      if (resolvedIPAddress is null)
        Logger?.LogDebug("Could not resolved {MacAddress}", macAddress.ToMacAddressString());
      else
        Logger?.LogDebug("Resolved {MacAddress} to {IPAddress}", macAddress.ToMacAddressString(), resolvedIPAddress);

      return resolvedIPAddress;
    }
  }

  protected abstract ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(
    PhysicalAddress macAddress,
    CancellationToken cancellationToken
  );

  /// <summary>
  /// Marks the <paramref name="macAddress"/> as 'invalidated', for example, if the resolved <see cref="IPAddress"/>
  /// corresponding to the <paramref name="macAddress"/> was unreachable or expired.
  /// </summary>
  /// <remarks>
  /// Invalidated addresses will not be used in subsequent address resolution or will be automatically updated before resolution.
  /// To explicitly update the invalidated addresses, invoke <see cref="RefreshInvalidatedAddressesAsync(CancellationToken)"/>.
  /// </remarks>
  /// <param name="macAddress">The <see cref="PhysicalAddress"/> to mark as 'invalidated'.</param>
  /// <seealso cref="ResolveMacAddressToIPAddressAsync(PhysicalAddress, CancellationToken)"/>
  /// <seealso cref="RefreshInvalidatedAddressesAsync(CancellationToken)"/>
  /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
  public void Invalidate(PhysicalAddress macAddress)
  {
    if (macAddress is null)
      throw new ArgumentNullException(nameof(macAddress));

    ThrowIfDisposed();

    Logger?.LogDebug("Invalidating {IPAddress}", macAddress.ToMacAddressString());

    InvalidateCore(macAddress);
  }

  protected abstract void InvalidateCore(PhysicalAddress macAddress);

  /*
   * other virtual/abstract members
   */

  /// <summary>
  /// Requests an update to the mechanism that caches the correspondence between IP addresses and MAC addresses.
  /// </summary>
  /// <remarks>
  /// In a concrete implementation, updates the system cache (e.g., ARP table) by performing network scan.
  /// </remarks>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests. The default value is <see langword="default" />.</param>
  /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
  public ValueTask RefreshAddressTableAsync(
    CancellationToken cancellationToken = default
  )
  {
    if (cancellationToken.IsCancellationRequested)
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      return ValueTask.FromCanceled(cancellationToken);
#else
      return ValueTaskShim.FromCanceled(cancellationToken);
#endif

    ThrowIfDisposed();

    return RefreshAddressTableAsyncCore(cancellationToken);
  }

  protected virtual ValueTask RefreshAddressTableAsyncCore(
    CancellationToken cancellationToken
  )
    =>
      // do nothing in this class
#if SYSTEM_THREADING_TASKS_VALUETASK_COMPLETEDTASK
      ValueTask.CompletedTask;
#else
      default;
#endif

  /// <summary>
  /// Requests an update to the invalidated addresses, to the mechanism that caches the correspondence between IP addresses and MAC addresses.
  /// </summary>
  /// <remarks>
  /// In a concrete implementation, updates the system cache (e.g., ARP table) by performing network scan for the invalidated addresses.
  /// </remarks>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests. The default value is <see langword="default" />.</param>
  /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
  public ValueTask RefreshInvalidatedAddressesAsync(
    CancellationToken cancellationToken = default
  )
  {
    if (cancellationToken.IsCancellationRequested)
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      return ValueTask.FromCanceled(cancellationToken);
#else
      return ValueTaskShim.FromCanceled(cancellationToken);
#endif

    ThrowIfDisposed();

    return RefreshInvalidatedAddressesAsyncCore(cancellationToken);
  }

  protected virtual ValueTask RefreshInvalidatedAddressesAsyncCore(
    CancellationToken cancellationToken
  )
    =>
      // do nothing in this class
#if SYSTEM_THREADING_TASKS_VALUETASK_COMPLETEDTASK
      ValueTask.CompletedTask;
#else
      default;
#endif
}
