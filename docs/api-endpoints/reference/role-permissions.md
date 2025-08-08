# Endpoint Permissions Matrix

Quick reference for role-based access control across all Subject Interface API endpoints.

## Sessions API

| Endpoint | Method | TLC_ADMIN | TLC_SYSTEM | TLC_ANALYST | BROKER_ADMIN | BROKER_SYSTEM | BROKER_ANALYST | MONITOR_ADMIN | MONITOR_SYSTEM |
|----------|--------|-----------|------------|-------------|--------------|---------------|----------------|---------------|----------------|
| `/v1/sessions` | GET | Yes | Yes | No | Yes | Yes | No | Yes | Yes |
| `/v1/sessions` | POST | Yes | Yes | No | Yes | Yes | No | Yes | Yes |
| `/v1/sessions/{token}` | GET | Yes | Yes | No | Yes | Yes | No | Yes | Yes |
| `/v1/sessions/{token}` | PUT | Yes | Yes | No | Yes | Yes | No | Yes | Yes |
| `/v1/sessions/{token}` | DELETE | Yes | No | No | Yes | No | No | Yes | No |

**Notes:**
- TLC roles can only access sessions for their authorized TLC scope
- BROKER and MONITOR roles have domain-wide session access
- Only ADMIN roles can delete sessions

## Session Logs API

| Endpoint | Method | TLC_ADMIN | TLC_SYSTEM | TLC_ANALYST | BROKER_ADMIN | BROKER_SYSTEM | BROKER_ANALYST | MONITOR_ADMIN | MONITOR_SYSTEM |
|----------|--------|-----------|------------|-------------|--------------|---------------|----------------|---------------|----------------|
| `/v1/sessionlogs` | GET | Yes | No | Yes | Yes | No | Yes | Yes | Yes |
| `/v1/sessionlogs/{token}` | GET | Yes | No | Yes | Yes | No | Yes | Yes | Yes |

**Notes:**
- SYSTEM roles generally don't need historical log access
- ANALYST and ADMIN roles have read access to logs
- MONITOR_SYSTEM has special read access for operational monitoring

## TLCs API

| Endpoint | Method | TLC_ADMIN | TLC_SYSTEM | TLC_ANALYST | BROKER_ADMIN | BROKER_SYSTEM | BROKER_ANALYST | MONITOR_ADMIN | MONITOR_SYSTEM |
|----------|--------|-----------|------------|-------------|--------------|---------------|----------------|---------------|----------------|
| `/v1/tlcs` | GET | Yes | No | Yes | Yes | Yes | Yes | Yes | Yes |
| `/v1/tlcs` | POST | Yes | No | No | No | No | No | No | No |
| `/v1/tlcs/{uuid}` | GET | Yes | No | Yes | Yes | Yes | Yes | Yes | Yes |
| `/v1/tlcs/{uuid}` | DELETE | Yes | No | No | No | No | No | No | No |

**Notes:**
- Only TLC_ADMIN can create and delete TLC registrations
- Most roles can read TLC information for discovery
- TLC_SYSTEM doesn't need TLC management access

## Authorizations API

| Endpoint | Method | TLC_ADMIN | TLC_SYSTEM | TLC_ANALYST | BROKER_ADMIN | BROKER_SYSTEM | BROKER_ANALYST | MONITOR_ADMIN | MONITOR_SYSTEM |
|----------|--------|-----------|------------|-------------|--------------|---------------|----------------|---------------|----------------|
| `/v1/authorizations` | GET | Yes | No | No | Yes | No | No | Yes | No |
| `/v1/authorizations` | POST | Yes | No | No | Yes | No | No | Yes | No |
| `/v1/authorizations/{uuid}` | GET | Yes | No | No | Yes | No | No | Yes | No |
| `/v1/authorizations/{uuid}` | PUT | Yes | No | No | Yes | No | No | Yes | No |
| `/v1/authorizations/{uuid}` | DELETE | Yes | No | No | Yes | No | No | Yes | No |

**Notes:**
- Only ADMIN roles can manage authorizations
- Each ADMIN role can only manage authorizations within their category
- SYSTEM and ANALYST roles have no authorization management access

## Authorization Tokens API

| Endpoint | Method | TLC_ADMIN | TLC_SYSTEM | TLC_ANALYST | BROKER_ADMIN | BROKER_SYSTEM | BROKER_ANALYST | MONITOR_ADMIN | MONITOR_SYSTEM |
|----------|--------|-----------|------------|-------------|--------------|---------------|----------------|---------------|----------------|
| `/v1/authorizationtokens` | GET | Yes | No | No | Yes | No | No | Yes | No |
| `/v1/authorizationtokens` | POST | Yes | No | No | Yes | No | No | Yes | No |
| `/v1/authorizationtokens/{uuid}` | GET | Yes | No | No | Yes | No | No | Yes | No |
| `/v1/authorizationtokens/{uuid}` | PUT | Yes | No | No | Yes | No | No | Yes | No |
| `/v1/authorizationtokens/{uuid}` | DELETE | Yes | No | No | Yes | No | No | Yes | No |

**Notes:**
- Only ADMIN roles can manage tokens
- Token management is restricted to same role category
- Token revocation is immediate and irreversible

---

**See Also:**
- **Implementation:** [API Endpoints Reference](endpoints-reference.md#authorizations)
- **API Reference:** [API Endpoints Reference](endpoints-reference.md)
- **Getting Started:** [Quick Start Guide](../getting-started/quick-start.md)
- **Architecture:** [Core Concepts](../../architecture/core-concepts.md)