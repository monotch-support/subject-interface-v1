FROM rust:1.83-alpine AS builder

# Install build dependencies
RUN apk add --no-cache musl-dev openssl-dev openssl-libs-static pkgconfig

# Set working directory
WORKDIR /app

# Copy Cargo files
COPY Cargo.toml ./

# Copy the source code
COPY main.rs ./

# Build the application
RUN cargo build --release

# Final stage
FROM alpine:latest

# Install ca-certificates for HTTPS requests
RUN apk --no-cache add ca-certificates

WORKDIR /root/

# Copy the binary from builder stage
COPY --from=builder /app/target/release/main ./multiplex-example

# Set RUST_LOG environment variable for logging
ENV RUST_LOG=info

# Run the example
CMD ["./multiplex-example"]