FROM golang:1.21-alpine AS builder

# Set working directory
WORKDIR /app

# Copy go mod files (if they existed, they would go here)
# For this single-file example, we'll use go mod init
RUN go mod init multiplex-example

# Copy the example script
COPY main.go .

# Build the application
RUN go build -o multiplex-example main.go

# Final stage
FROM alpine:latest

# Install ca-certificates for HTTPS requests
RUN apk --no-cache add ca-certificates

WORKDIR /root/

# Copy the binary from builder stage
COPY --from=builder /app/multiplex-example .

# Run the example
CMD ["./multiplex-example"]