FROM python:3.11-slim

# Install system dependencies
RUN apt-get update && apt-get install -y \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy requirements and install Python dependencies
# Note: We only need requests for this example
RUN pip install --no-cache-dir requests

# Copy the example script
COPY main.py .

# Run the example
CMD ["python", "main.py"]