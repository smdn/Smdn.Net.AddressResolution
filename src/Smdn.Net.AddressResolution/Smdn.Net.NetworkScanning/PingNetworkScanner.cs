// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.NetworkScanning;

public sealed class PingNetworkScanner : INetworkScanner {
  private readonly ILogger? logger;
  private readonly Func<IEnumerable<IPAddress>?> getScanTargetAddresses;
  private Ping? ping;
  private readonly PingOptions pingOptions;

  public PingNetworkScanner(
    IPNetworkProfile networkProfile,
    IServiceProvider? serviceProvider = null
  )
  {
    getScanTargetAddresses = (networkProfile ?? throw new ArgumentNullException(nameof(networkProfile))).GetAddressRange;
    logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<PingNetworkScanner>();

    ping = new Ping();
    pingOptions = new() {
      // TODO: TTL
      // Ttl = 1,
    };
  }

  public void Dispose()
  {
    ping?.Dispose();
    ping = null;
  }

  private void ThrowIfDisposed()
  {
    if (ping is null)
      throw new ObjectDisposedException(GetType().FullName);
  }

  /*
   * INetworkScanner
   */
  public ValueTask ScanAsync(
    CancellationToken cancellationToken = default
  )
    => ScanAsync(
      addresses: getScanTargetAddresses() ?? throw new InvalidOperationException("could not get address range"),
      cancellationToken: cancellationToken
    );

  public ValueTask ScanAsync(
    IEnumerable<IPAddress> addresses,
    CancellationToken cancellationToken = default
  )
  {
    if (addresses is null)
      throw new ArgumentNullException(nameof(addresses));

    ThrowIfDisposed();

    return ScanAsyncCore();

    async ValueTask ScanAsyncCore()
    {
      const int timeoutMilliseconds = 100;

      foreach (var address in addresses) {
        cancellationToken.ThrowIfCancellationRequested();

        try {
          var reply = await ping!.SendPingAsync(
            address: address,
            timeout: timeoutMilliseconds,
            buffer: Array.Empty<byte>(),
            options: pingOptions
          ).ConfigureAwait(false);

          logger?.LogDebug(
            "{Address}: {Status} ({RoundtripTime} ms)",
            address,
            reply.Status,
            reply.RoundtripTime
          );
        }
        catch (Exception ex) {
          logger?.LogWarning(
            exception: ex,
            "Ping failed: {Address}",
            address
          );
        }
      }
    }
  }
}
