# Multiplex TCP Streaming Example

This example demonstrates the complete Subject Interface v1 workflow, showcasing both the JSON-REST API layer for session management and the TCP Streaming Protocol layer for real-time data exchange. The implementation illustrates how clients interact with the dual-layer architecture to establish streaming sessions and exchange C-ITS messages.

!!! note "TLC Terminology"
    The interface originally served Traffic Light Controllers (TLCs), and this legacy terminology persists in the API specifications for backward compatibility. "TLC" now refers to any connected entity: traffic controllers, sensors, barriers, displays, or any C-ITS participant. See [Terminology Reference](appendix/evolution-terminology.md) for the mapping between legacy and generic terminology.

The example runs two concurrent sessions using the multiplex protocol:

- A **TLC session** representing a connected subject that produces data
- A **Broker session** acting as a data distribution hub that consumes and routes messages

While this example shows a simplified scenario with the TLC session sending data and the Broker session receiving it, the Subject Interface supports full bi-directional communication. TLC sessions can receive data from Broker sessions, and Broker sessions can send data to TLC sessions.

The implementation demonstrates the essential workflow: authenticating with the REST API, creating streaming sessions, establishing TCP connections to the Streaming Service Nodes, and exchanging framed messages using the protocol's datagram format.

!!! warning "Important Disclaimer"
    This example is for educational purposes only and is not production-ready. It demonstrates the basic workflow and protocol interaction but lacks the error handling, reconnection logic, monitoring, and robustness required for production deployments. Use this as a learning resource and starting point for understanding the Subject Interface v1.

## Prerequisites

### General Requirements

- Valid authorization tokens for Subject Interface v1 API access
- Access to a Subject Interface v1 instance
- Network connectivity to both REST API and TCP streaming endpoints

### Language-Specific Requirements

=== "Python"

    - Python 3.10 or later (uses match/case statements)
    - `requests` library for HTTP API calls
    - Standard library modules (socket, ssl, struct, threading, json, logging)

=== "Go"

    - Go 1.21 or later
    - Standard library modules (net/http, crypto/tls, encoding/binary, sync)

=== "Java"

    - Java 11 or later
    - org.json library (automatically downloaded in Docker build)
    - Standard library modules (java.net, javax.net.ssl, java.util.concurrent)

=== "Rust"

    - Rust 1.83 or later
    - Cargo dependencies: reqwest, serde, tokio, rustls, rand

=== ".NET"

    - .NET 8.0 or later
    - NuGet dependencies: Newtonsoft.Json

## Configuration by Environment

The example uses environment variables for configuration:

| Variable                      | Description                            | Example                                     |
| ----------------------------- | -------------------------------------- | ------------------------------------------- |
| `STREAMING_API_BASEURL`       | Base URL of the Subject Interface API  | `https://localhost/api` |
| `STREAMING_API_TLC_TOKEN`     | Authorization token for TLC session    | `your-tlc-auth-token`                            |
| `STREAMING_API_BROKER_TOKEN`  | Authorization token for Broker session | `your-broker-auth-token`                         |
| `STREAMING_API_DOMAIN`        | Domain for session creation            | `dev_001`                                   |
| `STREAMING_API_SECURITY_MODE` | Security mode (`NONE` or `TLSv1.2`)    | `TLSv1.2`                                   |
| `STREAMING_API_IDENTIFIER`    | TLC identifier for payload messages    | `sub00001`                                  |

=== "Python"

    ```python
    {% include-markdown "../../examples/multiplex/python/main.py" start="# ======== CONFIGURATION ========" end="# ======== UTILITY FUNCTIONS ========" %}
    ```

=== "Go"

    ```go
    {% include-markdown "../../examples/multiplex/go/main.go" start="// ======== CONFIGURATION ========" end="// ======== UTILITY FUNCTIONS ========" %}
    ```

