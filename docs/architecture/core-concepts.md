# Core Concepts

The Subject Interface is built on fundamental concepts that implementers must understand: the system entities, role-based access control, and session behaviors.

## System Entities

The system is built on an entity-relationship model that governs access control, data ownership, and communication patterns:

### Account
- Represents the identity of an authorization holder
- Can own multiple TLC registrations and authorizations
- Serves as the primary entity for access control and data ownership
- Enables individual or organizational control over interface resources

### Authorization
- Defines permission to use the API through a combination of Account, Domain, and Role
- Establishes what actions can be performed and what data can be accessed
- Multiple authorizations can exist per account across different domains
- Forms the basis for role-based access control implementation

### Domain
- Defines the operational scope or jurisdiction to which an authorization applies
- Enables segmentation of access control across different geographical or organizational boundaries
- Supports multi-tenant deployments and jurisdictional separation
- Allows for hierarchical access management structures

### TLC Registration
- Represents the registration of any connected subject within a specific domain
- Despite the "TLC" naming, encompasses all types of connected entities
- Includes unique identification and configuration parameters for each subject
- Serves as the reference point for session management and data routing

### AuthorizationTLC
- Defines the specific TLC scope for certain authorization types
- Only applicable to TLC_SYSTEM and TLC_ANALYST roles
- Enables fine-grained access control to specific subjects or groups of subjects
- Supports selective data access and operational permissions

### AuthorizationToken
- The actual security token used for API authentication
- One authorization can have multiple active tokens for operational flexibility
- Enables token rotation and management without disrupting service
- Provides the mechanism for secure API access across all endpoints

## Role-Based Access

The Subject Interface implements a role-based access control system with three role categories, each having administrative, operational, and analytical variants.

### Role Categories

| Category | Purpose | Typical Users | Key Capabilities |
|----------|---------|---------------|------------------|
| **TLC** | Subject ownership and data generation | Roadside equipment operators, central system administrators | Direct control, data generation/transmission, session lifecycle, subject configuration |
| **BROKER** | Data distribution and routing | Service providers, system integrators | Multi-subject handling, value-added services, data aggregation, cross-domain operations |
| **MONITOR** | Data observation and analysis | Monitoring platforms, research systems | Read-only access, performance monitoring, analytics, compliance verification |

### Role Structure

| Role | Usage | Primary Function |
|------|-------|------------------|
| **TLC_ADMIN** | Administrative | Manage subject registrations, authorizations, and tokens |
| **TLC_SYSTEM** | Operational | Create/update sessions and stream data for assigned subjects |
| **TLC_ANALYST** | Diagnostics | Monitor and analyze subject-specific data and sessions |
| **BROKER_ADMIN** | Administrative | Manage broker service authorizations and configurations |
| **BROKER_SYSTEM** | Operational | Create multiplex sessions and distribute data across subjects |
| **BROKER_ANALYST** | Diagnostics | Monitor and analyze broker service performance and data flows |
| **MONITOR_ADMIN** | Administrative | Manage monitoring service access and configurations |
| **MONITOR_SYSTEM** | Operational | Create read-only sessions and receive data from multiple subjects |
| **MONITOR_ANALYST** | Diagnostics | Analyze monitoring data and system performance trends |

## Session Behaviors

The Subject Interface supports three session types that correspond to the role categories, each with distinct operational characteristics:

| Session Type | Created By | Purpose | Capabilities |
|-------------|------------|---------|-------------|
| **TLC** | Data owners/generators | Production of information | Full control over data streams and connection parameters |
| **BROKER** | Distribution systems | Data routing and distribution | Intermediary between producers and consumers, value-added services |
| **MONITOR** | Observation systems | Data analysis and monitoring | Read-only access, cannot modify or control streams |

For technical implementation details including concurrency rules, data flow patterns, and protocol support, see [Communication Modes](../tcp-streaming-protocol/communication-modes.md#session-types).