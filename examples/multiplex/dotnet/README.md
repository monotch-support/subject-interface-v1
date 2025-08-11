# Subject Interface v1 .NET Multiplex Example

This .NET example demonstrates multiplexed TCP streaming with the Subject Interface v1, running concurrent producer (TLC) and consumer (Broker) sessions. The example showcases the multiplex protocol capabilities by establishing two simultaneous connections that exchange data with timestamping and keepalive mechanisms.

## Prerequisites

### General Requirements

- Valid authorization token for the Subject Interface v1 API for the given domain and subject (TLC)
- Network connectivity to both the REST API and TCP streaming endpoints

### .NET Requirements

- .NET 8.0 or later
- NuGet dependencies: `Newtonsoft.Json` (see MultiplexExample.csproj)

## Environment Variables

Configure the example using these environment variables:

| Variable                      | Description                                            | Example                      |
| ----------------------------- | ------------------------------------------------------ | ---------------------------- |
| `STREAMING_API_BASEURL`       | Base URL of the Subject Interface API                  | `https://localhost/api` |
| `STREAMING_API_TLC_TOKEN`     | Authorization token for API access as TLC              | `your-tlc-auth-token`       |
| `STREAMING_API_BROKER_TOKEN`  | Authorization token for API access as Broker           | `your-broker-auth-token`       |
| `STREAMING_API_DOMAIN`        | Domain for the sessions to create                      | `dev_001`        |
| `STREAMING_API_SECURITY_MODE` | Security mode for TCP connection (`NONE` or `TLSv1.2`) | `TLSv1.2`                       |
| `STREAMING_API_IDENTIFIER`    | TLC identifier for payload messages                    | `sub00001`                     |

## Running the Example

### Using docker

```bash
# Build the Docker image
docker build -t subject-interface-multiplex-example-dotnet .

# Run the example
docker run --rm \
  -e STREAMING_API_BASEURL=https://localhost/api \
  -e STREAMING_API_TLC_TOKEN=your-tlc-auth-token \
  -e STREAMING_API_BROKER_TOKEN=your-broker-auth-token \
  -e STREAMING_API_DOMAIN=dev_001 \
  -e STREAMING_API_SECURITY_MODE=TLSv1.2 \
  -e STREAMING_API_IDENTIFIER=sub00001 \
  subject-interface-multiplex-example-dotnet
```

### Using the run script (still uses docker)

```bash
./example.sh
```

## What the Example Does

1. **Creates two concurrent sessions**: One TLC (producer) and one Broker (consumer) session using the Subject Interface v1 REST API
2. **Establishes multiplex TCP connections**: Connects to TCP streaming endpoints with optional TLS encryption using .NET's SslStream
3. **Performs protocol handshake**: Exchanges protocol version and authentication tokens
4. **Producer task (TLC)**: Sends random payload data with identifier every second using multiplex datagram format
5. **Consumer task (Broker)**: Listens for incoming messages and handles various datagram types
6. **Maintains connections**: Sends periodic keepalive messages and responds to timestamp requests
7. **Handles multiplex protocol**: Processes framed datagrams with proper header validation and type handling using C# switch expressions

## Troubleshooting

### Common Issues

1. **Authentication Errors (401)**
   - Verify your `STREAMING_API_TLC_TOKEN` and `STREAMING_API_BROKER_TOKEN` are valid and have the necessary permissions

2. **Connection Errors**
   - Verify `STREAMING_API_BASEURL` is correct and accessible
   - Check network connectivity to the Subject Interface instance
   - Ensure firewall rules allow access to both REST API and TCP streaming ports

3. **Session Creation Failures**
   - Verify `STREAMING_API_DOMAIN` is valid for your environment
   - Verify `STREAMING_API_IDENTIFIER` is valid for your environment