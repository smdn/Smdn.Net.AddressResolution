// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

using NUnit.Framework;

namespace Smdn.Net.AddressTables;

[TestFixture]
public class AddressTableEntryTests {
  private static readonly IPAddress TestIPAddress = IPAddress.Parse("192.0.2.255");
  private static readonly PhysicalAddress TestMacAddress = PhysicalAddress.Parse("00:00:5E:00:53:00");

  [Test]
  public void Default()
  {
    var entry = default(AddressTableEntry);

    Assert.IsTrue(entry.IsEmpty, nameof(entry.IsEmpty));
    Assert.IsNull(entry.IPAddress, nameof(entry.IPAddress));
    Assert.IsNull(entry.PhysicalAddress, nameof(entry.PhysicalAddress));
    Assert.IsFalse(entry.IsPermanent, nameof(entry.IsPermanent));
    Assert.AreEqual(default(AddressTableEntryState), entry.State, nameof(entry.State));
    Assert.IsNull(entry.InterfaceId, nameof(entry.InterfaceId));

    Assert.IsTrue(entry.Equals(AddressTableEntry.Empty), "equals to AddressTableEntry.Empty");
    Assert.IsTrue(entry.Equals((IPAddress?)null), "equals to (IPAddress)null");
    Assert.IsTrue(entry.Equals((PhysicalAddress?)null), "equals to (PhysicalAddress)null");

    Assert.DoesNotThrow(() => entry.ToString());
    Assert.DoesNotThrow(() => entry.GetHashCode());
  }

  [Test]
  public void IsEmpty()
  {
    Assert.IsTrue(default(AddressTableEntry).IsEmpty);
    Assert.IsTrue(AddressTableEntry.Empty.IsEmpty);

    Assert.IsFalse(
      new AddressTableEntry(
        ipAddress: TestIPAddress,
        physicalAddress: null,
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: null
      ).IsEmpty
    );
  }

