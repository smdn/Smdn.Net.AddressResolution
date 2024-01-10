// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Smdn.Net.AddressTables;
using Smdn.Net.NetworkScanning;

namespace Smdn.Net.AddressResolution;

[TestFixture]
public partial class MacAddressResolverTests {
  private sealed class PerformDelayNetworkScanner : INetworkScanner {
    private readonly TimeSpan delayOnNetworkScan;
    private readonly object lockObject = new();
    private volatile int numberOfTasksPerformed;
    private volatile int numberOfTasksInProgress;
    private volatile int maxNumberOfTasksPerformedInParallel;

    public int NumberOfTasksPerformed => numberOfTasksPerformed;
    public int MaxNumberOfTasksPerformedInParallel => maxNumberOfTasksPerformedInParallel;

    public PerformDelayNetworkScanner(TimeSpan delayOnNetworkScan)
    {
      this.delayOnNetworkScan = delayOnNetworkScan;
    }

    public void Dispose()
    {
    }

    public async ValueTask ScanAsync(CancellationToken cancellationToken)
    {
      Interlocked.Increment(ref numberOfTasksPerformed);
      Interlocked.Increment(ref numberOfTasksInProgress);

      lock (lockObject) {
        if (maxNumberOfTasksPerformedInParallel < numberOfTasksInProgress)
          Interlocked.Exchange(ref maxNumberOfTasksPerformedInParallel, numberOfTasksInProgress);
      }
      await Task.Delay(delayOnNetworkScan, cancellationToken);

      Interlocked.Decrement(ref numberOfTasksInProgress);
    }

