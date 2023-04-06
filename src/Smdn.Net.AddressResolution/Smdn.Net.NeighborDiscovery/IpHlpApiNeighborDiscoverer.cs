// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if NET5_0_OR_GREATER
#define SYSTEM_RUNTIME_EXCEPTIONSERVICES_EXCEPTIONDISPATCHINFO_SETCURRENTSTACKTRACE
#endif

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
#if SYSTEM_RUNTIME_EXCEPTIONSERVICES_EXCEPTIONDISPATCHINFO_SETCURRENTSTACKTRACE
using System.Runtime.ExceptionServices;
#endif
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Vanara.PInvoke;

using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

namespace Smdn.Net.NeighborDiscovery;

public sealed class IpHlpApiNeighborDiscoverer : INeighborDiscoverer {
  private static readonly Win32Error ERROR_BAD_NET_NAME = new(0x80070043u);

  private readonly Func<IEnumerable<IPAddress>?> getDiscoveryTargetAddresses;
  private readonly ILogger? logger;

  public IpHlpApiNeighborDiscoverer(
    IPNetworkProfile networkProfile,
    ILogger? logger = null
  )
  {
    this.getDiscoveryTargetAddresses = (networkProfile ?? throw new ArgumentNullException(nameof(networkProfile))).GetAddressRange;
    this.logger = logger;
  }

  void IDisposable.Dispose()
  {
    // nothing to do
  }

  public ValueTask DiscoverAsync(
    CancellationToken cancellationToken = default
  )
    => DiscoverAsync(
      addresses: getDiscoveryTargetAddresses() ?? throw new InvalidOperationException("could not get address range"),
      cancellationToken: cancellationToken
    );

  public async ValueTask DiscoverAsync(
    IEnumerable<IPAddress> addresses,
    CancellationToken cancellationToken = default
  )
  {
    if (addresses is null)
      throw new ArgumentNullException(nameof(addresses));

    foreach (var address in addresses) {
      var (succeeded, ipnetRow2) = await TryResolveIpNetEntry2Async(address).ConfigureAwait(false);

      if (succeeded) {
        logger?.LogDebug(
          "ResolveIpNetEntry2 resolved {IPAddress} => {MacAddress}",
          address,
          PhysicalAddressExtensions.ToMacAddressString(
            ipnetRow2.PhysicalAddress,
            (int)ipnetRow2.PhysicalAddressLength
          )
        );
      }
      else {
        logger?.LogDebug(
          "ResolveIpNetEntry2 could not resolve {IPAddress}",
          address
        );
      }
    }
  }

  private ValueTask<(bool Succeeded, MIB_IPNET_ROW2 IpNetRow2)> TryResolveIpNetEntry2Async(
    IPAddress address
  )
  {
    MIB_IPNET_ROW2 row = address.AddressFamily switch {
      AddressFamily.InterNetworkV6 => new(
        ipV6: new(addr: address.GetAddressBytes(), scope_id: (uint)address.ScopeId),
        ifIdx: default
      ),
      AddressFamily.InterNetwork => new(
        ipV4: new SOCKADDR_IN(addr: new IN_ADDR(v4addr: address.GetAddressBytes())),
        ifIdx: default
      ),
      _ => throw new ArgumentException(
        message: $"invalid address familiy ({address.AddressFamily})",
        paramName: nameof(address)
      ),
    };

    var ret = ResolveIpNetEntry2(ref row, SourceAddress: IntPtr.Zero);

    if (ret.Succeeded)
      return new((true, row));
    if (ret.ToHRESULT() == ERROR_BAD_NET_NAME.ToHRESULT())
      return new((false, default));

    logger?.LogWarning("ResolveIpNetEntry2({Address}) {Result}", address, ret.ToString());

    var ex = ret.GetException();

#if SYSTEM_RUNTIME_EXCEPTIONSERVICES_EXCEPTIONDISPATCHINFO_SETCURRENTSTACKTRACE
    ex = ExceptionDispatchInfo.SetCurrentStackTrace(ex);
#endif

#if SYSTEM_THREADING_TASKS_VALUETASK_FROMEXCEPTION
    return ValueTask.FromException<(bool, MIB_IPNET_ROW2)>(ex);
#else
    return ValueTaskShim.FromException<(bool, MIB_IPNET_ROW2)>(ex);
#endif
  }
}
