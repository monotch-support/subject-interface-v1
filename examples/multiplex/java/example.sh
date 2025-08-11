#!/bin/bash

export STREAMING_API_BASEURL="https://localhost/api"
export STREAMING_API_TLC_TOKEN="your-tlc-auth-token"
export STREAMING_API_BROKER_TOKEN="your-broker-auth-token"
export STREAMING_API_DOMAIN="dev_001"
export STREAMING_API_IDENTIFIER="sub00001"
export STREAMING_API_SECURITY_MODE="TLSv1.2"

# Build and run Docker container
docker build -t subject-interface-multiplex-example .

docker run --rm -it \
  -e STREAMING_API_BASEURL="$STREAMING_API_BASEURL" \
  -e STREAMING_API_TLC_TOKEN="$STREAMING_API_TLC_TOKEN" \
  -e STREAMING_API_BROKER_TOKEN="$STREAMING_API_BROKER_TOKEN" \
  -e STREAMING_API_DOMAIN="$STREAMING_API_DOMAIN" \
  -e STREAMING_API_IDENTIFIER="$STREAMING_API_IDENTIFIER" \
  -e STREAMING_API_SECURITY_MODE="$STREAMING_API_SECURITY_MODE" \
  subject-interface-multiplex-example