=== "Java"

    ```java
    {% include-markdown "../../examples/multiplex/java/Main.java" start="    // ======== CONFIGURATION ========" end="    // ======== UTILITY FUNCTIONS ========" %}
    ```

=== "Rust"

    ```rust
    {% include-markdown "../../examples/multiplex/rust/main.rs" start="// ======== CONFIGURATION ========" end="// ======== UTILITY FUNCTIONS ========" %}
    ```

=== ".NET"

    ```csharp
    {% include-markdown "../../examples/multiplex/dotnet/Program.cs" start="// ======== CONFIGURATION ========" end="// ======== UTILITY FUNCTIONS ========" %}
    ```

## REST API Client Functions

The REST API client creates sessions for both TLC and Broker types:

=== "Python"

    ```python
    {% include-markdown "../../examples/multiplex/python/main.py" dedent=true start="# ======== REST API FUNCTIONS ========" end="# ======== TCP STREAMING FUNCTIONS ========" %}
    ```

=== "Go"

    ```go
    {% include-markdown "../../examples/multiplex/go/main.go" dedent=true start="// ======== REST API FUNCTIONS ========" end="// ======== TCP STREAMING FUNCTIONS ========" %}
    ```

=== "Java"

    ```java
    {% include-markdown "../../examples/multiplex/java/Main.java" dedent=true start="    // ======== REST API FUNCTIONS ========" end="    // ======== TCP STREAMING FUNCTIONS ========" %}
    ```

=== "Rust"

    ```rust
    {% include-markdown "../../examples/multiplex/rust/main.rs" dedent=true start="// ======== REST API FUNCTIONS ========" end="// ======== TCP STREAMING FUNCTIONS ========" %}
    ```

=== ".NET"

    ```csharp
    {% include-markdown "../../examples/multiplex/dotnet/Program.cs" dedent=true start="// ======== REST API FUNCTIONS ========" end="// ======== TCP STREAMING FUNCTIONS ========" %}
    ```

!!! note "Explanation"
    === "Python"
        The `create_session` function creates either a TLC or Broker session with multiplex protocol support. It returns the TCP streaming endpoint details (host, port) and session token for authentication.

    === "Go"
        The `createSession` function creates either a TLC or Broker session with multiplex protocol support. It returns the TCP streaming endpoint details (host, port) and session token for authentication.

    === "Java"
        The `createSession` function creates either a TLC or Broker session with multiplex protocol support. It returns the TCP streaming endpoint details (host, port) and session token for authentication.

    === "Rust"
        The `create_session` function creates either a TLC or Broker session with multiplex protocol support. It returns the TCP streaming endpoint details (host, port) and session token for authentication.

    === ".NET"
        The `CreateSessionAsync` function creates either a TLC or Broker session with multiplex protocol support. It returns the TCP streaming endpoint details (host, port) and session token for authentication.

## TCP Streaming Client Functions

The TCP streaming client handles the multiplex protocol communication:

=== "Python"

    ```python
    {% include-markdown "../../examples/multiplex/python/main.py" dedent=true start="# ======== TCP STREAMING FUNCTIONS ========" end="# ======== PRODUCER ========" %}
    ```

=== "Go"

    ```go
    {% include-markdown "../../examples/multiplex/go/main.go" dedent=true start="// ======== TCP STREAMING FUNCTIONS ========" end="// ======== PRODUCER ========" %}
    ```

=== "Java"

    ```java
    {% include-markdown "../../examples/multiplex/java/Main.java" dedent=true start="    // ======== TCP STREAMING FUNCTIONS ========" end="    // ======== PRODUCER ========" %}
    ```

=== "Rust"

    ```rust
    {% include-markdown "../../examples/multiplex/rust/main.rs" dedent=true start="// ======== TCP STREAMING FUNCTIONS ========" end="// ======== PRODUCER ========" %}
    ```

