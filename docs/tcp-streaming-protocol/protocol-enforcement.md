# Protocol Enforcement

TCP Streaming Protocol sessions operate under strict enforcement policies to ensure optimal performance, resource management, and system stability. When sessions are created, the server provides enforcement parameters that clients **must respect**. Violations result in immediate connection termination without warning.

!!! warning "Critical Implementation Requirement"
    All enforcement limits are **mandatory**. Clients exceeding any threshold will have their connection terminated immediately. There are no warnings or grace periods.

## Enforcement Overview

All enforcement violations result in immediate connection termination without warning.

| Enforcement Type | Purpose | Mechanism | Example |
|------------------|---------|-----------|---------|
| **Payload Rate Limiting** | Prevent message flooding | Sliding window average of payload messages per second | Max 12 messages/second over 5s window |
| **Throughput Limiting** | Manage bandwidth consumption | Sliding window average of payload bytes per second | Max 60 bytes/second over 5s window |
| **Connection Timeout** | Detect connectivity issues | Maximum idle time without receiving any datagrams | Disconnect after 10s of no activity |
| **Clock Synchronization** | Time accuracy requirements | Sliding window average of absolute clock differences | Max 3s average deviation over 1m window |

## Payload Rate Limiting

### Mechanism

Payload rate limiting controls the frequency of payload messages (datagram types 0x04 and 0x05) that clients can transmit.

| Parameter | Description | Source |
|-----------|-------------|---------|
| **PayloadRateLimit** | Maximum payload messages per second | Session response |
| **PayloadRateLimitDuration** | Sliding window duration for rate calculation | Session response |

### Enforcement Algorithm

```
Current Rate = Total Payload Messages / PayloadRateLimitDuration (sliding window)

IF Current Rate > PayloadRateLimit THEN
    Terminate Connection Immediately
END IF
```

### Implementation Details

| Aspect | Details |
|--------|---------|
| **Calculation Method** | Sliding window average over specified duration |
| **Message Types** | Payload datagrams only (0x04, 0x05); excludes keep-alive, time sync |
| **Window Behavior** | Continuous sliding window; not fixed time buckets |
| **Precision** | Sub-second precision; calculated per datagram transmission |

### Example Scenario

```
PayloadRateLimit: 12 messages/second
PayloadRateLimitDuration: PT5S (5 seconds)

Sliding Window Enforcement:
- At time T: Client has sent 60 messages in past 5 seconds
- Current rate: 60 messages ÷ 5 seconds = 12 messages/second ✓ ALLOWED
- At time T+1: Client sends 1 more message (61 total in 5-second window)  
- New rate: 61 messages ÷ 5 seconds = 12.2 messages/second ✗ VIOLATION
- Result: Connection terminated immediately
```

## Throughput Limiting

### Mechanism

Throughput limiting controls the bandwidth consumption of payload data transmitted by clients.

| Parameter | Description | Source |
|-----------|-------------|---------|
| **PayloadThroughputLimit** | Maximum payload bytes per second | Session response |
| **PayloadThroughputLimitDuration** | Sliding window duration for throughput calculation | Session response |

### Enforcement Algorithm

```
Current Throughput = Total Payload Bytes / PayloadThroughputLimitDuration (sliding window)

IF Current Throughput > PayloadThroughputLimit THEN
    Terminate Connection Immediately  
END IF
```

### Implementation Details

| Aspect | Details |
|--------|---------|
| **Byte Calculation** | Payload data only; excludes frame headers, datagram type bytes |
| **Message Types** | Payload datagrams only (0x04, 0x05); excludes protocol overhead |
| **Window Behavior** | Continuous sliding window; recalculated per transmission |
| **Precision** | Byte-level accuracy; real-time monitoring |

### Example Scenario

```
PayloadThroughputLimit: 60 bytes/second
PayloadThroughputLimitDuration: PT5S (5 seconds)

Sliding Window Enforcement:
- At time T: Client has sent 300 bytes in past 5 seconds
- Current rate: 300 bytes ÷ 5 seconds = 60 bytes/second ✓ ALLOWED
- At time T+1: Client sends 10-byte message (310 total in 5-second window)
- New rate: 310 bytes ÷ 5 seconds = 62 bytes/second ✗ VIOLATION  
- Result: Connection terminated immediately
```

