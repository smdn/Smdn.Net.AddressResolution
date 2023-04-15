// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace Smdn.Net;

public static class PhysicalAddressExtensions {
  internal static readonly PhysicalAddress AllZeroMacAddress = new(new byte[6]); // 00:00:00:00:00:00

  public static string ToMacAddressString(
    this PhysicalAddress hardwareAddress,
    char delimiter = ':'
  )
  {
    if (hardwareAddress is null)
      throw new ArgumentNullException(nameof(hardwareAddress));

    var addressBytes = hardwareAddress.GetAddressBytes();

    return ToMacAddressString(addressBytes, addressBytes.Length, delimiter);
  }

  internal static string ToMacAddressString(
    byte[] addressBytes,
    int lengthOfAddressBytes,
    char delimiter = ':'
  )
    => delimiter == '\0'
      ? string.Concat(
          addressBytes
            .Take(lengthOfAddressBytes)
            .Select(static b => b.ToString("X2", provider: null))
        )
      :
#pragma warning disable SA1114
        string.Join(
#if SYSTEM_STRING_JOIN_CHAR
          delimiter,
#else
          delimiter.ToString(),
#endif
          addressBytes
            .Take(lengthOfAddressBytes)
            .Select(static b => b.ToString("X2", provider: null))
        );
#pragma warning restore SA1114
}
