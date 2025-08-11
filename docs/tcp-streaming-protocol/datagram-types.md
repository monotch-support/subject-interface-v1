# Datagram Types

The TCP Streaming Protocol defines datagram types to support different communication requirements. Each datagram type serves specific purposes within the streaming protocol, from basic connection management to payload transmission and system monitoring.

## Datagram Structure Overview

All datagrams follow a consistent structure with frame headers and type-specific content.

### Frame Header Structure
```
+------------------+------------------+----------------------+
| Fixed Prefix     | Data Size        | Datagram Content     |
| 0xAA 0xBB        | 2 bytes          | Variable Size        |
| (big-endian)     | (big-endian)     | (Type + Data)        |
+------------------+------------------+----------------------+
```

### Datagram Type Identification

| Type | Hex Value | Name | Purpose |
|------|-----------|------|----------|
| KeepAlive | `0x00` | Keep connection alive | Prevents timeout during low activity |
| Token | `0x01` | Authentication token | Session authentication after connect |
| Bye | `0x02` | Disconnect message | Graceful connection termination |
| Reconnect | `0x03` | Reconnect instruction | Service-initiated reconnection |
| Payload (no ID) | `0x04` | Singleplex payload | Optimized for single TLC sessions |
| Payload (with ID) | `0x05` | Multiplex payload | Tagged for multiplex/broker sessions |
| Timestamps Request | `0x06` | Time sync request | Initiates clock synchronization |
| Timestamps Response | `0x07` | Time sync response | Returns timing information |
| Monitor Payload | `0xF0` | Enhanced monitoring | Payload with diagnostic metadata |

## Connection Management Datagrams

These datagrams handle connection lifecycle and maintenance operations.

### KeepAlive Datagram (0x00)

#### Purpose
Prevents connection timeouts during periods of low data activity.

#### Format
```
+---------------+
| Datagram Type |
| 0x00          |
+---------------+
```

