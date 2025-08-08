# API Endpoints Reference

Complete reference for all Subject Interface REST API endpoints with request/response examples and field definitions.

## Data Types

| Type          | Description              | Example                                   |
| ------------- | ------------------------ | ----------------------------------------- |
| **string**    | Text value               | `"example-value"`                         |
| **number**    | Numeric value            | `8443`                                    |
| **boolean**   | True/false value         | `true`                                    |
| **array**     | List of values           | `["value1", "value2"]`                    |
| **object**    | JSON object              | `{"key": "value"}`                        |
| **enum**      | Fixed set of values      | `"TLC"` (from `TLC`, `Broker`, `Monitor`) |
| **timestamp** | Unix epoch milliseconds  | `1640995200000`                           |
| **iso8601**   | ISO 8601 datetime string | `"2024-01-15T10:30:00Z"`                  |
| **uuid**      | UUID identifier          | `"550e8400-e29b-41d4-a716-446655440000"`  |

## Authentication

**Header:** `X-Authorization: your-token-value`

**Required for:** All endpoints except public documentation

## Sessions

### List Sessions
```http
GET /v1/sessions
GET /v1/sessions?type=TLC
GET /v1/sessions?protocol=TCPStreaming_Multiplex
```

**Required Roles:** TLC/Broker/Monitor Admin/System

**Query Parameters:**

| Parameter  | Type | Required | Description            | Valid Values                                        |
| ---------- | ---- | -------- | ---------------------- | --------------------------------------------------- |
| `type`     | enum | No       | Filter by session type | `TLC`, `Broker`, `Monitor`                          |
| `protocol` | enum | No       | Filter by protocol     | `TCPStreaming_Singleplex`, `TCPStreaming_Multiplex` |

**Response:** Array of session objects

```json
[
  {
    "domain": "your-domain",
    "type": "TLC",
    "token": "session-token-123",
    "protocol": "TCPStreaming_Singleplex",
    "details": {
      "securityMode": "TLSv1.2",
      "listener": {
        "host": "streaming.example.com",
        "port": 8443,
        "expiration": "2024-01-15T10:30:00Z"
      },
      "tlcIdentifier": "device01"
    }
  }
]
```

**Response Fields:**

