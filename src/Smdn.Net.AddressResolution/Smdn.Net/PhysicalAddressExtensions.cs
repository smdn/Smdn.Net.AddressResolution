// SPDX-FileCopyrightText: 2022 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER || NET5_0_OR_GREATER
#define SYSTEM_STRING_JOIN_CHAR
#endif

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
    => delimiter == '\0'
      ? string.Concat(
          (hardwareAddress ?? throw new ArgumentNullException(nameof(hardwareAddress)))
            .GetAddressBytes()
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
          (hardwareAddress ?? throw new ArgumentNullException(nameof(hardwareAddress)))
            .GetAddressBytes()
            .Select(static b => b.ToString("X2", provider: null))
        );
#pragma warning restore SA1114
}
