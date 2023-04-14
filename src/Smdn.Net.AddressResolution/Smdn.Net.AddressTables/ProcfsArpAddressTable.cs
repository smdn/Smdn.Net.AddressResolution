// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if NET7_0_OR_GREATER
#define SYSTEM_IO_STREAMREADER_READLINEASYNC_CANCELLATIONTOKEN
#endif

using System;
using System.Collections.Generic;
#if NULL_STATE_STATIC_ANALYSIS_ATTRIBUTES
using System.Diagnostics.CodeAnalysis;
#endif
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressTables;

public sealed class ProcfsArpAddressTable : AddressTable {
  private const string PathToProcNetArp = "/proc/net/arp";

  public static bool IsSupported => File.Exists(PathToProcNetArp);

  [Flags]
  private enum Flags : byte {
    Incomplete = 0x00,
    Complete = 0x02,
    Permanent = 0x04,
    Publish = 0x08,
    UserTrailers = 0x10,
    NetMask = 0x20,
    DontPub = 0x40,
  }

  public ProcfsArpAddressTable(IServiceProvider? serviceProvider = null)
    : base(serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ProcfsArpAddressTable>())
  {
  }

  protected override async IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsyncCore(
    [EnumeratorCancellation] CancellationToken cancellationToken
  )
  {
    Logger?.LogTrace("Start reading '{PathToProcNetArp}'", PathToProcNetArp);

    using var reader = new StreamReader(PathToProcNetArp);

    for (
#if SYSTEM_IO_STREAMREADER_READLINEASYNC_CANCELLATIONTOKEN
      var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
      line is not null;
      line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)
#else
      var line = await reader.ReadLineAsync().ConfigureAwait(false);
      line is not null;
      line = await reader.ReadLineAsync().ConfigureAwait(false)
#endif
    ) {
      cancellationToken.ThrowIfCancellationRequested();

      if (line.StartsWith("IP address", StringComparison.Ordinal))
        continue; // ignore header line

      if (!TryParse(line, out var ipAddress, out var hardwareType, out var flags, out var hardwareAddress, out var device)) {
        Logger?.LogWarning("Failed to parse line: '{Line}'", line);
        continue;
      }

      Logger?.LogTrace(
        "IP={IPAddress} HW={HardwareAddress} Type=0x{HardwareType} Device={Device} Flags=0x{Flags}",
        ipAddress,
        hardwareAddress?.ToMacAddressString(),
        hardwareType.ToString("X2", provider: null),
        device,
        ((byte)flags).ToString("X2", provider: null)
      );

      AddressTableEntryState state = default;

      // TODO: flags translation
      if (flags.HasFlag(Flags.Incomplete))
        state = AddressTableEntryState.Incomplete;
      if (flags.HasFlag(Flags.Complete))
        state = AddressTableEntryState.Stale;

      var isPermanent = flags.HasFlag(Flags.Permanent);

      yield return new(
        ipAddress: ipAddress!,
        physicalAddress: hardwareAddress,
        isPermanent: isPermanent,
        state: state,
        interfaceId: device
      );
    }
  }

#if !SYSTEM_STRING_SPLIT_CHAR
  private static readonly char[] arpTableEntryDelimiter = new[] { ' ' };
#endif

  // [/proc/net/arp]
  // IP address       HW type     Flags       HW address            Mask     Device
  // 192.168.0.1      0x1         0x0         00:00:00:00:00:00     *        eth0
  // 192.168.0.2      0x1         0x0         00:00:00:00:00:00     *        eth0
  // :                :           :           :                     :        :
  private static bool TryParse(
    string arpTableEntryLine,
#if NULL_STATE_STATIC_ANALYSIS_ATTRIBUTES
    [NotNullWhen(true)]
#endif
    out IPAddress? ipAddress,
    out int hardwareType,
    out Flags flags,
    out PhysicalAddress? hardwareAddress,
#if NULL_STATE_STATIC_ANALYSIS_ATTRIBUTES
    [NotNullWhen(true)]
#endif
    out string? device
  )
  {
    ipAddress = default;
    hardwareType = default;
    flags = default;
    hardwareAddress = default;
    device = default;

    var columns =
#if SYSTEM_STRINGSPLITOPTIONS_TRIMENTRIES
      arpTableEntryLine.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
#elif SYSTEM_STRING_SPLIT_CHAR
      arpTableEntryLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
#else
      arpTableEntryLine.Split(arpTableEntryDelimiter, StringSplitOptions.RemoveEmptyEntries);
#endif

    if (columns.Length < 6)
      return false;

    // <IP address> field
    if (!IPAddress.TryParse(columns[0], out ipAddress))
      return false;

    // <HW type> field
    var columnHWType = columns[2]
#if SYSTEM_STRINGSPLITOPTIONS_TRIMENTRIES
      ;
#else
      .Trim();
#endif

    if (!columnHWType.StartsWith("0x", StringComparison.Ordinal))
      return false;
#if SYSTEM_INT32_TRYPARSE_READONLYSPAN_OF_CHAR
    if (!byte.TryParse(columnHWType.AsSpan(2), NumberStyles.HexNumber, provider: null, out var hardwareTypeInHexInt))
#else
    if (!byte.TryParse(columnHWType.Substring(2), NumberStyles.HexNumber, provider: null, out var hardwareTypeInHexInt))
#endif
      return false;

    hardwareType = hardwareTypeInHexInt;

    // <Flags> field
    var columnFlags = columns[2]
#if SYSTEM_STRINGSPLITOPTIONS_TRIMENTRIES
      ;
#else
      .Trim();
#endif

    if (!columnFlags.StartsWith("0x", StringComparison.Ordinal))
      return false;
#if SYSTEM_INT32_TRYPARSE_READONLYSPAN_OF_CHAR
    if (!byte.TryParse(columns[2].AsSpan(2), NumberStyles.HexNumber, provider: null, out var flagsInHexInt))
#else
    if (!byte.TryParse(columns[2].Substring(2), NumberStyles.HexNumber, provider: null, out var flagsInHexInt))
#endif
      return false;

    flags = (Flags)flagsInHexInt;

    // <HW address> field
#if SYSTEM_NET_NETWORKINFORMATION_PHYSICALADDRESS_TRYPARSE
    if (!PhysicalAddress.TryParse(columns[3], out hardwareAddress))
      return false;
#else

    try {
      hardwareAddress = PhysicalAddress.Parse(columns[3]);
    }
    catch (FormatException) {
      hardwareAddress = null;
      return false;
    }
#endif

    // <Device> field
    device = columns[5]
#if SYSTEM_STRINGSPLITOPTIONS_TRIMENTRIES
      ;
#else
      .Trim();
#endif

    return true;
  }
}
