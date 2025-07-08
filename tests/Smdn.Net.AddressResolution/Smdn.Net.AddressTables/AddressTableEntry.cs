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

    Assert.That(entry.IsEmpty, Is.True, nameof(entry.IsEmpty));
    Assert.That(entry.IPAddress, Is.Null, nameof(entry.IPAddress));
    Assert.That(entry.PhysicalAddress, Is.Null, nameof(entry.PhysicalAddress));
    Assert.That(entry.IsPermanent, Is.False, nameof(entry.IsPermanent));
    Assert.That(entry.State, Is.Default, nameof(entry.State));
    Assert.That(entry.InterfaceId, Is.Null, nameof(entry.InterfaceId));

    Assert.That(entry.Equals(AddressTableEntry.Empty), Is.True, "equals to AddressTableEntry.Empty");
    Assert.That(entry.Equals((IPAddress?)null), Is.True, "equals to (IPAddress)null");
    Assert.That(entry.Equals((PhysicalAddress?)null), Is.True, "equals to (PhysicalAddress)null");

    Assert.DoesNotThrow(() => entry.ToString());
    Assert.DoesNotThrow(() => entry.GetHashCode());
  }

  [Test]
  public void IsEmpty()
  {
    Assert.That(default(AddressTableEntry).IsEmpty, Is.True);
    Assert.That(AddressTableEntry.Empty.IsEmpty, Is.True);

    Assert.That(
      new AddressTableEntry(
        ipAddress: TestIPAddress,
        physicalAddress: null,
        isPermanent: true,
        state: AddressTableEntryState.None,
        interfaceId: null
      ).IsEmpty,
      Is.False
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

    const bool AreEqual = true;
    const bool AreNotEqual = false;

    yield return new object[] {
      AreEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0", AddressTableEntryState.None),
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0", AddressTableEntryState.None),
      "are equal"
    };

    yield return new object[] {
      AreNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0", AddressTableEntryState.None),
      AddressTableEntry.Empty,
      "are not equal to Empty"
    };

    yield return new object[] {
      AreEqual,
      AddressTableEntry.Empty,
      AddressTableEntry.Empty,
      "are equal (both empty)"
    };

    yield return new object[] {
      AreEqual,
      Create("192.0.2.0", null, true, "wlan0"),
      Create("192.0.2.0", null, true, "wlan0"),
      "are equal (both of PhysicalAddress are null)"
    };

    yield return new object[] {
      AreEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", false, "wlan0", AddressTableEntryState.None),
      Create("192.0.2.0", "00-00-5E-00-53-00", false, "wlan0", AddressTableEntryState.None),
      "are equal (both of IsPermanent are false)"
    };

    yield return new object[] {
      AreEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, null, AddressTableEntryState.None),
      Create("192.0.2.0", "00-00-5E-00-53-00", true, null, AddressTableEntryState.None),
      "are equal (both of InterfaceId are null)"
    };

    yield return new object[] {
      RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? AreEqual : AreNotEqual,
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
      AreNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.1", "00-00-5E-00-53-00", true, "wlan0"),
      "difference in IPAddress"
    };

    yield return new object[] {
      AreNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.0", "00-00-5E-00-53-01", true, "wlan0"),
      "difference in PhysicalAddress"
    };

    yield return new object[] {
      AreNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.0", null, true, "wlan0"),
      "difference in PhysicalAddress (null)"
    };

    yield return new object[] {
      AreNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.0", "00-00-5E-00-53-00", false, "wlan0"),
      "difference in IsPermanent"
    };

    yield return new object[] {
      exceptState ? AreEqual : AreNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0", AddressTableEntryState.None),
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0", AddressTableEntryState.Reachable),
      "difference in State"
    };

    yield return new object[] {
      AreNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan1"),
      "difference in InterfaceId"
    };

    yield return new object[] {
      AreNotEqual,
      Create("192.0.2.0", "00-00-5E-00-53-00", true, "wlan0"),
      Create("192.0.2.0", "00-00-5E-00-53-00", true, null),
      "difference in InterfaceId (null)"
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Equals))]
  public void Equals(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    Assert.That(x.Equals(y), Is.EqualTo(areEqual), $"{message}: {x} {(areEqual ? "==" : "!=")} {y}");
    Assert.That(y.Equals(x), Is.EqualTo(areEqual), $"{message}: {y} {(areEqual ? "==" : "!=")} {x}");
  }

  [TestCaseSource(nameof(YieldTestCases_Equals))]
  public void GetHashCode(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    var hashCodeX = x.GetHashCode();
    var hashCodeY = y.GetHashCode();

    if (areEqual)
      Assert.That(hashCodeY, Is.EqualTo(hashCodeX), $"{message}: HashCode {x} == {y}");
    else
      Assert.That(hashCodeY, Is.Not.EqualTo(hashCodeX), $"{message}: HashCode {x} != {y}");
  }

  [TestCaseSource(nameof(YieldTestCases_Equals))]
  public void DefaultEqualityComparer_Equals(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    Assert.That(AddressTableEntry.DefaultEqualityComparer.Equals(x, y), Is.EqualTo(areEqual), $"{message}: {x} {(areEqual ? "==" : "!=")} {y}");
    Assert.That(AddressTableEntry.DefaultEqualityComparer.Equals(y, x), Is.EqualTo(areEqual), $"{message}: {y} {(areEqual ? "==" : "!=")} {x}");
  }

  [TestCaseSource(nameof(YieldTestCases_Equals))]
  public void DefaultEqualityComparer_GetHashCode(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    var hashCodeX = AddressTableEntry.DefaultEqualityComparer.GetHashCode(x);
    var hashCodeY = AddressTableEntry.DefaultEqualityComparer.GetHashCode(y);

    if (areEqual)
      Assert.That(hashCodeY, Is.EqualTo(hashCodeX), $"{message}: HashCode {x} == {y}");
    else
      Assert.That(hashCodeY, Is.Not.EqualTo(hashCodeX), $"{message}: HashCode {x} != {y}");
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_ExceptState))]
  public void ExceptStateEqualityComparer_Equals(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    Assert.That(AddressTableEntry.ExceptStateEqualityComparer.Equals(x, y), Is.EqualTo(areEqual), $"{message}: {x} {(areEqual ? "==" : "!=")} {y}");
    Assert.That(AddressTableEntry.ExceptStateEqualityComparer.Equals(y, x), Is.EqualTo(areEqual), $"{message}: {y} {(areEqual ? "==" : "!=")} {x}");
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_ExceptState))]
  public void ExceptStateEqualityComparer_GetHashCode(bool areEqual, AddressTableEntry x, AddressTableEntry y, string? message)
  {
    var hashCodeX = AddressTableEntry.ExceptStateEqualityComparer.GetHashCode(x);
    var hashCodeY = AddressTableEntry.ExceptStateEqualityComparer.GetHashCode(y);

    if (areEqual)
      Assert.That(hashCodeY, Is.EqualTo(hashCodeX), $"{message}: HashCode {x} == {y}");
    else
      Assert.That(hashCodeY, Is.Not.EqualTo(hashCodeX), $"{message}: HashCode {x} != {y}");
  }

  private static System.Collections.IEnumerable YieldTestCases_Equals_Object()
  {
    const bool AreEqual = true;
    const bool AreNotEqual = false;

    yield return new object[] {
      AreEqual,
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
      AreNotEqual,
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
      AreNotEqual,
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
      AreNotEqual,
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
    => Assert.That(entry.Equals(obj), Is.EqualTo(expected), $"{entry} {(expected ? "==" : "!=")} {obj}");

  private static System.Collections.IEnumerable YieldTestCases_Equals_OfIPAddress()
  {
    var nonNullIPAddressEntry = new AddressTableEntry(
      ipAddress: TestIPAddress,
      physicalAddress: null,
      isPermanent: true,
      state: AddressTableEntryState.None,
      interfaceId: null
    );
    IPAddress? nullIPAddress = null;

    yield return new object?[] {
      nonNullIPAddressEntry,
      nullIPAddress,
      false
    };
    yield return new object?[] {
      nonNullIPAddressEntry,
      IPAddress.Any,
      false
    };
    yield return new object?[] {
      nonNullIPAddressEntry,
      nonNullIPAddressEntry.IPAddress,
      true
    };
    yield return new object?[] {
      nonNullIPAddressEntry,
      nonNullIPAddressEntry.IPAddress!.MapToIPv6(), // compare with fIPv4-mapped IPv6 address
      false
    };
    yield return new object?[] {
      nonNullIPAddressEntry,
      IPAddress.Parse(nonNullIPAddressEntry.IPAddress!.ToString()),
      true
    };

    var nullIPAddressEntry = default(AddressTableEntry);

    yield return new object?[] {
      nullIPAddressEntry,
      nullIPAddress,
      true
    };
    yield return new object?[] {
      nullIPAddressEntry,
      IPAddress.Any,
      false
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_OfIPAddress))]
  public void Equals_OfIPAddress(AddressTableEntry entry, IPAddress? ipAddress, bool expected)
    => Assert.That(entry.Equals(ipAddress), Is.EqualTo(expected));

  private static System.Collections.IEnumerable YieldTestCases_Equals_OfIPAddress_ShouldConsiderIPv4MappedIPv6Address()
  {
    var ipv4AddressEntry = new AddressTableEntry(
      ipAddress: TestIPAddress,
      physicalAddress: null,
      isPermanent: true,
      state: AddressTableEntryState.None,
      interfaceId: null
    );
    var ipv4MappedIPv6AddressEntry = new AddressTableEntry(
      ipAddress: TestIPAddress.MapToIPv6(),
      physicalAddress: null,
      isPermanent: true,
      state: AddressTableEntryState.None,
      interfaceId: null
    );
    IPAddress? nullIPAddress = null;

    foreach (var shouldConsiderIPv4MappedIPv6Address in new[] { true, false }) {
      yield return new object?[] {
        ipv4AddressEntry,
        nullIPAddress,
        shouldConsiderIPv4MappedIPv6Address,
        false
      };
      yield return new object?[] {
        ipv4AddressEntry,
        ipv4AddressEntry.IPAddress,
        shouldConsiderIPv4MappedIPv6Address,
        true
      };
      yield return new object?[] {
        ipv4AddressEntry,
        ipv4AddressEntry.IPAddress!.MapToIPv6(), // compare with IPv4-mapped IPv6 address
        shouldConsiderIPv4MappedIPv6Address,
        shouldConsiderIPv4MappedIPv6Address
      };
    }

    foreach (var shouldConsiderIPv4MappedIPv6Address in new[] { true, false }) {
      yield return new object?[] {
        ipv4MappedIPv6AddressEntry,
        nullIPAddress,
        shouldConsiderIPv4MappedIPv6Address,
        false
      };
      yield return new object?[] {
        ipv4MappedIPv6AddressEntry,
        ipv4MappedIPv6AddressEntry.IPAddress,
        shouldConsiderIPv4MappedIPv6Address,
        true
      };
      yield return new object?[] {
        ipv4MappedIPv6AddressEntry,
        ipv4MappedIPv6AddressEntry.IPAddress!.MapToIPv4(), // compare with IPv4-mapped IPv6 address
        shouldConsiderIPv4MappedIPv6Address,
        shouldConsiderIPv4MappedIPv6Address
      };
    }

    var nullIPAddressEntry = default(AddressTableEntry);

    foreach (var shouldConsiderIPv4MappedIPv6Address in new[] { true, false }) {
      yield return new object?[] {
        nullIPAddressEntry,
        nullIPAddress,
        shouldConsiderIPv4MappedIPv6Address,
        true
      };
      yield return new object?[] {
        nullIPAddressEntry,
        TestIPAddress,
        shouldConsiderIPv4MappedIPv6Address,
        false
      };
      yield return new object?[] {
        nullIPAddressEntry,
        TestIPAddress.MapToIPv6(),
        shouldConsiderIPv4MappedIPv6Address,
        false
      };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_Equals_OfIPAddress_ShouldConsiderIPv4MappedIPv6Address))]
  public void Equals_OfIPAddress_ShouldConsiderIPv4MappedIPv6Address(AddressTableEntry entry, IPAddress? ipAddress, bool shouldConsiderIPv4MappedIPv6Address, bool expected)
    => Assert.That(entry.Equals(ipAddress, shouldConsiderIPv4MappedIPv6Address), Is.EqualTo(expected));

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

    Assert.That(entry.Equals((PhysicalAddress?)null), Is.True, "#1");
    Assert.That(entry.Equals(PhysicalAddress.None), Is.False, "#2");
    Assert.That(entry.Equals(TestMacAddress), Is.False, "#3");
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

    Assert.That(entry.Equals((PhysicalAddress?)null), Is.False, "#1");
    Assert.That(entry.Equals(PhysicalAddress.None), Is.False, "#2");
    Assert.That(entry.Equals(entry.PhysicalAddress), Is.True, "#3");
    Assert.That(entry.Equals(PhysicalAddress.Parse(entry.PhysicalAddress!.ToMacAddressString())), Is.True, "#4");
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

    Assert.That(str, Is.Not.Null);
    Assert.That(str, Is.Not.Empty);
  }
}
