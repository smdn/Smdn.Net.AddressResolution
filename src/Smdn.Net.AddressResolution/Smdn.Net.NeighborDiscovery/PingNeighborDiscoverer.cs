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

namespace Smdn.Net.NeighborDiscovery;

public sealed class PingNeighborDiscoverer : INeighborDiscoverer {
  private readonly ILogger? logger;
  private readonly Func<IEnumerable<IPAddress>?> getDiscoveryTargetAddresses;
  private Ping? ping;
  private readonly PingOptions pingOptions;

  public PingNeighborDiscoverer(
    IPNetworkProfile networkProfile,
    IServiceProvider? serviceProvider = null
  )
  {
    getDiscoveryTargetAddresses = (networkProfile ?? throw new ArgumentNullException(nameof(networkProfile))).GetAddressRange;
    logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<PingNeighborDiscoverer>();

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
   * INeighborDiscoverer
   */
  public ValueTask DiscoverAsync(
    CancellationToken cancellationToken = default
  )
    => DiscoverAsync(
      addresses: getDiscoveryTargetAddresses() ?? throw new InvalidOperationException("could not get address range"),
      cancellationToken: cancellationToken
    );

  public ValueTask DiscoverAsync(
    IEnumerable<IPAddress> addresses,
    CancellationToken cancellationToken = default
  )
  {
    if (addresses is null)
      throw new ArgumentNullException(nameof(addresses));

    ThrowIfDisposed();

    return DiscoverAsyncCore();

    async ValueTask DiscoverAsyncCore()
    {
      const int timeoutMilliseconds = 100;

      foreach (var address in addresses) {
        cancellationToken.ThrowIfCancellationRequested();

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
    }
  }
}
