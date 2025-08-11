# Architecture

This chapter covers the Subject Interface system architecture, security framework, and access control mechanisms.

!!! note "TLC Terminology"
    The interface originally served Traffic Light Controllers (TLCs), and this legacy terminology persists in the API specifications for backward compatibility. "TLC" now refers to any connected entity: traffic controllers, sensors, barriers, displays, or any C-ITS participant. See [Terminology Reference](appendix/evolution-terminology.md) for the mapping between legacy and generic terminology.

## Architecture Overview

The Subject Interface employs a dual-layer architecture for administrative control and data exchange:

- **JSON-REST API Layer** - Administrative control, lifecycle management, and configuration
- **TCP Streaming Protocol Layer** - Real-time data exchange

## Chapter Contents

- **[Dual-Layer Architecture](system-architecture.md)** - Dual-layer architecture, API nodes, streaming service nodes, and component interactions
- **[Core Concepts](core-concepts.md)** - System entities, role-based access control, and session behaviors
- **[Security Framework](security-framework.md)** - Authorization framework, token-based authentication, and TCP streaming security

## Key Architectural Principles

| Principle | Implementation |
|-----------|---------------|
| **Separation of Concerns** | Distinct REST API and streaming layers |
| **Scalability** | Horizontal scaling, distributed architecture |
| **Security in Depth** | Multi-layer security model |
| **Operational Flexibility** | Configurable security levels, multiple tokens |