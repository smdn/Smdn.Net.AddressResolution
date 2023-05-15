// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.NetworkScanning;

public sealed class PingNetworkScanner : NetworkScanner {
  public static bool IsSupported => true;

  private Ping ping;
  private readonly PingOptions pingOptions;

  public PingNetworkScanner(
    IPNetworkProfile networkProfile,
    IServiceProvider? serviceProvider = null
  )
    : base(
      networkProfile: networkProfile ?? throw new ArgumentNullException(nameof(networkProfile)),
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<PingNetworkScanner>()
    )
  {
    ping = new Ping();
    pingOptions = new() {
      // TODO: TTL
      // Ttl = 1,
    };
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing) {
      ping?.Dispose();
      ping = null!;
    }

    base.Dispose(disposing);
  }

  protected override async ValueTask ScanAsyncCore(
    IPAddress address,
    CancellationToken cancellationToken = default
  )
  {
    const int timeoutMilliseconds = 100;

    try {
      var reply = await ping!.SendPingAsync(
        address: address,
        timeout: timeoutMilliseconds,
        buffer: Array.Empty<byte>(),
        options: pingOptions
      ).ConfigureAwait(false);

      Logger?.LogDebug(
        "{Address}: {Status} ({RoundtripTime} ms)",
        address,
        reply.Status,
        reply.RoundtripTime
      );
    }
#pragma warning disable CA1031
    catch (Exception ex) {
      Logger?.LogWarning(
        exception: ex,
        "Ping failed: {Address}",
        address
      );
    }
#pragma warning restore CA1031
  }
}
