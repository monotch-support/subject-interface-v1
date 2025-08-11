# TCP Streaming Protocol

The TCP Streaming Protocol is the high-performance data exchange layer of the Subject Interface. It provides continuous asynchronous bidirectional streaming of datagrams between C-ITS participants. This chapter provides technical specifications for implementing and utilizing the TCP streaming protocol.

!!! note "TLC Terminology"
    The interface originally served Traffic Light Controllers (TLCs), and this legacy terminology persists in the API specifications for backward compatibility. "TLC" now refers to any connected entity: traffic controllers, sensors, barriers, displays, or any C-ITS participant. See [Terminology Reference](appendix/evolution-terminology.md) for the mapping between legacy and generic terminology.

## Protocol Overview

| Feature | Description |
|---------|-------------|
| **Ultra-low Latency** | Optimized communication for real-time C-ITS operations |
| **Continuous Bidirectional** | Asynchronous data streams in both directions |
| **Message-agnostic** | Handles any payload type without protocol restrictions |
| **Time Synchronization** | Built-in clock synchronization and drift monitoring |
| **Optional TLS** | Configurable encryption for secure communications |

## Chapter Contents

- **[Protocol Fundamentals](protocol-fundamentals.md)** - Core protocol specifications and encoding
- **[Communication Modes](communication-modes.md)** - Session types and connection patterns
- **[Datagram Types](datagram-types.md)** - Detailed datagram format specifications
- **[Connection Management](connection-management.md)** - Lifecycle and monitoring procedures
- **[Protocol Enforcement](protocol-enforcement.md)** - Rate limiting, throughput control, timeout and clock sync enforcement

## Key Features

| Category | Capabilities |
|----------|-------------|
| **Performance** | Real-time data exchange; minimal overhead; efficient frame structure; high-frequency updates |
| **Reliability** | Automatic reconnection; time sync monitoring; connection health tracking; graceful error handling |
| **Flexibility** | Singleplex/multiplex modes; variable payload sizes; version negotiation; security mode selection |

## Implementation Requirements

| Requirement | Details |
|-------------|---------|
| **System Prerequisites** | Synchronized clocks (NTP/GPS); TCP/IP connectivity; sufficient bandwidth; low-latency paths |
| **Protocol Compliance** | Protocol version 0x01; all mandatory datagrams; time synchronization; connection lifecycle |