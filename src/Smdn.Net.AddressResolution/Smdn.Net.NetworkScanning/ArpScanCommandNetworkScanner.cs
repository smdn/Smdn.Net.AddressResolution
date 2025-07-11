// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
// cSpell:ignore SGID,SUID,localnet,hostnames
using System;
using System.Collections.Generic;
#if SYSTEM_IO_UNIXFILEMODE
using System.IO;
#endif
using System.Net;
#if SYSTEM_IO_UNIXFILEMODE || SYSTEM_IO_FILE_GETUNIXFILEMODE
using System.Runtime.InteropServices;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.NetworkScanning;

public sealed class ArpScanCommandNetworkScanner : CommandNetworkScanner {
  // ref: https://manpages.ubuntu.com/manpages/jammy/man1/arp-scan.1.html
  //   --numeric: IP addresses only, no hostnames.
  //   --quiet: Only display minimal output. No protocol decoding.
  private const string ArpScanCommandBaseOptions = "--numeric --quiet ";

  public static bool IsSupported =>
    LazyArpScanCommand.Value.IsAvailable &&
#pragma warning disable IDE0047, SA1003, SA1119
    (
#if SYSTEM_ENVIRONMENT_ISPRIVILEGEDPROCESS
      Environment.IsPrivilegedProcess ||
#endif
#if SYSTEM_IO_FILE_GETUNIXFILEMODE
      (
        !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
        HasSgidOrSuid(File.GetUnixFileMode(LazyArpScanCommand.Value.GetExecutablePathOrThrow()))
      )
#else
      false // TODO: use Mono.Posix
#endif
    );
#pragma warning restore IDE0047, SA1003, SA1119

#if SYSTEM_IO_UNIXFILEMODE
  private static bool HasSgidOrSuid(UnixFileMode fileMode)
    => fileMode.HasFlag(UnixFileMode.SetGroup) || fileMode.HasFlag(UnixFileMode.SetUser);
#endif

  private static readonly Lazy<Command> LazyArpScanCommand = new(
    valueFactory: static () => FindCommand(
      command: "arp-scan",
      paths: DefaultCommandPaths
    ),
    isThreadSafe: true
  );

  /*
   * instance members
   */
  private readonly string arpScanCommandCommonOptions;
  private readonly string arpScanCommandFullScanOptions;

  public ArpScanCommandNetworkScanner(
    IPNetworkProfile? networkProfile,
    IServiceProvider? serviceProvider = null
  )
    : base(
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<ArpScanCommandNetworkScanner>(),
      serviceProvider: serviceProvider
    )
  {
    arpScanCommandCommonOptions = ArpScanCommandBaseOptions;

    if (networkProfile?.NetworkInterface is not null)
      arpScanCommandCommonOptions += $"--interface={networkProfile.NetworkInterface.Id} ";

    var addressRange = networkProfile?.GetAddressRange();
    var arpScanCommandTargetSpecification = addressRange is null
      ? null
      : string.Join(" ", addressRange);

    arpScanCommandFullScanOptions = string.Concat(
      arpScanCommandCommonOptions,
      // ref: https://manpages.ubuntu.com/manpages/jammy/man1/arp-scan.1.html
      //   --localnet: Generate addresses from network interface configuration.
      arpScanCommandTargetSpecification ?? "--localnet "
    );
  }

  protected override bool GetCommandLineArguments(
    out string executable,
    out string arguments
  )
  {
    executable = LazyArpScanCommand.Value.GetExecutablePathOrThrow();

    // perform full scan
    arguments = arpScanCommandFullScanOptions;

    return true;
  }

  protected override bool GetCommandLineArguments(
    IEnumerable<IPAddress> addressesToScan,
    out string executable,
    out string arguments
  )
  {
    executable = LazyArpScanCommand.Value.GetExecutablePathOrThrow();

    var arpScanCommandTargetSpecification = string.Join(" ", addressesToScan);

    if (arpScanCommandTargetSpecification.Length == 0) {
      arguments = string.Empty;
      return false; // do nothing
    }

    // perform scan for specific target IPs
    arguments = arpScanCommandCommonOptions + arpScanCommandTargetSpecification;

    return true;
  }
}