| Field | Type | Description | Notes |
|-------|------|-------------|-------|
| `domain` | string | Domain context | Matches token's domain |
| `type` | enum | Session type | `TLC`, `Broker`, or `Monitor` |
| `token` | string | Unique session identifier | System-generated |
| `protocol` | enum | Communication protocol | `TCPStreaming_Singleplex` or `TCPStreaming_Multiplex` |
| `details` | object | Session configuration and limits | Contains operational parameters |
| `details.securityMode` | enum | Encryption mode | `NONE` or `TLSv1.2` |
| `details.keepAliveTimeout` | iso8601 | Maximum idle time before connection timeout | ISO 8601 duration format. See [Connection Timeout Enforcement](../../tcp-streaming-protocol/protocol-enforcement.md#connection-timeout-enforcement) |
| `details.payloadRateLimit` | number | Maximum payload messages per duration | Rate limiting for message frequency. See [Payload Rate Limiting](../../tcp-streaming-protocol/protocol-enforcement.md#payload-rate-limiting) |
| `details.payloadRateLimitDuration` | iso8601 | Time window for rate limit | ISO 8601 duration format. See [Payload Rate Limiting](../../tcp-streaming-protocol/protocol-enforcement.md#payload-rate-limiting) |
| `details.payloadThroughputLimit` | number | Maximum bytes per duration | Bandwidth limiting in bytes. See [Throughput Limiting](../../tcp-streaming-protocol/protocol-enforcement.md#throughput-limiting) |
| `details.payloadThroughputLimitDuration` | iso8601 | Time window for throughput limit | ISO 8601 duration format. See [Throughput Limiting](../../tcp-streaming-protocol/protocol-enforcement.md#throughput-limiting) |
| `details.clockDiffLimit` | iso8601 | Maximum allowed clock difference | Time synchronization tolerance. See [Clock Sync Enforcement](../../tcp-streaming-protocol/protocol-enforcement.md#clock-synchronization-enforcement) |
| `details.clockDiffLimitDuration` | iso8601 | Clock difference measurement window | ISO 8601 duration format. See [Clock Sync Enforcement](../../tcp-streaming-protocol/protocol-enforcement.md#clock-synchronization-enforcement) |
| `details.listener` | object | Connection details | Present in responses only |
| `details.listener.host` | string | Streaming server hostname | System-determined |
| `details.listener.port` | number | TCP port number | Range: 1024-65535 |
| `details.listener.expiration` | iso8601 | Session expiration time | ISO 8601 with Z suffix |
| `details.tlcIdentifier` | string | Single TLC identifier | For singleplex sessions only |
| `details.tlcIdentifiers` | array | Multiple TLC identifiers | For multiplex sessions only |

### Create Session
```http
POST /v1/sessions
```

**Required Roles:** TLC/Broker/Monitor Admin/System

**Request Body:**
```json
{
  "domain": "your-domain",
  "type": "TLC\|Broker\|Monitor",
  "protocol": "TCPStreaming_Singleplex|TCPStreaming_Multiplex",
  "details": {
    "securityMode": "NONE|TLSv1.2",
    "tlcIdentifier": "device01",          // Singleplex only
    "tlcIdentifiers": ["sub00001", "sub00002"]  // Multiplex only
  }
}
```

**Request Fields:**

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `domain` | string | Yes | Domain context | Must match token's domain |
| `type` | enum | Yes | Session type | `TLC`, `Broker`, or `Monitor` |
| `protocol` | enum | Yes | Communication protocol | `TCPStreaming_Singleplex` or `TCPStreaming_Multiplex` |
| `details` | object | Yes | Session configuration | Structure varies by protocol |
| `details.securityMode` | enum | Yes | Encryption mode | `NONE` or `TLSv1.2` |
| `details.tlcIdentifier` | string | Conditional | Single TLC identifier | Required for singleplex, 8 chars, lowercase alphanumeric, underscore, hyphen |
| `details.tlcIdentifiers` | array | Conditional | Multiple TLC identifiers | Required for multiplex, 1-100 items |

**Response:** Session object with connection details (same structure as List Sessions)

```json
{
  "domain": "dev_001",
  "type": "TLC", 
  "token": "session-token-abc123",
  "protocol": "TCPStreaming_Singleplex",
  "details": {
    "securityMode": "TLSv1.2",
    "keepAliveTimeout": "PT10S",
    "payloadRateLimit": 12,
    "payloadRateLimitDuration": "PT5S",
    "payloadThroughputLimit": 60,
    "payloadThroughputLimitDuration": "PT5S",
    "clockDiffLimit": "PT3S",
    "clockDiffLimitDuration": "PT1M",
    "listener": {
      "host": "streaming.example.com",
      "port": 8443,
      "expiration": "2024-01-15T10:30:00Z"
    },
    "tlcIdentifier": "sub00001"
  }
}
```

**Response Fields:**

| Field | Type | Description | Notes |
|-------|------|-------------|-------|
| `domain` | string | Domain context | Matches request domain |
| `type` | enum | Session type | `TLC`, `Broker`, or `Monitor` |
| `token` | string | Unique session identifier | System-generated, used for streaming connection |
| `protocol` | enum | Communication protocol | `TCPStreaming_Singleplex` or `TCPStreaming_Multiplex` |
| `details` | object | Session configuration and limits | Contains operational parameters |
| `details.securityMode` | enum | Encryption mode | `NONE` or `TLSv1.2` |
| `details.keepAliveTimeout` | iso8601 | Maximum idle time before connection timeout | ISO 8601 duration format. See [Connection Timeout Enforcement](../../tcp-streaming-protocol/protocol-enforcement.md#connection-timeout-enforcement) |
| `details.payloadRateLimit` | number | Maximum payload messages per duration | Rate limiting for message frequency. See [Payload Rate Limiting](../../tcp-streaming-protocol/protocol-enforcement.md#payload-rate-limiting) |
| `details.payloadRateLimitDuration` | iso8601 | Time window for rate limit | ISO 8601 duration format. See [Payload Rate Limiting](../../tcp-streaming-protocol/protocol-enforcement.md#payload-rate-limiting) |
| `details.payloadThroughputLimit` | number | Maximum bytes per duration | Bandwidth limiting in bytes. See [Throughput Limiting](../../tcp-streaming-protocol/protocol-enforcement.md#throughput-limiting) |
| `details.payloadThroughputLimitDuration` | iso8601 | Time window for throughput limit | ISO 8601 duration format. See [Throughput Limiting](../../tcp-streaming-protocol/protocol-enforcement.md#throughput-limiting) |
| `details.clockDiffLimit` | iso8601 | Maximum allowed clock difference | Time synchronization tolerance. See [Clock Sync Enforcement](../../tcp-streaming-protocol/protocol-enforcement.md#clock-synchronization-enforcement) |
| `details.clockDiffLimitDuration` | iso8601 | Clock difference measurement window | ISO 8601 duration format. See [Clock Sync Enforcement](../../tcp-streaming-protocol/protocol-enforcement.md#clock-synchronization-enforcement) |
| `details.listener` | object | Connection details | Streaming endpoint information |
| `details.listener.host` | string | Streaming server hostname | System-determined endpoint |
| `details.listener.port` | number | TCP port number | Range: 1024-65535 |
| `details.listener.expiration` | iso8601 | Session expiration time | ISO 8601 with Z suffix |
| `details.tlcIdentifier` | string | Single TLC identifier | For singleplex sessions only |
| `details.tlcIdentifiers` | array | Multiple TLC identifiers | For multiplex sessions only |

### Get Session
```http
GET /v1/sessions/{token}
```

**Required Roles:** TLC/Broker/Monitor Admin/System

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `token` | string | Yes | Session token |

**Response:** Single session object (same structure as List Sessions)

### Update Session
```http
PUT /v1/sessions/{token}
```

**Required Roles:** TLC/Broker/Monitor Admin/System

**Note:** Multiplex sessions only

**Request Body:**
```json
{
  "securityMode": "TLSv1.2",  // Must match existing
  "tlcIdentifiers": ["sub00001", "sub00002", "sub00003"]
}
```

**Request Fields:**

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `securityMode` | enum | Yes | Encryption mode | Must match existing session |
| `tlcIdentifiers` | array | Yes | Updated TLC list | 1-100 valid TLC identifiers |

### Delete Session
```http
DELETE /v1/sessions/{token}
```

**Required Roles:** TLC/Broker/Monitor Admin

**Response:** 204 No Content

## Session Logs

### List Session Logs
```http
GET /v1/sessionlogs?from=2024-01-15T00:00:00Z&until=2024-01-15T23:59:59Z
```

**Required Roles:** TLC/Broker Admin/Analyst, Monitor Admin/System

**Query Parameters:**

| Parameter | Type | Required | Description | Format |
|-----------|------|----------|-------------|--------|
| `from` | iso8601 | Yes | Start time | ISO 8601 with Z suffix |
| `until` | iso8601 | Yes | End time | ISO 8601 with Z suffix |

**Response:** Array of session log objects

```json
[
  {
    "token": "session-token-123",
    "domain": "your-domain",
    "account": "your-account",
    "type": "TLC",
    "protocol": "TCPStreaming_Singleplex",
    "details": "{\"securityMode\":\"TLSv1.2\",\"tlcIdentifier\":\"device01\"}",
    "created": 1640995200000,
    "connected": 1640995205000,
    "ended": 1640998800000,
    "endReason": "CLIENT_DISCONNECT",
    "remoteAddress": "192.168.1.100"
  }
]
```

**Response Fields:**

| Field | Type | Required | Description | Notes |
|-------|------|----------|-------------|-------|
| `token` | string | Yes | Session token | Unique identifier |
| `domain` | string | Yes | Domain context | Domain name |
| `account` | string | Yes | Account identifier | Account name |
| `type` | enum | Yes | Session type | `TLC`, `Broker`, or `Monitor` |
| `protocol` | enum | Yes | Protocol used | `TCPStreaming_Singleplex` or `TCPStreaming_Multiplex` |
| `details` | string | Yes | JSON-encoded session details | Serialized configuration |
| `created` | timestamp | Yes | Session creation time | Unix epoch milliseconds |
| `connected` | timestamp | No | First connection time | Unix epoch milliseconds, null if never connected |
| `ended` | timestamp | No | Session end time | Unix epoch milliseconds, null if still active |
| `endReason` | string | No | Termination reason | See End Reasons below |
| `remoteAddress` | string | No | Client IP address | IPv4/IPv6 address |

**End Reasons:**

| Reason | Description | Cause |
|--------|-------------|-------|
| `CLIENT_DISCONNECT` | Client closed connection | Normal client-initiated termination |
| `SESSION_EXPIRED` | Session reached expiration | Time-based expiration |
| `ADMIN_TERMINATION` | Admin deleted session | Manual termination via DELETE endpoint |
| `TLC_DELETED` | Referenced TLC was deleted | TLC resource removal |
| `TOKEN_REVOKED` | Session token was revoked | Token management action |
| `CONNECTION_ERROR` | Network/TCP error | Technical connectivity issue |
| `PROTOCOL_ERROR` | Streaming protocol violation | Invalid client behavior |

### Get Session Log
```http
GET /v1/sessionlogs/{token}
```

**Required Roles:** TLC/Broker Admin/Analyst, Monitor Admin/System

**Response:** Single session log object (same structure as List Session Logs)

## TLCs (Subjects)

### List TLCs
```http
GET /v1/tlcs
```

**Required Roles:** TLC Admin/Analyst, Broker Admin/System/Analyst, Monitor Admin/System

**Response:** Array of TLC objects

```json
[
  {
    "uuid": "550e8400-e29b-41d4-a716-446655440000",
    "identifier": "device01",
    "domain": "your-domain", 
    "account": "your-account",
    "type": "TCPStreaming"
  }
]
```

**Response Fields:**

| Field | Type | Description | Notes |
|-------|------|-------------|-------|
| `uuid` | uuid | Unique TLC identifier | System-generated UUID |
| `identifier` | string | TLC identifier | 8 chars, pattern: `^[a-z0-9_-]{8}$` |
| `domain` | string | Domain context | Inherited from token |
| `account` | string | Account context | Inherited from token |
| `type` | enum | TLC type | Currently only `TCPStreaming` |

### Create TLC
```http
POST /v1/tlcs
```

**Required Roles:** TLC Admin

**Request Body:**
```json
{
  "identifier": "device01"  // 8 chars, lowercase alphanumeric, underscore, hyphen
}
```

**Request Fields:**

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `identifier` | string | Yes | TLC identifier | Exactly 8 chars, pattern: `^[a-z0-9_-]{8}$`, unique within domain |

**TLC Identifier Examples:**

- **Valid:** `sub00001`, `device01`, `traffic1`, `sensor99`, `ctrl0001`, `zone12ab`, `dev_0001`, `test-001`, `prod_v01`
- **Invalid:** `Device01` (uppercase), `device1` (7 chars), `device001` (9 chars), `dev@001` (invalid char)

**Response:** TLC object with generated UUID (same structure as List TLCs)

### Get TLC
```http
GET /v1/tlcs/{uuid}
```

**Required Roles:** TLC Admin/Analyst, Broker Admin/System/Analyst, Monitor Admin/System

**Response:** Single TLC object (same structure as List TLCs)

### Delete TLC
```http
DELETE /v1/tlcs/{uuid}
```

**Required Roles:** TLC Admin

**Response:** 204 No Content

**Note:** Terminates any active sessions using this TLC

## Authorizations

### List Authorizations
```http
GET /v1/authorizations
```

**Required Roles:** TLC/Broker/Monitor Admin

**Response:** Array of authorization objects

```json
[
  {
    "uuid": "660f9500-f39c-52e5-b827-556766551111",
    "domain": "your-domain",
    "account": "your-account",
    "role": "TLC_SYSTEM",
    "tlcIdentifiers": ["device01", "device02"]
  }
]
```

**Response Fields:**

| Field | Type | Description | Notes |
|-------|------|-------------|-------|
| `uuid` | uuid | Unique authorization ID | System-generated UUID |
| `domain` | string | Domain context | Inherited from token |
| `account` | string | Account context | Inherited from token |
| `role` | enum | Operational role | See Role Types below |
| `tlcIdentifiers` | array | TLC scope restriction | Optional, for TLC roles only |

**Role Types:**

| Role | Category | Level | Description |
|------|----------|--------|-------------|
| `TLC_ADMIN` | TLC | Administrative | TLC management and configuration |
| `TLC_SYSTEM` | TLC | Operational | Data generation and streaming |
| `TLC_ANALYST` | TLC | Analytical | Monitoring and analysis |
| `BROKER_ADMIN` | BROKER | Administrative | Broker service management |
| `BROKER_SYSTEM` | BROKER | Operational | Data aggregation and distribution |
| `BROKER_ANALYST` | BROKER | Analytical | Broker performance analysis |
| `MONITOR_ADMIN` | MONITOR | Administrative | Monitoring service management |
| `MONITOR_SYSTEM` | MONITOR | Operational | Read-only data consumption |

**TLC Scope Rules:**

| Role Category | Scope Behavior | Notes |
|---------------|----------------|-------|
| **TLC Roles** | Optional TLC restriction | Empty array = domain-wide access |
| **BROKER Roles** | Always domain-wide | `tlcIdentifiers` ignored |
| **MONITOR Roles** | Always domain-wide | `tlcIdentifiers` ignored |

### Create Authorization
```http
POST /v1/authorizations
```

**Required Roles:** TLC/Broker/Monitor Admin

**Request Body:**
```json
{
  "role": "TLC_ADMIN\|TLC_SYSTEM\|TLC_ANALYST\|BROKER_ADMIN\|BROKER_SYSTEM\|BROKER_ANALYST\|MONITOR_ADMIN\|MONITOR_SYSTEM",
  "tlcIdentifiers": ["device01", "device02"]  // Optional, TLC roles only
}
```

**Request Fields:**

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `role` | enum | Yes | Operational role | Must be valid role from list above |
| `tlcIdentifiers` | array | No | TLC scope restriction | Optional for TLC roles, ignored for others |

**Response:** Authorization object with generated UUID (same structure as List Authorizations)

### Get Authorization
```http
GET /v1/authorizations/{uuid}
```

**Required Roles:** TLC/Broker/Monitor Admin

**Response:** Single authorization object (same structure as List Authorizations)

### Update Authorization
```http
PUT /v1/authorizations/{uuid}
```

**Required Roles:** TLC/Broker/Monitor Admin

**Request Body:**
```json
{
  "role": "TLC_ANALYST",
  "tlcIdentifiers": ["device01"]
}
```

**Request Fields:** Same as Create Authorization

### Delete Authorization
```http
DELETE /v1/authorizations/{uuid}
```

**Required Roles:** TLC/Broker/Monitor Admin

**Response:** 204 No Content

**Note:** Automatically revokes all associated tokens

## Authorization Tokens

### List Tokens
```http
GET /v1/authorizationtokens
GET /v1/authorizationtokens?authorization={uuid}
```

**Required Roles:** TLC/Broker/Monitor Admin

**Query Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `authorization` | uuid | No | Filter by authorization UUID |

**Response:** Array of token objects

```json
[
  {
    "uuid": "770fa600-04ad-63f6-c938-667877662222",
    "token": "dtNB_vhvJ0wgTGf1N0DxN38_AmTL_4yiPRZdqZSuK3k",
    "authorization": "660f9500-f39c-52e5-b827-556766551111"
  }
]
```

**Response Fields:**

| Field | Type | Description | Notes |
|-------|------|-------------|-------|
| `uuid` | uuid | Unique token ID | System-generated UUID |
| `token` | string | Token value | Base64url-encoded, cryptographically secure |
| `authorization` | uuid | Associated authorization | Must be valid authorization UUID |

**Token Format:**

- Base64url-encoded strings
- Cryptographically random
- URL-safe characters only
- No expiration mechanism
- Case-sensitive
- Example: `dtNB_vhvJ0wgTGf1N0DxN38_AmTL_4yiPRZdqZSuK3k`

### Create Token
```http
POST /v1/authorizationtokens
```

**Required Roles:** TLC/Broker/Monitor Admin

**Request Body:**
```json
{
  "authorization": "660f9500-f39c-52e5-b827-556766551111"
}
```

**Request Fields:**

| Field | Type | Required | Description | Validation |
|-------|------|----------|-------------|------------|
| `authorization` | uuid | Yes | Associated authorization | Must be valid authorization UUID within domain |

**Response:** Token object with generated token value (same structure as List Tokens)

### Get Token
```http
GET /v1/authorizationtokens/{uuid}
```

**Required Roles:** TLC/Broker/Monitor Admin

**Response:** Single token object (same structure as List Tokens)

### Update Token
```http
PUT /v1/authorizationtokens/{uuid}
```

**Required Roles:** TLC/Broker/Monitor Admin

**Request Body:**
```json
{
  "authorization": "new-authorization-uuid"
}
```

**Request Fields:**

| Field | Type | Required | Description | Notes |
|-------|------|----------|-------------|-------|
| `authorization` | uuid | Yes | New authorization UUID | Changes token's permissions and scope |

### Delete Token
```http
DELETE /v1/authorizationtokens/{uuid}
```

**Required Roles:** TLC/Broker/Monitor Admin

**Response:** 204 No Content

**Note:** Immediately invalidates token for API access