  [Test]
  public void Ctor()
  {
    Assert.DoesNotThrow(
      () => new AddressTableEntry(
        ipAddress: TestIPAddress,
        physicalAddress: null,
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: null
      )
    );

    Assert.Throws<ArgumentNullException>(
      () => new AddressTableEntry(
        ipAddress: null!,
        physicalAddress: null,
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: null
      )
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_Equals()
    => YieldTestCases_Equals_Common(exceptState: false);

  private static System.Collections.IEnumerable YieldTestCases_Equals_ExceptState()
    => YieldTestCases_Equals_Common(exceptState: true);

  private static System.Collections.IEnumerable YieldTestCases_Equals_Common(bool exceptState)
  {
    static AddressTableEntry Create(
      string ipAddress,
      string? macAddress,
      bool isPermanent,
      string? interfaceId,
      AddressTableEntryState state = AddressTableEntryState.None
    )
      => new(
        ipAddress: IPAddress.Parse(ipAddress),
        physicalAddress: macAddress is null ? null : PhysicalAddress.Parse(macAddress),
        isPermanent: isPermanent,
        state: state,
        interfaceId: interfaceId
      );

    const bool areEqual = true;
    const bool areNotEqual = false;

    yield return new object[] {
      areEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0", AddressTableEntryState.None),
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0", AddressTableEntryState.None),
      "are equal"
    };

    yield return new object[] {
      areNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0", AddressTableEntryState.None),
      AddressTableEntry.Empty,
      "are not equal to Empty"
    };

    yield return new object[] {
      areEqual,
      AddressTableEntry.Empty,
      AddressTableEntry.Empty,
      "are equal (both empty)"
    };

    yield return new object[] {
      areEqual,
      Create("192.0.2.0", null, true, "wlan0"),
      Create("192.0.2.0", null, true, "wlan0"),
      "are equal (both of PhysicalAddress are null)"
    };

    yield return new object[] {
      areEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", false, "wlan0", AddressTableEntryState.None),
      Create("192.0.2.0", "00-00-5E-00-53-00", false, "wlan0", AddressTableEntryState.None),
      "are equal (both of IsPermanent are false)"
    };

    yield return new object[] {
      areEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, null, AddressTableEntryState.None),
      Create("192.0.2.0", "00-00-5E-00-53-00", true, null, AddressTableEntryState.None),
      "are equal (both of InterfaceId are null)"
    };

    yield return new object[] {
      RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? areEqual : areNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "WLAN0"),
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      "difference of the casing conventions for the network interface " +
        (
          RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "must not be ignored"
            : "must be ignored"
        )
    };

    yield return new object[] {
      areNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.1", "00-00-5E-00-53-00", true, "wlan0"),
      "difference in IPAddress"
    };

    yield return new object[] {
      areNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.0", "00-00-5E-00-53-01", true, "wlan0"),
      "difference in PhysicalAddress"
    };

    yield return new object[] {
      areNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.0", null, true, "wlan0"),
      "difference in PhysicalAddress (null)"
    };

    yield return new object[] {
      areNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.0", "00-00-5E-00-53-00", false, "wlan0"),
      "difference in IsPermanent"
    };

    yield return new object[] {
      exceptState ? areEqual : areNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0", AddressTableEntryState.None),
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0", AddressTableEntryState.Reachable),
      "difference in State"
    };

    yield return new object[] {
      areNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan1"),
      "difference in InterfaceId"
    };

    yield return new object[] {
      areNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.0", "00-00-5E-00-53-00", true, null),
      "difference in InterfaceId (null)"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Equals))]
  public void Equals(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    Assert.AreEqual(areEqual, x.Equals(y), $"{message}: {x} {(areEqual ? "==" : "!=")} {y}");
    Assert.AreEqual(areEqual, y.Equals(x), $"{message}: {y} {(areEqual ? "==" : "!=")} {x}");
  }

  [TestCaseSource(nameof(YieldTestCases_Equals))]
  public void GetHashCode(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    var hashCodeX = x.GetHashCode();
    var hashCodeY = y.GetHashCode();

    if (areEqual)
      Assert.AreEqual(hashCodeX, hashCodeY, $"{message}: HashCode {x} == {y}");
    else
      Assert.AreNotEqual(hashCodeX, hashCodeY, $"{message}: HashCode {x} != {y}");
  }

  [TestCaseSource(nameof(YieldTestCases_Equals))]
  public void DefaultEqualityComparer_Equals(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    Assert.AreEqual(areEqual, AddressTableEntry.DefaultEqualityComparer.Equals(x, y), $"{message}: {x} {(areEqual ? "==" : "!=")} {y}");
    Assert.AreEqual(areEqual, AddressTableEntry.DefaultEqualityComparer.Equals(y, x), $"{message}: {y} {(areEqual ? "==" : "!=")} {x}");
  }

  [TestCaseSource(nameof(YieldTestCases_Equals))]
  public void DefaultEqualityComparer_GetHashCode(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    var hashCodeX = AddressTableEntry.DefaultEqualityComparer.GetHashCode(x);
    var hashCodeY = AddressTableEntry.DefaultEqualityComparer.GetHashCode(y);

    if (areEqual)
      Assert.AreEqual(hashCodeX, hashCodeY, $"{message}: HashCode {x} == {y}");
    else
      Assert.AreNotEqual(hashCodeX, hashCodeY, $"{message}: HashCode {x} != {y}");
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_ExceptState))]
  public void ExceptStateEqualityComparer_Equals(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    Assert.AreEqual(areEqual, AddressTableEntry.ExceptStateEqualityComparer.Equals(x, y), $"{message}: {x} {(areEqual ? "==" : "!=")} {y}");
    Assert.AreEqual(areEqual, AddressTableEntry.ExceptStateEqualityComparer.Equals(y, x), $"{message}: {y} {(areEqual ? "==" : "!=")} {x}");
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_ExceptState))]
  public void ExceptStateEqualityComparer_GetHashCode(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    var hashCodeX = AddressTableEntry.ExceptStateEqualityComparer.GetHashCode(x);
    var hashCodeY = AddressTableEntry.ExceptStateEqualityComparer.GetHashCode(y);

    if (areEqual)
      Assert.AreEqual(hashCodeX, hashCodeY, $"{message}: HashCode {x} == {y}");
    else
      Assert.AreNotEqual(hashCodeX, hashCodeY, $"{message}: HashCode {x} != {y}");
  }

  private static System.Collections.IEnumerable YieldTestCases_Equals_Object()
  {
    const bool areEqual = true;
    const bool areNotEqual = false;

    yield return new object[] {
      areEqual,
      new AddressTableEntry(
        ipAddress: IPAddress.Parse("192.0.2.0"),
        physicalAddress: PhysicalAddress.Parse("00-00-5E-00-53-00"),
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: "wlan0"
      ),
      new AddressTableEntry(
        ipAddress: IPAddress.Parse("192.0.2.0"),
        physicalAddress: PhysicalAddress.Parse("00-00-5E-00-53-00"),
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: "wlan0"
      )
    };

    yield return new object[] {
      areNotEqual,
      new AddressTableEntry(
        ipAddress: IPAddress.Parse("192.0.2.0"),
        physicalAddress: PhysicalAddress.Parse("00-00-5E-00-53-00"),
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: "wlan0"
      ),
      new AddressTableEntry(
        ipAddress: IPAddress.Parse("192.0.2.1"),
        physicalAddress: PhysicalAddress.Parse("00-00-5E-00-53-01"),
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: "wlan1"
      )
    };

    yield return new object[] {
      areNotEqual,
      new AddressTableEntry(
        ipAddress: IPAddress.Parse("192.0.2.0"),
        physicalAddress: PhysicalAddress.Parse("00-00-5E-00-53-00"),
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: "wlan0"
      ),
      null!
    };

    yield return new object[] {
      areNotEqual,
      new AddressTableEntry(
        ipAddress: IPAddress.Parse("192.0.2.0"),
        physicalAddress: PhysicalAddress.Parse("00-00-5E-00-53-00"),
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: "wlan0"
      ),
      "string"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_Object))]
  public void Equals_Object(bool expected, AddressTableEntry entry, object obj)
    => Assert.AreEqual(expected, entry.Equals(obj), $"{entry} {(expected ? "==" : "!=")} {obj}");

  [Test]
  public void Equals_IPAddress()
  {
    var entry = new AddressTableEntry(
      ipAddress: TestIPAddress,
      physicalAddress: null,
      isPermanent: true,
      state: AddressTableEntryState.None,
      interfaceId: null
    );

    Assert.IsFalse(entry.Equals((IPAddress?)null), "#1");
    Assert.IsFalse(entry.Equals(IPAddress.Any), "#2");
    Assert.IsTrue(entry.Equals(entry.IPAddress), "#3");
    Assert.IsTrue(entry.Equals(IPAddress.Parse(entry.IPAddress!.ToString())), "#4");
  }

  [Test]
  public void Equals_PhysicalAddressNull()
  {
    var entry = new AddressTableEntry(
      ipAddress: TestIPAddress,
      physicalAddress: null,
      isPermanent: true,
      state: AddressTableEntryState.None,
      interfaceId: null
    );

    Assert.IsTrue(entry.Equals((PhysicalAddress?)null), "#1");
    Assert.IsFalse(entry.Equals(PhysicalAddress.None), "#2");
    Assert.IsFalse(entry.Equals(TestMacAddress), "#3");
  }

  [Test]
  public void Equals_PhysicalAddressNotNull()
  {
    var entry = new AddressTableEntry(
      ipAddress: TestIPAddress,
      physicalAddress: TestMacAddress,
      isPermanent: true,
      state: AddressTableEntryState.None,
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
      new AddressTableEntry(
        ipAddress: TestIPAddress,
        physicalAddress: null,
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: null
      )
    };
    yield return new object?[] {
      new AddressTableEntry(
        ipAddress: TestIPAddress,
        physicalAddress: TestMacAddress,
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: null
      )
    };
    yield return new object?[] {
      new AddressTableEntry(
        ipAddress: TestIPAddress,
        physicalAddress: TestMacAddress,
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: "eth0"
      )
    };
  }

  [TestCaseSource(nameof(YieldTestCases_ToString))]
  public void ToString(AddressTableEntry entry)
  {
    Assert.DoesNotThrow(() => entry.ToString());

    var str = entry.ToString();

    Assert.IsNotNull(str);
    Assert.IsNotEmpty(str);
  }
}
