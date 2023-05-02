// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Runtime.InteropServices;

namespace Smdn.Net;

/// <summary>
/// Provides <see cref="StringComparison"/> and <see cref="StringComparer"/> for comparing the network interface ID strings.
/// </summary>
/// <remarks>
/// On Windows, <see cref="System.Net.NetworkInformation.NetworkInterface.Id"/>
/// is set to a string representing the GUID of the network interface,
/// but its casing conventions is not specified explicitly, so perform the
/// case-insensitive comparison.
/// </remarks>
internal static class NetworkInterfaceIdComparer {
  /// <summary><see cref="StringComparer"/> for comparing the network interface ID strings.</summary>
  public static readonly StringComparer Comparer =
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
      ? StringComparer.OrdinalIgnoreCase
      : StringComparer.Ordinal;

  /// <summary><see cref="StringComparison"/> for comparing the network interface ID strings.</summary>
  public static readonly StringComparison Comparison =
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
      ? StringComparison.OrdinalIgnoreCase
      : StringComparison.Ordinal;
}