    public async ValueTask ScanAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken)
    {
      Interlocked.Increment(ref numberOfTasksPerformed);
      Interlocked.Increment(ref numberOfTasksInProgress);

      lock (lockObject) {
        if (maxNumberOfTasksPerformedInParallel < numberOfTasksInProgress)
          Interlocked.Exchange(ref maxNumberOfTasksPerformedInParallel, numberOfTasksInProgress);
      }

      await Task.Delay(delayOnNetworkScan, cancellationToken);

      Interlocked.Decrement(ref numberOfTasksInProgress);
    }
  }

  private static readonly IPAddress TestIPAddress = IPAddress.Parse("192.0.2.255");
  private static readonly PhysicalAddress TestMacAddress = PhysicalAddress.Parse("00:00:5E:00:53:00");

  [Test]
  public void Ctor_AddressTable_ArgumentNull()
  {
    Assert.Throws<ArgumentNullException>(
      () => new MacAddressResolver(
        addressTable: null,
        networkScanner: new PseudoNetworkScanner(),
        serviceProvider: null
      ),
      "#1"
    );

    Assert.Throws<InvalidOperationException>(
      () => new MacAddressResolver(
        addressTable: null,
        networkScanner: new PseudoNetworkScanner(),
        serviceProvider: new ServiceCollection().BuildServiceProvider()
      ),
      "#2"
    );
  }

  [Test]
  public void Ctor_NetworkScanner_ArgumentNull()
  {
    Assert.DoesNotThrow(
      () => {
        using var resolver = new MacAddressResolver(
          addressTable: new PseudoAddressTable(),
          networkScanner: null,
          serviceProvider: null
        );

        Assert.That(resolver.CanPerformNetworkScan, Is.False, nameof(resolver.CanPerformNetworkScan));
      },
      "#1"
    );

    Assert.DoesNotThrow(
      () => {
        using var resolver = new MacAddressResolver(
          addressTable: new PseudoAddressTable(),
          networkScanner: null,
          serviceProvider: new ServiceCollection().BuildServiceProvider()
        );

        Assert.That(resolver.CanPerformNetworkScan, Is.False, nameof(resolver.CanPerformNetworkScan));
      },
      "#2"
    );
  }

  [TestCase(0)]
  [TestCase(-1)]
  [TestCase(int.MinValue)]
  public void Ctor_MaxParallelCountForRefreshInvalidatedAddresses_ArgumentOutOfRange(int maxParallelCount)
    => Assert.Throws<ArgumentOutOfRangeException>(
      () => new MacAddressResolver(
        addressTable: new PseudoAddressTable(),
        networkScanner: new PseudoNetworkScanner(),
        maxParallelCountForRefreshInvalidatedAddresses: maxParallelCount
      )
    );

  private static System.Collections.IEnumerable YieldTestCases_CanPerformNetworkScan()
  {
    yield return new object[] {
      new MacAddressResolver(
        addressTable: new PseudoAddressTable(),
        networkScanner: null,
        serviceProvider: null
      ),
      false,
      "no INetworkScanner provided by networkScanner parameter"
    };

    yield return new object[] {
      new MacAddressResolver(
        addressTable: new PseudoAddressTable(),
        networkScanner: null,
        serviceProvider: new ServiceCollection().BuildServiceProvider()
      ),
      false,
      "no INetworkScanner provided by IServiceProvider"
    };

    yield return new object[] {
      new MacAddressResolver(
        addressTable: new PseudoAddressTable(),
        networkScanner: new PseudoNetworkScanner(),
        serviceProvider: null
      ),
      true,
      "INetworkScanner provided by ctor parameter"
    };

    var services = new ServiceCollection();

    services.AddSingleton<INetworkScanner>(new PseudoNetworkScanner());

    yield return new object[] {
      new MacAddressResolver(
        addressTable: new PseudoAddressTable(),
        networkScanner: null,
        serviceProvider: services.BuildServiceProvider()
      ),
      true,
      "INetworkScanner provided by IServiceProvider"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_CanPerformNetworkScan))]
  public void CanPerformNetworkScan(MacAddressResolver resolver, bool expected, string message)
  {
    Assert.That(resolver.CanPerformNetworkScan, Is.EqualTo(expected), message);

    resolver.Dispose();
  }

  private static System.Collections.IEnumerable YieldTestCases_NetworkScanInterval()
  {
    yield return new object?[] { TimeSpan.Zero, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { TimeSpan.MinValue, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { TimeSpan.FromTicks(-1), typeof(ArgumentOutOfRangeException) };

    yield return new object?[] { TimeSpan.FromMilliseconds(1), null };
    yield return new object?[] { TimeSpan.FromTicks(1), null };
    yield return new object?[] { TimeSpan.MaxValue, null };
    yield return new object?[] { TimeSpan.FromMilliseconds(-1), null }; // == Timeout.InfiniteTimeSpan
    yield return new object?[] { Timeout.InfiniteTimeSpan, null };
  }

  [TestCaseSource(nameof(YieldTestCases_NetworkScanInterval))]
  public void NetworkScanInterval(
    TimeSpan networkScanInterval,
    Type? typeOfExpectedException
  )
  {
    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: new PseudoNetworkScanner()
    );

    if (typeOfExpectedException is null) {
      Assert.DoesNotThrow(() => resolver.NetworkScanInterval = networkScanInterval);
      Assert.That(resolver.NetworkScanInterval, Is.EqualTo(networkScanInterval), nameof(resolver.NetworkScanInterval));
    }
    else {
      var initialNetworkScanInterval = resolver.NetworkScanInterval;

      Assert.Throws(typeOfExpectedException, () => resolver.NetworkScanInterval = networkScanInterval);
      Assert.That(resolver.NetworkScanInterval, Is.EqualTo(initialNetworkScanInterval), nameof(resolver.NetworkScanInterval));
    }
  }

  private static System.Collections.IEnumerable YieldTestCases_NetworkScanMinInterval()
  {
    yield return new object?[] { TimeSpan.MinValue, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { TimeSpan.FromTicks(-1), typeof(ArgumentOutOfRangeException) };

    yield return new object?[] { TimeSpan.Zero, null };
    yield return new object?[] { TimeSpan.FromMilliseconds(1), null };
    yield return new object?[] { TimeSpan.FromTicks(1), null };
    yield return new object?[] { TimeSpan.MaxValue, null };
    yield return new object?[] { TimeSpan.FromMilliseconds(-1), null }; // == Timeout.InfiniteTimeSpan
    yield return new object?[] { Timeout.InfiniteTimeSpan, null };
  }

  [TestCaseSource(nameof(YieldTestCases_NetworkScanMinInterval))]
  public void NetworkScanMinInterval(
    TimeSpan networkScanMinInterval,
    Type? typeOfExpectedException
  )
  {
    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: new PseudoNetworkScanner()
    );

    if (typeOfExpectedException is null) {
      Assert.DoesNotThrow(() => resolver.NetworkScanMinInterval = networkScanMinInterval);
      Assert.That(resolver.NetworkScanMinInterval, Is.EqualTo(networkScanMinInterval), nameof(resolver.NetworkScanMinInterval));
    }
    else {
      var initialNetworkScanMinInterval = resolver.NetworkScanMinInterval;

      Assert.Throws(typeOfExpectedException, () => resolver.NetworkScanMinInterval = networkScanMinInterval);
      Assert.That(resolver.NetworkScanMinInterval, Is.EqualTo(initialNetworkScanMinInterval), nameof(resolver.NetworkScanMinInterval));
    }
  }

  [Test]
  public void HasInvalidated_IPAddressInvalidated()
  {
    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: new PseudoNetworkScanner()
    );

    Assert.That(resolver.HasInvalidated, Is.False, "initial state");

    resolver.Invalidate(TestIPAddress);

    Assert.That(resolver.HasInvalidated, Is.True, "after invalidate IP address");

    resolver.Invalidate(TestMacAddress);

    Assert.That(resolver.HasInvalidated, Is.True, "after invalidate MAC address");
  }

  [Test]
  public void HasInvalidated_MacAddressInvalidated()
  {
    var resolver = new MacAddressResolver(
      addressTable: new PseudoAddressTable(),
      networkScanner: new PseudoNetworkScanner()
    );

    Assert.That(resolver.HasInvalidated, Is.False, "initial state");

    resolver.Invalidate(TestMacAddress);

    Assert.That(resolver.HasInvalidated, Is.True, "after invalidate MAC address");

    resolver.Invalidate(TestIPAddress);

    Assert.That(resolver.HasInvalidated, Is.True, "after invalidate IP address");
  }

  [Test]
  public void Dispose_AddressTableAndNetworkScannerAreSuppliedViaServiceProvider()
  {
    var addressTable = new PseudoAddressTable();
    var networkScanner = new PseudoNetworkScanner();
    var services = new ServiceCollection();

    services.AddSingleton<IAddressTable>(addressTable);
    services.AddSingleton<INetworkScanner>(networkScanner);

    var resolver = new MacAddressResolver(
      networkProfile: null,
      serviceProvider: services.BuildServiceProvider()
    );

    resolver.Dispose();

    Assert.That(addressTable.IsDisposed, Is.False, $"{nameof(IAddressTable)} should not be disposed by default.");
    Assert.That(networkScanner.IsDisposed, Is.False, $"{nameof(INetworkScanner)} should not be disposed by default.");
  }

  [Test]
  public void Dispose_AddressTableAndNetworkScannerAreSuppliedAsCtorParameter()
  {
    var addressTable = new PseudoAddressTable();
    var networkScanner = new PseudoNetworkScanner();
    var resolver = new MacAddressResolver(
      addressTable: addressTable,
      networkScanner: networkScanner
    );

    resolver.Dispose();

    Assert.That(addressTable.IsDisposed, Is.False, $"{nameof(IAddressTable)} should not be disposed by default.");
    Assert.That(networkScanner.IsDisposed, Is.False, $"{nameof(INetworkScanner)} should not be disposed by default.");
  }

  [TestCase(true, true)]
  [TestCase(false, true)]
  [TestCase(true, false)]
  [TestCase(false, false)]
  public void Dispose_ShouldDisposeAddressTableAndNetworkScanner(
    bool shouldDisposeAddressTable,
    bool shouldDisposeNetworkScanner
  )
  {
    var addressTable = new PseudoAddressTable();
    var networkScanner = new PseudoNetworkScanner();
    var resolver = new MacAddressResolver(
      addressTable: addressTable,
      shouldDisposeAddressTable: shouldDisposeAddressTable,
      networkScanner: networkScanner,
      shouldDisposeNetworkScanner: shouldDisposeNetworkScanner
    );

    resolver.Dispose();

    Assert.That(shouldDisposeAddressTable, Is.EqualTo(addressTable.IsDisposed));
    Assert.That(shouldDisposeNetworkScanner, Is.EqualTo(networkScanner.IsDisposed));

    Assert.DoesNotThrow(resolver.Dispose, "dispose again");

    Assert.That(shouldDisposeAddressTable, Is.EqualTo(addressTable.IsDisposed));
    Assert.That(shouldDisposeNetworkScanner, Is.EqualTo(networkScanner.IsDisposed));
  }
}
