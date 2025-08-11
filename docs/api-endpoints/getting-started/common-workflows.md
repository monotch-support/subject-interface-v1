# Common Workflows

Task-oriented guides for the most frequent Subject Interface operations. Each workflow provides step-by-step instructions with examples.

## Environment Variables

Set these environment variables to make the examples directly runnable:

```bash
# Base URL for the Subject Interface API
export STREAMING_API_BASEURL="https://localhost/api"

# Authorization token
export STREAMING_API_ADMIN_TOKEN="your-admin-token"

# Domain and security configuration
export STREAMING_API_DOMAIN="dev_001"
export STREAMING_API_SECURITY_MODE="TLSv1.2"  # or "NONE"
export STREAMING_API_IDENTIFIER="sub00001"

# Variables populated from API responses
export TLC_UUID="..."          # Returned from TLC registration
export AUTH_UUID="..."          # Returned from authorization creation
export SESSION_TOKEN="..."      # Returned from session creation
export TOKEN_UUID="..."         # Returned from token generation
```

## TLC/Subject Management

### Register a New TLC (Subject)

**When:** Setting up a new data-producing entity (traffic controller, sensor, etc.)

**Steps:**

- Choose an 8-character identifier (lowercase alphanumeric, underscores, hyphens)
- Send registration request

```bash
curl -X POST ${STREAMING_API_BASEURL}/v1/tlcs \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"identifier": "sub00001"}'
```

- Save the returned UUID for future reference

### Remove a TLC

**When:** Decommissioning equipment or cleaning up unused registrations

**Steps:**

- List TLCs to find the target UUID
- Delete the TLC (terminates any active sessions)

```bash
curl -X DELETE ${STREAMING_API_BASEURL}/v1/tlcs/${TLC_UUID} \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

## Access Control Management

### Set Up User Access

**When:** Granting API access to team members or systems

**Steps:**

- Create authorization with appropriate role

```bash
curl -X POST ${STREAMING_API_BASEURL}/v1/authorizations \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "role": "TLC_ANALYST",
    "tlcIdentifiers": ["sub00001", "sub00002"]
  }'
```

- Generate API token for the authorization

```bash
curl -X POST ${STREAMING_API_BASEURL}/v1/authorizationtokens \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"authorization": "${AUTH_UUID}"}'
```

- Securely share the token with the user

### Grant Domain-Wide Access

**When:** Setting up admin users or system accounts

**Steps:**

- Create authorization without TLC scope restriction

```bash
curl -X POST ${STREAMING_API_BASEURL}/v1/authorizations \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"role": "BROKER_SYSTEM"}'
```

- Generate token as above

### Revoke User Access

**When:** Employee departure, security incident, or access cleanup

**Steps:**

- List authorization tokens to find target

```bash
curl -X GET ${STREAMING_API_BASEURL}/v1/authorizationtokens \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

- Revoke specific tokens

```bash
curl -X DELETE ${STREAMING_API_BASEURL}/v1/authorizationtokens/${TOKEN_UUID} \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

- (Optional) Delete the authorization entirely

```bash
curl -X DELETE ${STREAMING_API_BASEURL}/v1/authorizations/${AUTH_UUID} \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

## Session Management

### Create a Simple Session

**When:** Setting up single TLC data streaming

**Steps:**

- Ensure TLC is registered
- Create singleplex session

```bash
curl -X POST ${STREAMING_API_BASEURL}/v1/sessions \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "domain": "'${STREAMING_API_DOMAIN}'",
    "type": "TLC",
    "protocol": "TCPStreaming_Singleplex", 
    "details": {
      "securityMode": "'${STREAMING_API_SECURITY_MODE:-TLSv1.2}'",
      "tlcIdentifier": "'${STREAMING_API_IDENTIFIER}'"
    }
  }'
```

**Response:**
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

- Connect to streaming endpoint using returned connection details

### Create Multi-TLC Session

**When:** Aggregating data from multiple sources

**Step:**

Create multiplex session with TLC list

