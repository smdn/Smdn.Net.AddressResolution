// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.AddressTables;

public sealed partial class IpHlpApiAddressTable : AddressTable {
  public static partial bool IsSupported { get; }

  public IpHlpApiAddressTable(IServiceProvider? serviceProvider = null)
    : base(logger: serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<IpHlpApiAddressTable>())
  {
  }
}
