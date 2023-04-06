// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressResolution;

public abstract class MacAddressResolverBase :
  IDisposable,
  IAddressResolver<PhysicalAddress, IPAddress>,
  IAddressResolver<IPAddress, PhysicalAddress>
{
  protected static PhysicalAddress AllZeroMacAddress => PhysicalAddressExtensions.AllZeroMacAddress;

  public static MacAddressResolverBase Null { get; } = new NullMacAddressResolver();

  /*
   * instance members
   */
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
  private bool disposed = false;

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    disposed = true;
  }

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
  public ValueTask RefreshCacheAsync(
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

    return RefreshCacheAsyncCore(cancellationToken);
  }

  protected virtual ValueTask RefreshCacheAsyncCore(
    CancellationToken cancellationToken
  )
    =>
      // do nothing in this class
#if SYSTEM_THREADING_TASKS_VALUETASK_COMPLETEDTASK
      ValueTask.CompletedTask;
#else
      default;
#endif

  public ValueTask RefreshInvalidatedCacheAsync(
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

    return RefreshInvalidatedCacheAsyncCore(cancellationToken);
  }

  protected virtual ValueTask RefreshInvalidatedCacheAsyncCore(
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