| Characteristic     | Details                                                                                     |
| ------------------ | ------------------------------------------------------------------------------------------- |
| **Usage**          | Timeout prevention; bidirectional; minimal overhead; automatic generation                   |
| **Timeout Threshold** | Session details from [Create Session](../api-endpoints/reference/endpoints-reference.md#create-session) API response contain timeout threshold; send KeepAlive well before timeout if no other data transmitted |
| **Implementation** | Send before timeout thresholds; consider traffic patterns; balance frequency with bandwidth |

### Token Datagram (0x01)

#### Purpose
Provides session authentication immediately after connection establishment.

#### Format
```
+---------------+---------------------------+
| Datagram Type | Token (ASCII encoded)     |
| 0x01          | Variable Length           |
+---------------+---------------------------+
```

| Aspect       | Details                                                                                                   |
| ------------ | --------------------------------------------------------------------------------------------------------- |
| **Usage**    | First datagram after version; service validates against database; invalid tokens terminate connection     |
| **Security** | Sensitive credentials; transmit after TLS; validation failure = immediate termination; no retry mechanism |
| **Encoding** | ASCII character encoding                                                                                  |

#### Example
```
Token: dtNB_vhvJ0wgTGf1N0DxN38_AmTL_4yiPRZdqZSuK3k
Encoded as ASCII bytes in datagram
```

### Bye Datagram (0x02)

#### Purpose
Graceful connection termination with optional reason indication.

#### Format
```
+---------------+---------------------------------------+
| Datagram Type | Disconnect Reason (ASCII, optional)   |
| 0x02          | Variable Length                       |
+---------------+---------------------------------------+
```

| Aspect             | Details                                                                                            |
| ------------------ | -------------------------------------------------------------------------------------------------- |
| **Usage**          | Graceful termination; bidirectional; final datagram; optional reason provides diagnostics          |
| **Common Reasons** | Normal shutdown, system maintenance, authentication failure, protocol violation, time sync failure |

### Reconnect Datagram (0x03)

#### Purpose
Instructs client to reconnect as soon as possible.

#### Format
```
+---------------+
| Datagram Type |
| 0x03          |
+---------------+
```

#### Usage Characteristics
- **Service-Initiated** - Only sent by Streaming Service
- **Immediate Action** - Client should reconnect ASAP
- **Session Recreation** - New session must be created via API
- **Load Balancing** - Enables maintenance and redistribution

#### Client Response Requirements
1. Close existing connection immediately
2. Create new session using JSON-REST API
3. Establish new TCP connection (possibly different node)
4. Resume operations with new session

## Payload Datagrams

These datagrams carry actual data between C-ITS participants.

### Payload without TLC Identifier (0x04)

#### Purpose
Optimized payload transmission for Singleplex mode operations.

#### Format
```
+---------------+---------------+------------------+-------------+
| Datagram Type | Payload Type  | Origin Timestamp | Payload     |
| 0x04          | 1 byte        | 8 bytes          | Variable    |
+---------------+---------------+------------------+-------------+
```

#### Field Specifications

| Field                | Size     | Description                                                                                                                                         |
| -------------------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Payload Type**     | 1 byte   | Identifies payload content type; application-specific values; no protocol interpretation; used for routing and processing decisions                 |
| **Origin Timestamp** | 8 bytes  | UTC milliseconds since epoch; big-endian 64-bit integer; payload creation time (not transmission time); must use same clock as time synchronization |
| **Payload**          | Variable | Actual message content; maximum 65535 bytes total frame size; no protocol-level interpretation; binary or text data supported                       |

#### Usage Restrictions
- **Singleplex Only** - Cannot be used in multiplex sessions
- **Protocol Enforcement** - Wrong usage terminates session
- **Implicit Identifier** - TLC ID from session configuration

#### Example Payload Types
- `0x01` - Traffic signal state
- `0x02` - Detector data
- `0x03` - Emergency vehicle detection
- `0xFF` - Custom application data

### Payload with TLC Identifier (0x05)

#### Purpose
Identifier-tagged payload transmission for multiplex and broker operations.

#### Format
```
+---------------+----------------+---------------+------------------+-------------+
| Datagram Type | TLC Identifier | Payload Type  | Origin Timestamp | Payload     |
| 0x05          | 8 bytes        | 1 byte        | 8 bytes          | Variable    |
+---------------+----------------+---------------+------------------+-------------+
```

#### Field Specifications

| Field                | Size     | Description                                                                                                                         |
| -------------------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| **TLC Identifier**   | 8 bytes  | ASCII-encoded identifier; specifies payload source/destination; must match session scope; padded with nulls if shorter than 8 chars |
| **Payload Type**     | 1 byte   | Same as type 0x04: identifies payload content type; application-specific values; no protocol interpretation                         |
| **Origin Timestamp** | 8 bytes  | Same as type 0x04: UTC milliseconds since epoch; big-endian 64-bit integer; payload creation time                                   |
| **Payload**          | Variable | Same as type 0x04: actual message content; maximum total frame size; no protocol-level interpretation                               |

#### Usage Requirements
- **Multiplex and Broker Only** - Required for these session types
- **Identifier Validation** - Must match session scope
- **Explicit Routing** - Enables routing within multiplex sessions
- **Scope Enforcement** - Invalid identifiers dropped

#### Identifier Management
- ASCII encoding required
- Maximum 8 bytes length
- Case sensitive
- Null padding for shorter identifiers

## Time Synchronization Datagrams

These datagrams implement the mandatory time synchronization system.

### Timestamps Request (0x06)

#### Purpose
Initiates time synchronization measurement for clock difference detection.

#### Format
```
+---------------+---------------------------+
| Datagram Type | t0: Transmission Timestamp|
| 0x06          | 8 bytes                   |
+---------------+---------------------------+
```

#### Usage Characteristics
- **Service-Initiated** - Sent by Streaming Service only
- **Regular Interval** - Configurable interval determined by Streaming Service deployment
- **Clock Measurement** - Enables roundtrip and drift calculation
- **Mandatory Response** - Client must respond immediately

#### Field Specifications

| Field                          | Size    | Description                                                                                                                                         |
| ------------------------------ | ------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| **t0: Transmission Timestamp** | 8 bytes | UTC milliseconds when request sent; big-endian 64-bit integer; obtained as close to transmission as possible; used for clock difference calculation |

### Timestamps Response (0x07)

#### Purpose
Responds to time synchronization requests with precise timing information.

#### Format
```
+---------------+-------------+-------------+-------------+
| Datagram Type | t0: Original| t1: Receipt | t2: Response|
| 0x07          | Timestamp   | Timestamp   | Timestamp   |
|               | 8 bytes     | 8 bytes     | 8 bytes     |
+---------------+-------------+-------------+-------------+
```

#### Field Specifications

| Field                       | Size    | Description                                                                                                        |
| --------------------------- | ------- | ------------------------------------------------------------------------------------------------------------------ |
| **t0: Original Timestamp**  | 8 bytes | Copy from the request datagram; unchanged from received value; enables service to match request/response           |
| **t1: Reception Timestamp** | 8 bytes | When request was received; determined as early as possible upon reception; same clock source as payload timestamps |
| **t2: Response Timestamp**  | 8 bytes | When response is sent; determined as late as possible in transmission; immediate priority over other traffic       |

#### Processing Requirements
- **Priority Handling** - Prioritize over other traffic
- **Minimal Delay** - Send immediately after request
- **Accurate Timestamps** - Use consistent clock source
- **No Queuing** - Process immediately

#### Clock Difference Calculation
Service calculates using all four timestamps:

- **Roundtrip Time:** `(t3 - t0) - (t2 - t1)`
- **Clock Difference:** `((t1 - t0) + (t2 - t3)) / 2`
- Where t3 is response reception time at service

## Special Monitoring Payloads

### Monitor Payload Encoding (0xF0)

#### Purpose
Enhanced payload format with diagnostic metadata for monitoring and analytics.

#### Format
```
+---------------------+-----------+----------+----------+----------+----------+
| Publisher Token Len | Publisher | Publish  | Sent     | Original | Original |
| 4 bytes             | Token     | Time     | Time     | Type     | Payload  |
|                     | Variable  | 8 bytes  | 8 bytes  | 1 byte   | Variable |
+---------------------+-----------+----------+----------+----------+----------+
```

#### Field Specifications

| Field                      | Size     | Description                                                                                     |
| -------------------------- | -------- | ----------------------------------------------------------------------------------------------- |
| **Publisher Token Length** | 4 bytes  | Big-endian 32-bit integer; length of publisher token field; can be zero for resent payloads     |
| **Publisher Token**        | Variable | ASCII-encoded session token; identifies original publisher; empty for system-generated payloads |
| **Publishing Timestamp**   | 8 bytes  | When payload was originally published; UTC milliseconds; from original publisher                |
| **Sent Timestamp**         | 8 bytes  | When service sent to monitor; UTC milliseconds; service clock time                              |
| **Original Type**          | 1 byte   | Original payload type before encapsulation; either 0x04 or 0x05                                 |
| **Original Payload**       | Variable | Complete original payload content; unchanged from source                                        |

#### Usage Characteristics
- **Monitor Sessions Only** - Primarily for monitoring
- **Diagnostic Enhancement** - Additional metadata
- **Publisher Tracking** - Source identification
- **Audit Trail** - Complete payload history

## Implementation Guidelines

| Category           | Guidelines                                                                                   |
| ------------------ | -------------------------------------------------------------------------------------------- |
| **Processing**     | Validate type first; check frame size; handle unknown types gracefully; log violations       |
| **Error Handling** | Discard invalid frames; log unknown types; size mismatches terminate connection              |
| **Performance**    | Optimize for payload datagrams; pre-allocate buffers; minimize copies; efficient timestamps  |
| **Security**       | Validate input lengths; sanitize content; protect against overflows; log suspicious patterns |