```bash
curl -X POST ${STREAMING_API_BASEURL}/v1/sessions \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "domain": "'${STREAMING_API_DOMAIN}'",
    "type": "Broker",
    "protocol": "TCPStreaming_Multiplex",
    "details": {
      "securityMode": "'${STREAMING_API_SECURITY_MODE:-TLSv1.2}'",
      "tlcIdentifiers": ["sub00001", "sub00002", "sub00003"]
    }
  }'
```

**Response:**
```json
{
  "domain": "dev_001",
  "type": "Broker", 
  "token": "session-token-xyz789",
  "protocol": "TCPStreaming_Multiplex",
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
      "port": 8444,
      "expiration": "2024-01-15T10:30:00Z"
    },
    "tlcIdentifiers": ["sub00001", "sub00002", "sub00003"]
  }
}
```

### Update Session Scope

**When:** Adding/removing TLCs from active multiplex session

**Step:**

Update session with new TLC list

```bash
curl -X PUT ${STREAMING_API_BASEURL}/v1/sessions/${SESSION_TOKEN} \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "securityMode": "'${STREAMING_API_SECURITY_MODE:-TLSv1.2}'",
    "tlcIdentifiers": ["sub00001", "sub00002", "sub00003", "sub00004"]
  }'
```

### Terminate Session

**When:** Ending data streaming or emergency shutdown

**Step:**

Delete the session

```bash
curl -X DELETE ${STREAMING_API_BASEURL}/v1/sessions/${SESSION_TOKEN} \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

## Token Management

### Rotate API Tokens

**When:** Regular security maintenance or suspected compromise

**Steps:**

- Create new token for same authorization

```bash
curl -X POST ${STREAMING_API_BASEURL}/v1/authorizationtokens \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"authorization": "${AUTH_UUID}"}'
```

- Update systems to use new token
- Test new token functionality
- Revoke old token

```bash
curl -X DELETE ${STREAMING_API_BASEURL}/v1/authorizationtokens/${OLD_TOKEN_UUID} \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

### Emergency Token Revocation

**When:** Security incident or compromised credentials

**Steps:**

- Immediately revoke compromised token

```bash
curl -X DELETE ${STREAMING_API_BASEURL}/v1/authorizationtokens/${COMPROMISED_TOKEN_UUID} \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

- Create replacement token if needed
- Update affected systems
- Review access logs for suspicious activity

## Monitoring and Troubleshooting

### Check Session Status

**When:** Verifying active connections or debugging issues

**Steps:**

- List active sessions

```bash
curl -X GET ${STREAMING_API_BASEURL}/v1/sessions \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

- Get specific session details

```bash
curl -X GET ${STREAMING_API_BASEURL}/v1/sessions/${SESSION_TOKEN} \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

### Review Session History

**When:** Troubleshooting connection issues or analyzing usage patterns

**Steps:**

- Query session logs with time range

```bash
curl -X GET "${STREAMING_API_BASEURL}/v1/sessionlogs?from=2024-01-15T00:00:00Z&until=2024-01-15T23:59:59Z" \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

- Get specific session log details

```bash
curl -X GET ${STREAMING_API_BASEURL}/v1/sessionlogs/${SESSION_TOKEN} \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

### Debug Authentication Issues

**When:** Receiving 401/403 errors

**Steps:**

- Verify token is active

```bash
curl -X GET ${STREAMING_API_BASEURL}/v1/authorizationtokens \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

- Check authorization permissions

```bash
curl -X GET ${STREAMING_API_BASEURL}/v1/authorizations/${AUTH_UUID} \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

- Test token with simple endpoint

```bash
curl -X GET ${STREAMING_API_BASEURL}/v1/tlcs \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}"
```

## Workflow Quick Reference

| Task | Primary Endpoint | Required Role |
|------|------------------|---------------|
| Register TLC | `POST /v1/tlcs` | TLC_ADMIN |
| Create user access | `POST /v1/authorizations` | TLC/BROKER/MONITOR_ADMIN |
| Generate token | `POST /v1/authorizationtokens` | *_ADMIN |
| Start streaming | `POST /v1/sessions` | *_ADMIN/*_SYSTEM |
| Monitor sessions | `GET /v1/sessionlogs` | *_ADMIN/*_ANALYST |

For detailed parameter reference, see [Endpoints Reference](../reference/endpoints-reference.md).