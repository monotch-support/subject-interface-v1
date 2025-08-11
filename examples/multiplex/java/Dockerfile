FROM openjdk:11-jdk-slim

# Install curl for downloading dependencies
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Download json-java library (org.json)
RUN curl -o json.jar https://repo1.maven.org/maven2/org/json/json/20230618/json-20230618.jar

# Copy the example source
COPY Main.java .

# Compile the Java program
RUN javac -cp json.jar Main.java

# Run the example
CMD ["java", "-cp", ".:json.jar", "Main"]