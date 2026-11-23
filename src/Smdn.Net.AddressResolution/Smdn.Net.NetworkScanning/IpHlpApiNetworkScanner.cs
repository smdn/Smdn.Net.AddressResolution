// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.NetworkScanning;

public sealed partial class IpHlpApiNetworkScanner : NetworkScanner {
  public static partial bool IsSupported { get; }

  public IpHlpApiNetworkScanner(
    IPNetworkProfile networkProfile,
    IServiceProvider? serviceProvider = null
  )
    : base(
      networkProfile: networkProfile ?? throw new ArgumentNullException(nameof(networkProfile)),
      logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<IpHlpApiNetworkScanner>()
    )
  {
  }
}
