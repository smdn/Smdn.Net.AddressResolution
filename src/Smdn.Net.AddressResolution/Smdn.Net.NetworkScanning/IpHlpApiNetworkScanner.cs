// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if NET5_0_OR_GREATER
#define SYSTEM_RUNTIME_EXCEPTIONSERVICES_EXCEPTIONDISPATCHINFO_SETCURRENTSTACKTRACE
#endif

using System;
using System.Net;
using System.Net.Sockets;
#if SYSTEM_RUNTIME_EXCEPTIONSERVICES_EXCEPTIONDISPATCHINFO_SETCURRENTSTACKTRACE
using System.Runtime.ExceptionServices;
#endif
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Vanara.PInvoke;

using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

namespace Smdn.Net.NetworkScanning;

public sealed class IpHlpApiNetworkScanner : NetworkScanner {
  private static readonly Win32Error ERROR_BAD_NET_NAME = new(0x80070043u);

  public IpHlpApiNetworkScanner(
    IPNetworkProfile networkProfile,
    IServiceProvider? serviceProvider = null
  )
    : base(
      networkProfile: networkProfile ?? throw new ArgumentNullException(nameof(networkProfile)),
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<IpHlpApiNetworkScanner>()
    )
  {
  }

  protected override async ValueTask ScanAsyncCore(
    IPAddress address,
    CancellationToken cancellationToken = default
  )
  {
    var (succeeded, ipnetRow2) = await TryResolveIpNetEntry2Async(address).ConfigureAwait(false);

    if (succeeded) {
      Logger?.LogDebug(
        "ResolveIpNetEntry2 resolved {IPAddress} => {MacAddress}",
        address,
        PhysicalAddressExtensions.ToMacAddressString(
          ipnetRow2.PhysicalAddress,
          (int)ipnetRow2.PhysicalAddressLength
        )
      );
    }
    else {
      Logger?.LogDebug(
        "ResolveIpNetEntry2 could not resolve {IPAddress}",
        address
      );
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

    Logger?.LogWarning("ResolveIpNetEntry2({Address}) {Result}", address, ret.ToString());

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