## Connection Timeout Enforcement

### Mechanism

Connection timeout enforcement automatically terminates idle connections to conserve server resources.

| Parameter | Description | Source |
|-----------|-------------|---------|
| **KeepAliveTimeout** | Maximum duration without receiving any datagrams | Session response |

### Enforcement Algorithm

```
Time Since Last Datagram = Current Time - Last Received Datagram Time

IF Time Since Last Datagram > KeepAliveTimeout THEN
    Terminate Connection Immediately
END IF
```

### Implementation Details

| Aspect | Details |
|--------|---------|
| **Monitoring Scope** | Any datagram type; includes keep-alive, payloads, time sync responses |
| **Timer Reset** | Every received datagram resets the timeout timer |
| **Precision** | Sub-second precision; continuous monitoring |
| **Client Responsibility** | Send keep-alive datagrams (0x00) to prevent timeout |

### Example Scenario

```
KeepAliveTimeout: PT10S (10 seconds)

Timeline:
- T+0s: Client sends payload datagram (timer resets)
- T+5s: No activity 
- T+10s: Timeout threshold reached
- T+10.001s: Connection terminated by server
- T+15s: Client attempts to send data → connection already closed

Prevention:
- T+8s: Client sends keep-alive datagram (0x00) → timer resets
- T+18s: Still connected, no timeout
```

## Clock Synchronization Enforcement

### Mechanism

Clock synchronization enforcement ensures time accuracy by monitoring clock differences between client and server.

| Parameter | Description | Source |
|-----------|-------------|---------|
| **ClockDiffLimit** | Maximum allowed average absolute clock deviation | Session response |
| **ClockDiffLimitDuration** | Sliding window for calculating clock difference average | Session response |

### Enforcement Algorithm

```
FOR each TimeSync measurement:
    Clock Difference = |Client Clock - Server Clock|
    Add to sliding window over ClockDiffLimitDuration

Average Clock Diff = Average(All measurements in sliding window)

IF Average Clock Diff > ClockDiffLimit THEN
    Terminate Connection Immediately
END IF
```

### Implementation Details

| Aspect | Details |
|--------|---------|
| **Measurement Trigger** | Server-initiated TimeSync requests (0x06) |
| **Calculation Method** | Absolute difference; sliding window average |
| **Window Behavior** | All measurements within duration window |
| **Client Requirement** | Must respond to TimeSync requests with accurate timestamps |

### Example Scenario

```
ClockDiffLimit: PT3S (3 seconds average deviation)
ClockDiffLimitDuration: PT1M (1 minute sliding window)

Measurements over 1 minute:
- Measurement 1: 2s clock diff ✓
- Measurement 2: 1s clock diff ✓  
- Measurement 3: 5s clock diff ✓ (individual high, but average still OK)
- Measurement 4: 4s clock diff
- Average: (2+1+5+4)/4 = 3s ✓ ALLOWED

- Measurement 5: 6s clock diff  
- New Average: (1+5+4+6)/4 = 4s ✗ VIOLATION
- Result: Connection terminated immediately
```

## Sliding Window Implementation

### Algorithm Characteristics

All enforcement mechanisms except connection timeout use sliding window algorithms for fair and accurate monitoring.

| Aspect | Implementation |
|--------|----------------|
| **Window Type** | Continuous sliding window (not fixed buckets) |
| **Update Frequency** | Recalculated on every relevant event |
| **Data Retention** | All events within window duration |
| **Calculation** | Real-time average of events in current window |



## Implementation Guidelines

### Client Design Patterns

| Pattern | Purpose | Implementation |
|---------|---------|----------------|
| **Rate Governor** | Prevent payload rate violations | Queue messages; enforce transmission rate; respect limits |
| **Throughput Monitor** | Track bandwidth usage | Monitor payload bytes; implement backpressure; throttle transmission |
| **Keep-Alive Manager** | Prevent connection timeouts | Send periodic keep-alive datagrams; track last activity |
| **Time Sync Handler** | Maintain clock synchronization | Respond to TimeSync requests; maintain accurate clocks |



For session creation and parameter retrieval, see [API Endpoints Reference](../api-endpoints/reference/endpoints-reference.md#create-session).