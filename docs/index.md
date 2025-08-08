# Introduction

This documentation covers the Subject Interface v1, an open protocol for communication between Cooperative Intelligent Transportation System (C-ITS) participants.

!!! note "TLC Terminology"
    The interface originally served Traffic Light Controllers (TLCs), and this legacy terminology persists in the API specifications for backward compatibility. "TLC" now refers to any connected entity: traffic controllers, sensors, barriers, displays, or any C-ITS participant. See [Terminology Reference](appendix/evolution-terminology.md) for the mapping between legacy and generic terminology.

## What is the Subject Interface?

The Subject Interface defines a vendor-neutral, license-free communication protocol for interoperability across C-ITS ecosystems. It provides data exchange between roadside devices, central systems, and service providers through a dual-layer architecture:

- **JSON-REST API Layer** - Administrative control and lifecycle management
- **TCP Streaming Protocol Layer** - Low latency data exchange

## Key Features

- **Low latency communication** for C-ITS applications
- **Bidirectional data exchange** with continuous asynchronous streaming
- **Message-agnostic architecture** supporting any payload type
- **Role-based access control** with security framework
- **Horizontal scaling** support for distributed deployments

## Introduction Contents

### [Communication Patterns](introduction/communication-patterns.md)
Roadside-to-central and central-to-central communication patterns, supported entity types, and system topology.

## Documentation Structure

This documentation is organized into main chapters:

### [Architecture](architecture/index.md)
System architecture, components, security framework, and role-based access control.

### [REST API](api-endpoints/index.md)
JSON-REST API reference including session management, registration, and token operations.

### [TCP Streaming Protocol](tcp-streaming-protocol/index.md)
Technical specifications for the streaming protocol including datagram types and connection management.

### [Example](examples/index.md)
Complete implementation walkthrough demonstrating both API and streaming protocol usage.

## Getting Started

For system integrators and developers new to the Subject Interface:

1. **Review this introduction** to understand the interface and communication patterns
2. **Study [Architecture](architecture/index.md)** for system architecture, security, and access model
3. **Implement using [REST API](api-endpoints/index.md) and [TCP Streaming Protocol](tcp-streaming-protocol/index.md)** specifications
4. **See [Example](examples/index.md)** for a complete implementation walkthrough