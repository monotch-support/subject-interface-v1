FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder

# Set working directory
WORKDIR /app

# Copy project files
COPY MultiplexExample.csproj ./
COPY Program.cs ./

# Build the application
RUN dotnet build --configuration Release

# Final stage
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine

# Install ca-certificates for HTTPS requests
RUN apk --no-cache add ca-certificates

WORKDIR /root/

# Copy the binary from builder stage
COPY --from=builder /app/bin/Release/net8.0/* ./

# Run the example
CMD ["dotnet", "MultiplexExample.dll"]