// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressResolution.Arp;

#pragma warning disable IDE0040
partial class ProcfsArpMacAddressResolver {
#pragma warning restore IDE0040
  [Flags]
  private enum ArpTableEntryFlags {
    Incomplete = 0x00,
    Complete = 0x02,
    Permanent = 0x04,
    Publish = 0x08,
    UserTrailers = 0x10,
    NetMask = 0x20,
    DontPub = 0x40,
  }

  // [/proc/net/arp]
  // IP address       HW type     Flags       HW address            Mask     Device
  // 192.168.0.1      0x1         0x0         00:00:00:00:00:00     *        eth0
  // 192.168.0.2      0x1         0x0         00:00:00:00:00:00     *        eth0
  // :                :           :           :                     :        :
  private readonly struct ArpTableEntry : IEquatable<IPAddress?>, IEquatable<PhysicalAddress?> {
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
    [MemberNotNullWhen(false, nameof(IsEmpty))]
#endif
    public IPAddress? IPAddress { get; init; }

    public int HardwareType { get; init; }

#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLWHENATTRIBUTE
    [MemberNotNullWhen(false, nameof(IsEmpty))]
#endif
    public PhysicalAddress? HardwareAddress { get; init; }

    public ArpTableEntryFlags Flags { get; init; }
    public string? Device { get; init; }

    public bool IsEmpty => IPAddress is null && HardwareAddress is null;
    public bool IsPermanent => Flags.HasFlag(ArpTableEntryFlags.Permanent);
    public bool IsPermanentOrComplete => Flags.HasFlag(ArpTableEntryFlags.Permanent) || Flags.HasFlag(ArpTableEntryFlags.Complete);

    public bool Equals(IPAddress? other)
    {
      if (IPAddress is null && other is null)
        return true;

      return IPAddress is not null && IPAddress.Equals(other);
    }

    public bool Equals(PhysicalAddress? other)
    {
      if (HardwareAddress is null && other is null)
        return true;

      return HardwareAddress is not null && HardwareAddress.Equals(other);
    }

    internal static async IAsyncEnumerable<ArpTableEntry> EnumerateArpTableEntriesAsync(
      Func<ArpTableEntry, bool> predicate,
      ILogger? logger,
      [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
      using var reader = new StreamReader(PathToProcNetArp);

      for (
        var line = await reader.ReadLineAsync().ConfigureAwait(false);
        line is not null;
        line = await reader.ReadLineAsync().ConfigureAwait(false)
      ) {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryParse(line, out var entry))
          continue;

        // ignore entry with an empty hardware address
        if (AllZeroMacAddress.Equals(entry.HardwareAddress))
          continue;

        if (predicate(entry)) {
          logger?.LogDebug(
            "[/proc/net/arp] IP={IPAddress} HW={HardwareAddress} Type=0x{HardwareType} Device={Device} Flags=0x{Flags}",
            entry.IPAddress,
            entry.HardwareAddress?.ToMacAddressString(),
            entry.HardwareType.ToString("X2", provider: null),
            entry.Device,
            ((byte)entry.Flags).ToString("X2", provider: null)
          );
          yield return entry;
        }
      }
    }

#if !SYSTEM_STRING_SPLIT_CHAR
    private static readonly char[] arpTableEntryDelimiter = new[] { ' ' };
#endif

    private static bool TryParse(
      string arpTableEntryLine,
      out ArpTableEntry entry
    )
    {
      entry = default;

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
      if (!System.Net.IPAddress.TryParse(columns[0], out var address))
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
      if (!byte.TryParse(columnHWType.AsSpan(2), NumberStyles.HexNumber, provider: null, out var hardwareTypeInByte))
#else
      if (!byte.TryParse(columnHWType.Substring(2), NumberStyles.HexNumber, provider: null, out var hardwareTypeInByte))
#endif
        return false;

      var hardwareType = (int)hardwareTypeInByte;

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
      if (!byte.TryParse(columns[2].AsSpan(2), NumberStyles.HexNumber, provider: null, out var flags))
#else
      if (!byte.TryParse(columns[2].Substring(2), NumberStyles.HexNumber, provider: null, out var flags))
#endif
        return false;

      var arpFlags = (ArpTableEntryFlags)flags;

      // <HW address> field
#if SYSTEM_NET_NETWORKINFORMATION_PHYSICALADDRESS_TRYPARSE
      if (!PhysicalAddress.TryParse(columns[3], out var hardwareAddress))
        return false;
#else
      PhysicalAddress? hardwareAddress = null;

      try {
        hardwareAddress = PhysicalAddress.Parse(columns[3]);
      }
      catch (FormatException) {
        return false;
      }
#endif

      // <Device> field
      var device = columns[5]
#if SYSTEM_STRINGSPLITOPTIONS_TRIMENTRIES
        ;
#else
        .Trim();
#endif

      entry = new() {
        IPAddress = address,
        HardwareType = hardwareType,
        HardwareAddress = hardwareAddress,
        Flags = arpFlags,
        Device = device,
      };

      return true;
    }
  }
}
