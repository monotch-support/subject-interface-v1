# Quick Start Guide

Get up and running with the Subject Interface REST API in 15 minutes. This guide walks you through the essential steps to create your first streaming session.

## Prerequisites

- API endpoint URL
- Administrative authorization token for initial setup
- Basic understanding of REST API concepts

## Environment Variables

Set these environment variables to make the examples directly runnable:

```bash
# Base URL for the Subject Interface API
export STREAMING_API_BASEURL="https://localhost/api"

# Authorization token
export STREAMING_API_ADMIN_TOKEN="your-admin-token"

# Domain and configuration
export STREAMING_API_DOMAIN="dev_001"
export STREAMING_API_IDENTIFIER="sub00001"
export STREAMING_API_SECURITY_MODE="TLSv1.2"  # or "NONE"

# Variables populated from API responses (save these as you go)
export AUTH_UUID="..."          # From Step 2
export GENERATED_TOKEN="..."   # From Step 3
```

## 4-Step Implementation

### Step 1: Register a TLC (Subject)

First, register a TLC (data-producing entity) in your domain:

```bash
curl -X POST \
  ${STREAMING_API_BASEURL}/v1/tlcs \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "identifier": "'${STREAMING_API_IDENTIFIER}'"
  }'
```

**Response:**
```json
{
  "uuid": "550e8400-e29b-41d4-a716-446655440000",
  "identifier": "sub00001",
  "domain": "dev_001",
  "account": "your-account",
  "type": "TCPStreaming"
}
```

> **Note:** TLC identifiers must be exactly 8 lowercase alphanumeric characters, underscores, and hyphens.

### Step 2: Create an Authorization

Create an authorization for operational access:

```bash
curl -X POST \
  ${STREAMING_API_BASEURL}/v1/authorizations \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "role": "TLC_SYSTEM"
  }'
```

**Response:**
```json
{
  "uuid": "660f9500-f39c-52e5-b827-556766551111",
  "domain": "dev_001", 
  "account": "your-account",
  "role": "TLC_SYSTEM",
  "tlcIdentifiers": []
}
```

### Step 3: Generate an API Token

Generate a token for the authorization:

```bash
curl -X POST \
  ${STREAMING_API_BASEURL}/v1/authorizationtokens \
  -H "X-Authorization: ${STREAMING_API_ADMIN_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "authorization": "'${AUTH_UUID}'"
  }'
```

**Response:**
```json
{
  "uuid": "770fa600-04ad-63f6-c938-667877662222",
  "token": "dtNB_vhvJ0wgTGf1N0DxN38_AmTL_4yiPRZdqZSuK3k",
  "authorization": "660f9500-f39c-52e5-b827-556766551111"
}
```

> **Save this token** - export it as `GENERATED_TOKEN` for the next step.

### Step 4: Create a Streaming Session

Create a streaming session using your new token:

```bash
curl -X POST \
  ${STREAMING_API_BASEURL}/v1/sessions \
  -H "X-Authorization: ${GENERATED_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "domain": "'${STREAMING_API_DOMAIN}'",
    "type": "TLC",
    "protocol": "TCPStreaming_Singleplex",
    "details": {
      "securityMode": "'${STREAMING_API_SECURITY_MODE}'",
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


## Verification

Test your setup by listing your active sessions:

```bash
curl -X GET \
  ${STREAMING_API_BASEURL}/v1/sessions \
  -H "X-Authorization: ${GENERATED_TOKEN}"
```

You should see your active session in the response.

## What's Next?

- **TCP Streaming:** See [Code Examples](../../examples/index.md) for complete working implementations in multiple languages
- **Monitoring:** Request [session logs](../reference/endpoints-reference.md#session-logs) for troubleshooting

For complete reference documentation, see [API Endpoints Reference](../reference/endpoints-reference.md).