=== ".NET"

    ```csharp
    {% include-markdown "../../examples/multiplex/dotnet/Program.cs" dedent=true start="// ======== TCP STREAMING FUNCTIONS ========" end="// ======== PRODUCER ========" %}
    ```

!!! note "Explanation"
    === "Python"
        These functions implement the multiplex protocol:

        - `connect`: Establishes TCP connection with optional TLS
        - `handshake`: Exchanges protocol version
        - `write_datagram`: Sends framed messages with header prefix
        - `read_datagram`: Receives and validates framed messages (non-blocking)
        - `handle_datagram`: Processes different datagram types (keepalive, payload, timestamp, bye)
        - `run_streaming_client`: Main client loop handling connection lifecycle

    === "Go"
        These functions implement the multiplex protocol:

        - `connect`: Establishes TCP connection with optional TLS
        - `handshake`: Exchanges protocol version
        - `writeDatagram`: Sends framed messages with header prefix
        - `readDatagram`: Receives and validates framed messages (non-blocking)
        - `handleDatagram`: Processes different datagram types (keepalive, payload, timestamp, bye)
        - `runStreamingClient`: Main client loop handling connection lifecycle

    === "Java"
        These functions implement the multiplex protocol:

        - `connect`: Establishes TCP connection with optional TLS
        - `handshake`: Exchanges protocol version
        - `writeDatagram`: Sends framed messages with header prefix
        - `readDatagram`: Receives and validates framed messages (non-blocking)
        - `handleDatagram`: Processes different datagram types (keepalive, payload, timestamp, bye)
        - `runStreamingClient`: Main client loop handling connection lifecycle

    === "Rust"
        These functions implement the multiplex protocol:

        - `connect`: Establishes TCP connection with optional TLS
        - `handshake`: Exchanges protocol version
        - `write_datagram`: Sends framed messages with header prefix
        - `read_datagram`: Receives and validates framed messages (non-blocking)
        - `handle_datagram`: Processes different datagram types (keepalive, payload, timestamp, bye)
        - `run_streaming_client`: Main client loop handling connection lifecycle

    === ".NET"
        These functions implement the multiplex protocol:

        - `Connect`: Establishes TCP connection with optional TLS
        - `Handshake`: Exchanges protocol version
        - `WriteDatagram`: Sends framed messages with header prefix
        - `ReadDatagram`: Receives and validates framed messages (non-blocking)
        - `HandleDatagram`: Processes different datagram types (keepalive, payload, timestamp, bye)
        - `RunStreamingClient`: Main client loop handling connection lifecycle

## Producer

The producer creates a TLC session and sends data:

=== "Python"

    ```python
    {% include-markdown "../../examples/multiplex/python/main.py" dedent=true start="# ======== PRODUCER ========" end="# ======== CONSUMER ========" %}
    ```

=== "Go"

    ```go
    {% include-markdown "../../examples/multiplex/go/main.go" dedent=true start="// ======== PRODUCER ========" end="// ======== CONSUMER ========" %}
    ```

=== "Java"

    ```java
    {% include-markdown "../../examples/multiplex/java/Main.java" dedent=true start="    // ======== PRODUCER ========" end="    // ======== CONSUMER ========" %}
    ```

=== "Rust"

    ```rust
    {% include-markdown "../../examples/multiplex/rust/main.rs" dedent=true start="// ======== PRODUCER ========" end="// ======== CONSUMER ========" %}
    ```

=== ".NET"

    ```csharp
    {% include-markdown "../../examples/multiplex/dotnet/Program.cs" dedent=true start="// ======== PRODUCER ========" end="// ======== CONSUMER ========" %}
    ```

!!! note "Explanation"
    The producer:

    1. Creates a TLC session via REST API
    2. Connects to the TCP streaming endpoint
    3. Sends random payload data every second with the configured identifier
    4. Can receive data from other sessions (bi-directional capability)

## Consumer

The consumer creates a Broker session and receives data:

