// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;

using NUnit.Framework;

namespace Smdn.Net.NeighborDiscovery;

[TestFixture]
public class NeighborTableEntryTests {
  private static readonly IPAddress TestIPAddress = IPAddress.Parse("192.0.2.255");
  private static readonly PhysicalAddress TestMacAddress = PhysicalAddress.Parse("00:00:5E:00:53:00");

  [Test]
  public void Ctor()
  {
    Assert.DoesNotThrow(
      () => new NeighborTableEntry(
        ipAddress: TestIPAddress,
        physicalAddress: null,
        isPermanent: true,
        state: NeighborTableEntryState.None,
        interfaceId: null
      )
    );

    Assert.Throws<ArgumentNullException>(
      () => new NeighborTableEntry(
        ipAddress: null!,
        physicalAddress: null,
        isPermanent: true,
        state: NeighborTableEntryState.None,
        interfaceId: null
      )
    );
  }

  [Test]
  public void Equals_IPAddress()
  {
    var entry = new NeighborTableEntry(
      ipAddress: TestIPAddress,
      physicalAddress: null,
      isPermanent: true,
      state: NeighborTableEntryState.None,
      interfaceId: null
    );

    Assert.IsFalse(entry.Equals((IPAddress?)null), "#1");
    Assert.IsFalse(entry.Equals(IPAddress.Any), "#2");
    Assert.IsTrue(entry.Equals(entry.IPAddress), "#3");
    Assert.IsTrue(entry.Equals(IPAddress.Parse(entry.IPAddress.ToString())), "#4");
  }

  [Test]
  public void Equals_PhysicalAddressNull()
  {
    var entry = new NeighborTableEntry(
      ipAddress: TestIPAddress,
      physicalAddress: null,
      isPermanent: true,
      state: NeighborTableEntryState.None,
      interfaceId: null
    );

    Assert.IsTrue(entry.Equals((PhysicalAddress?)null), "#1");
    Assert.IsFalse(entry.Equals(PhysicalAddress.None), "#2");
    Assert.IsFalse(entry.Equals(TestMacAddress), "#3");
  }

  [Test]
  public void Equals_PhysicalAddressNotNull()
  {
    var entry = new NeighborTableEntry(
      ipAddress: TestIPAddress,
      physicalAddress: TestMacAddress,
      isPermanent: true,
      state: NeighborTableEntryState.None,
      interfaceId: null
    );

    Assert.IsFalse(entry.Equals((PhysicalAddress?)null), "#1");
    Assert.IsFalse(entry.Equals(PhysicalAddress.None), "#2");
    Assert.IsTrue(entry.Equals(entry.PhysicalAddress), "#3");
    Assert.IsTrue(entry.Equals(PhysicalAddress.Parse(entry.PhysicalAddress!.ToMacAddressString())), "#4");
  }

  private static System.Collections.IEnumerable YieldTestCases_ToString()
  {
    yield return new object?[] {
      new NeighborTableEntry(
        ipAddress: TestIPAddress,
        physicalAddress: null,
        isPermanent: true,
        state: NeighborTableEntryState.None,
        interfaceId: null
      )
    };
    yield return new object?[] {
      new NeighborTableEntry(
        ipAddress: TestIPAddress,
        physicalAddress: TestMacAddress,
        isPermanent: true,
        state: NeighborTableEntryState.None,
        interfaceId: null
      )
    };
    yield return new object?[] {
      new NeighborTableEntry(
        ipAddress: TestIPAddress,
        physicalAddress: TestMacAddress,
        isPermanent: true,
        state: NeighborTableEntryState.None,
        interfaceId: "eth0"
      )
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ToString))]
  public void ToString(NeighborTableEntry entry)
  {
    Assert.DoesNotThrow(() => entry.ToString());

    var str = entry.ToString();

    Assert.IsNotNull(str);
    Assert.IsNotEmpty(str);
  }
}
