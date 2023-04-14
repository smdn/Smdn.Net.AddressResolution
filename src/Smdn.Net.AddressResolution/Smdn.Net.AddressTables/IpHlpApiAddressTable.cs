// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if NET5_0_OR_GREATER
#define SYSTEM_RUNTIME_EXCEPTIONSERVICES_EXCEPTIONDISPATCHINFO_SETCURRENTSTACKTRACE
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if SYSTEM_RUNTIME_EXCEPTIONSERVICES_EXCEPTIONDISPATCHINFO_SETCURRENTSTACKTRACE
using System.Runtime.ExceptionServices;
#endif
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

namespace Smdn.Net.AddressTables;

public sealed class IpHlpApiAddressTable : AddressTable {
  public static bool IsSupported => lazyIsSupported.Value;

  private static readonly Lazy<bool> lazyIsSupported = new(
    valueFactory: static () => {
      try {
        var ret = GetIpNetTable2(ADDRESS_FAMILY.AF_UNSPEC, out var table);

        table.Dispose();

        return ret.Succeeded;
      }
      catch (EntryPointNotFoundException) {
        return false;
      }
      catch (DllNotFoundException) {
        return false;
      }
    },
    isThreadSafe: true
  );

  public IpHlpApiAddressTable(IServiceProvider? serviceProvider = null)
    : base(logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<IpHlpApiAddressTable>())
  {
  }

  protected override async IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsyncCore(
    [EnumeratorCancellation] CancellationToken cancellationToken
  )
  {
    using var table = await GetIpNetTable2Async().ConfigureAwait(false);

    foreach (var ipnetRow2 in table.Table) {
      cancellationToken.ThrowIfCancellationRequested();

      Logger?.LogTrace(
        "MIB_IPNET_ROW2 Address={Address} PhysicalAddress={PhysicalAddress} Flags={Flags} State={State} InterfaceIndex={InterfaceIndex} InterfaceLuid={InterfaceLuid} ReachabilityTime={ReachabilityTime}",
        ipnetRow2.Address,
        PhysicalAddressExtensions.ToMacAddressString(
          ipnetRow2.PhysicalAddress,
          (int)ipnetRow2.PhysicalAddressLength
        ),
        ipnetRow2.Flags,
        ipnetRow2.State,
        ipnetRow2.InterfaceIndex,
        ipnetRow2.InterfaceLuid,
        ipnetRow2.ReachabilityTime
      );

      if (TryParseAddressTableEntry(ipnetRow2, out var entry))
        yield return entry;
    }

    ValueTask<MIB_IPNET_TABLE2> GetIpNetTable2Async()
    {
      var ret = GetIpNetTable2(ADDRESS_FAMILY.AF_UNSPEC, out var table);

      if (ret.Failed) {
        Logger?.LogWarning("GetIpNetTable2 {Result}", ret.ToString());

        table.Dispose();

        var ex = ret.GetException();

#if SYSTEM_RUNTIME_EXCEPTIONSERVICES_EXCEPTIONDISPATCHINFO_SETCURRENTSTACKTRACE
        ex = ExceptionDispatchInfo.SetCurrentStackTrace(ex);
#endif

#if SYSTEM_THREADING_TASKS_VALUETASK_FROMEXCEPTION
        return ValueTask.FromException<MIB_IPNET_TABLE2>(ex);
#else
        return ValueTaskShim.FromException<MIB_IPNET_TABLE2>(ex);
#endif
      }

#if SYSTEM_THREADING_TASKS_VALUETASK_FROMRESULT
      return ValueTask.FromResult(table);
#else
      return ValueTaskShim.FromResult(table);
#endif
    }
  }

  private bool TryParseAddressTableEntry(MIB_IPNET_ROW2 ipnetRow2, out AddressTableEntry entry)
  {
    entry = default;

    if (ipnetRow2.Address.si_family is not ADDRESS_FAMILY.AF_INET or ADDRESS_FAMILY.AF_INET6)
      return false;

    string? interfaceId = null;
    var ret = ConvertInterfaceLuidToGuid(ipnetRow2.InterfaceLuid, out var interfaceGuid);

    if (ret.Succeeded) {
      // NetworkInterface.Id is set to a string that represents a network interface
      // GUID in 'B' format, so convert the retrieved GUID to a string in 'B' format
      // to be able to compare with this value.
      interfaceId = interfaceGuid.ToString(format: "B");
    }
    else {
      Logger?.LogWarning("ConvertInterfaceLuidToGuid {Result}", ret.ToString());

      interfaceId = null;
    }

#if false
    const int NDIS_IF_MAX_STRING_SIZE = 256;

    var interfaceNameBuffer = new StringBuilder(capacity: NDIS_IF_MAX_STRING_SIZE + 1);
    var ret = ConvertInterfaceLuidToName(ipnetRow2.InterfaceLuid, interfaceNameBuffer, NDIS_IF_MAX_STRING_SIZE);

    if (ret.Failed) {
      Logger?.LogWarning("ConvertInterfaceLuidToName {Result}", ret.ToString());

      interfaceId = ipnetRow2.InterfaceLuid.ToString();
    }
    else {
      interfaceId = interfaceNameBuffer.ToString();
    }
#endif

    entry = new(
      ipAddress: ipnetRow2.Address.si_family switch {
        ADDRESS_FAMILY.AF_INET6 => new((byte[])ipnetRow2.Address.Ipv6.sin6_addr),
        ADDRESS_FAMILY.AF_INET or _ => new((byte[])ipnetRow2.Address.Ipv4.sin_addr),
      },
      physicalAddress: ipnetRow2.PhysicalAddressLength == 0
        ? null
        : new(ipnetRow2.PhysicalAddress.AsSpan(0, (int)ipnetRow2.PhysicalAddressLength).ToArray()),
      isPermanent: ipnetRow2.State == NL_NEIGHBOR_STATE.NlnsPermanent,
      state: ipnetRow2.State switch {
        NL_NEIGHBOR_STATE.NlnsIncomplete => AddressTableEntryState.Incomplete,
        NL_NEIGHBOR_STATE.NlnsProbe => AddressTableEntryState.Probe,
        NL_NEIGHBOR_STATE.NlnsDelay => AddressTableEntryState.Delay,
        NL_NEIGHBOR_STATE.NlnsStale => AddressTableEntryState.Stale,
        NL_NEIGHBOR_STATE.NlnsReachable => AddressTableEntryState.Reachable,

        NL_NEIGHBOR_STATE.NlnsUnreachable or
        NL_NEIGHBOR_STATE.NlnsPermanent or
        _ => AddressTableEntryState.None,
      },
      interfaceId: interfaceId
    );

    return true;
  }
}