=== "Python"

    ```python
    {% include-markdown "../../examples/multiplex/python/main.py" dedent=true start="# ======== CONSUMER ========" end="# ======== STARTUP AND RUN LOOP ========" %}
    ```

=== "Go"

    ```go
    {% include-markdown "../../examples/multiplex/go/main.go" dedent=true start="// ======== CONSUMER ========" end="// ======== STARTUP AND RUN LOOP ========" %}
    ```

=== "Java"

    ```java
    {% include-markdown "../../examples/multiplex/java/Main.java" dedent=true start="    // ======== CONSUMER ========" end="    // ======== STARTUP AND RUN LOOP ========" %}
    ```

=== "Rust"

    ```rust
    {% include-markdown "../../examples/multiplex/rust/main.rs" dedent=true start="// ======== CONSUMER ========" end="// ======== STARTUP AND RUN LOOP ========" %}
    ```

=== ".NET"

    ```csharp
    {% include-markdown "../../examples/multiplex/dotnet/Program.cs" dedent=true start="// ======== CONSUMER ========" end="// ======== STARTUP AND RUN LOOP ========" %}
    ```

!!! note "Explanation"
    The consumer:
    
    1. Creates a Broker session via REST API
    2. Connects to the TCP streaming endpoint
    3. Receives payload data from producers and calculates latency
    4. Sends keepalive messages every 5 seconds to maintain the connection

## Running the Example

The complete example runs both producer and consumer threads concurrently:

=== "Python"

    ```python
    {% include-markdown "../../examples/multiplex/python/main.py" dedent=true start="# ======== STARTUP AND RUN LOOP ========" %}
    ```

=== "Go"

    ```go
    {% include-markdown "../../examples/multiplex/go/main.go" dedent=true start="// ======== STARTUP AND RUN LOOP ========" %}
    ```

=== "Java"

    ```java
    {% include-markdown "../../examples/multiplex/java/Main.java" dedent=true start="    // ======== STARTUP AND RUN LOOP ========" %}
    ```

=== "Rust"

    ```rust
    {% include-markdown "../../examples/multiplex/rust/main.rs" dedent=true start="// ======== STARTUP AND RUN LOOP ========" %}
    ```

=== ".NET"

    ```csharp
    {% include-markdown "../../examples/multiplex/dotnet/Program.cs" dedent=true start="// ======== STARTUP AND RUN LOOP ========" %}
    ```

## Full Example

| Language | Location | Description |
| -------- | -------- | ----------- |
| Python   | [examples/multiplex/python]({{ config.repo_url }}/tree/main/examples/multiplex/python) | Complete multiplex implementation with producer and consumer |
| Go       | [examples/multiplex/go]({{ config.repo_url }}/tree/main/examples/multiplex/go) | Complete multiplex implementation with producer and consumer |
| Java     | [examples/multiplex/java]({{ config.repo_url }}/tree/main/examples/multiplex/java) | Complete multiplex implementation with producer and consumer |
| Rust     | [examples/multiplex/rust]({{ config.repo_url }}/tree/main/examples/multiplex/rust) | Complete multiplex implementation with producer and consumer |
| .NET     | [examples/multiplex/dotnet]({{ config.repo_url }}/tree/main/examples/multiplex/dotnet) | Complete multiplex implementation with producer and consumer |

## Related Documentation

This example demonstrates both layers of the Subject Interface v1 architecture:

### REST API Documentation

- [API Endpoints Reference](../api-endpoints/reference/endpoints-reference.md) - Creating and managing streaming sessions
- [OpenAPI specification](../api-reference.md) - Complete REST API endpoint documentation

### TCP Streaming Protocol Documentation  

- [Communication Modes](../tcp-streaming-protocol/communication-modes.md) - Singleplex vs Multiplex, session types
- [Datagram Types](../tcp-streaming-protocol/datagram-types.md) - Message formats and protocol details
- [Protocol Overview](../tcp-streaming-protocol/index.md) - Complete protocol documentation