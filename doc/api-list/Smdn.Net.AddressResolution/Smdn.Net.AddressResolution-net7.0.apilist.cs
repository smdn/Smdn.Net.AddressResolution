// Smdn.Net.AddressResolution.dll (Smdn.Net.AddressResolution-1.0.0-preview5)
//   Name: Smdn.Net.AddressResolution
//   AssemblyVersion: 1.0.0.0
//   InformationalVersion: 1.0.0-preview5+fe261fa7dd16c43347c28935b3055ec3b8ab0674
//   TargetFramework: .NETCoreApp,Version=v7.0
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Memory, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#nullable enable annotations

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smdn.Net.AddressResolution;

namespace Smdn.Net {
  public static class PhysicalAddressExtensions {
    public static string ToMacAddressString(this PhysicalAddress hardwareAddress, char delimiter = ':') {}
  }
}

namespace Smdn.Net.AddressResolution {
  public interface IAddressResolver<TAddress, TResolvedAddress> {
    void Invalidate(TAddress address);
    ValueTask<TResolvedAddress> ResolveAsync(TAddress address, CancellationToken cancellationToken);
  }

  public abstract class MacAddressResolver :
    IAddressResolver<IPAddress, PhysicalAddress>,
    IAddressResolver<PhysicalAddress, IPAddress>,
    IDisposable
  {
    protected static readonly PhysicalAddress AllZeroMacAddress; // = "000000000000"

    public static MacAddressResolver Null { get; }

    public static MacAddressResolver Create(MacAddressResolverOptions? options = null, IServiceProvider? serviceProvider = null) {}

    protected MacAddressResolver(ILogger? logger = null) {}

    public abstract bool HasInvalidated { get; }
    protected ILogger? Logger { get; }

    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public void Invalidate(IPAddress ipAddress) {}
    public void Invalidate(PhysicalAddress macAddress) {}
    protected abstract void InvalidateCore(IPAddress ipAddress);
    protected abstract void InvalidateCore(PhysicalAddress macAddress);
    public ValueTask RefreshCacheAsync(CancellationToken cancellationToken = default) {}
    protected virtual ValueTask RefreshCacheAsyncCore(CancellationToken cancellationToken) {}
    public ValueTask RefreshInvalidatedCacheAsync(CancellationToken cancellationToken = default) {}
    protected virtual ValueTask RefreshInvalidatedCacheAsyncCore(CancellationToken cancellationToken) {}
    public ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsync(IPAddress ipAddress, CancellationToken cancellationToken = default) {}
    protected abstract ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(IPAddress ipAddress, CancellationToken cancellationToken);
    public ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsync(PhysicalAddress macAddress, CancellationToken cancellationToken = default) {}
    protected abstract ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(PhysicalAddress macAddress, CancellationToken cancellationToken);
    void IAddressResolver<IPAddress, PhysicalAddress>.Invalidate(IPAddress address) {}
    ValueTask<PhysicalAddress?> IAddressResolver<IPAddress, PhysicalAddress>.ResolveAsync(IPAddress address, CancellationToken cancellationToken) {}
    void IAddressResolver<PhysicalAddress, IPAddress>.Invalidate(PhysicalAddress address) {}
    ValueTask<IPAddress?> IAddressResolver<PhysicalAddress, IPAddress>.ResolveAsync(PhysicalAddress address, CancellationToken cancellationToken) {}
    protected void ThrowIfDisposed() {}
  }

  public sealed class MacAddressResolverOptions {
    public static readonly MacAddressResolverOptions Default; // = "Smdn.Net.AddressResolution.MacAddressResolverOptions"

    public MacAddressResolverOptions() {}

    public string? ArpScanCommandInterfaceSpecification { get; init; }
    public string? ArpScanCommandTargetSpecification { get; init; }
    public string? NmapCommandInterfaceSpecification { get; init; }
    public string? NmapCommandTargetSpecification { get; init; }
    public TimeSpan ProcfsArpFullScanInterval { get; init; }
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.2.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.2.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
