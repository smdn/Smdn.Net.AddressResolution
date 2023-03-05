// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smdn.Net.AddressResolution.Arp;

namespace Smdn.Net.AddressResolution;

public abstract class MacAddressResolver :
  IDisposable,
  IAddressResolver<PhysicalAddress, IPAddress>,
  IAddressResolver<IPAddress, PhysicalAddress>
{
  protected static readonly PhysicalAddress AllZeroMacAddress = new(new byte[6]); // 00:00:00:00:00:00

  public static MacAddressResolver Null { get; } = new NullMacAddressResolver();

  public static MacAddressResolver Create(
    MacAddressResolverOptions? options = null,
    IServiceProvider? serviceProvider = null
  )
  {
    options ??= MacAddressResolverOptions.Default;

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
      if (ProcfsArpMacAddressResolver.IsSupported)
        return ProcfsArpMacAddressResolver.Create(options, serviceProvider);
    }

    throw new PlatformNotSupportedException();
  }

  /*
   * instance members
   */
  protected ILogger? Logger { get; }

  protected MacAddressResolver(
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

#pragma warning disable SA1305
  public ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsync(
    IPAddress ipAddress,
    CancellationToken cancellationToken = default
  )
#pragma warning restore SA1305
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

    // TODO: validate IP address

    return ResolveAsync();

    async ValueTask<PhysicalAddress?> ResolveAsync()
    {
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

#pragma warning disable SA1305
  protected abstract ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(
    IPAddress ipAddress,
    CancellationToken cancellationToken
  );
#pragma warning restore SA1305

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

    // TODO: validate MAC address

    return ResolveAsync();

    async ValueTask<IPAddress?> ResolveAsync()
    {
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
}
