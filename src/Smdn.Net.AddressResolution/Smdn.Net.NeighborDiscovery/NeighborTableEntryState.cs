// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.NeighborDiscovery;

// ref: https://www.ietf.org/rfc/rfc2461.txt
// 7.3.2.  Neighbor Cache Entry States
public enum NeighborTableEntryState {
  None,

  Incomplete,
  Reachable,
  Stale,
  Delay,
  Probe,
}
