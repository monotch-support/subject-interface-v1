package main

import (
	"bytes"
	"crypto/rand"
	"crypto/tls"
	"encoding/binary"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net"
	"net/http"
	"os"
	"strings"
	"sync"
	"time"
)

// ======== CONSTANTS ========
var (
	HEADER_PREFIX       = []byte{0xAA, 0xBB}
	SECURITY_MODE_NONE  = "NONE"
	SECURITY_MODE_TLS   = "TLSv1.2"
)

// ======== CONFIGURATION ========
var (
	STREAMING_API_BASEURL      = getEnv("STREAMING_API_BASEURL", "https://localhost/api")
	STREAMING_API_TLC_TOKEN    = getEnv("STREAMING_API_TLC_TOKEN", "your-tlc-auth-token")
	STREAMING_API_BROKER_TOKEN = getEnv("STREAMING_API_BROKER_TOKEN", "your-broker-auth-token")
	STREAMING_API_DOMAIN       = getEnv("STREAMING_API_DOMAIN", "dev_001")
	STREAMING_API_SECURITY_MODE = getEnv("STREAMING_API_SECURITY_MODE", "TLSv1.2")
	STREAMING_API_IDENTIFIER   = getEnv("STREAMING_API_IDENTIFIER", "sub00001")
)

// ======== UTILITY FUNCTIONS ========
func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}

func logMsg(threadName, msg string) {
	log.Printf("%s: %s", threadName, msg)
}

func logJSON(threadName, message string, data interface{}) {
	jsonBytes, _ := json.MarshalIndent(data, "", "  ")
	logMsg(threadName, fmt.Sprintf("%s: %s", message, string(jsonBytes)))
}

func asHexStream(data []byte) string {
	return "0x" + strings.ToUpper(fmt.Sprintf("%x", data))
}

func currentTimestamp() int64 {
	return time.Now().UnixMilli()
}

// ======== REST API FUNCTIONS ========
func createSession(sessionType, token, apiURL, securityMode, identifier, threadName string) (string, int, string, error) {
	url := fmt.Sprintf("%s/v1/sessions", apiURL)
	
	requestData := map[string]interface{}{
		"domain":   STREAMING_API_DOMAIN,
		"type":     sessionType,
		"protocol": "TCPStreaming_Multiplex",
		"details": map[string]interface{}{
			"securityMode":   securityMode,
			"tlcIdentifiers": []string{identifier},
		},
	}
	
	jsonData, err := json.Marshal(requestData)
	if err != nil {
		return "", 0, "", fmt.Errorf("failed to marshal request data: %v", err)
	}
	
	req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonData))
	if err != nil {
		return "", 0, "", fmt.Errorf("failed to create request: %v", err)
	}
	
	req.Header.Set("X-Authorization", token)
	req.Header.Set("Content-Type", "application/json")
	
	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return "", 0, "", fmt.Errorf("request failed: %v", err)
	}
	defer resp.Body.Close()
	
	if resp.StatusCode != http.StatusOK {
		return "", 0, "", fmt.Errorf("request failed with status code %d", resp.StatusCode)
	}
	
	var responseData map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&responseData); err != nil {
		return "", 0, "", fmt.Errorf("failed to decode response: %v", err)
	}
	
	logJSON(threadName, "Session created successfully", responseData)
	
	sessionToken := responseData["token"].(string)
	host := responseData["details"].(map[string]interface{})["listener"].(map[string]interface{})["host"].(string)
	port := int(responseData["details"].(map[string]interface{})["listener"].(map[string]interface{})["port"].(float64))
	
	logMsg(threadName, fmt.Sprintf("Token: %s", sessionToken))
	logMsg(threadName, fmt.Sprintf("Host: %s", host))
	logMsg(threadName, fmt.Sprintf("Port: %d", port))
	
	return host, port, sessionToken, nil
}

// ======== TCP STREAMING FUNCTIONS ========
func connect(host string, port int, useTLS bool, threadName string) (net.Conn, error) {
	address := fmt.Sprintf("%s:%d", host, port)
	
	if useTLS {
		config := &tls.Config{
			ServerName: host,
		}
		conn, err := tls.Dial("tcp", address, config)
		if err != nil {
			return nil, fmt.Errorf("TLS connection failed: %v", err)
		}
		
		logMsg(threadName, fmt.Sprintf("Connected to %s:%d (TLS: %t)", host, port, useTLS))
		
		// Log TLS connection details
		state := conn.ConnectionState()
		logMsg(threadName, fmt.Sprintf("TLS handshake successful - Version: %x, Cipher: %x", state.Version, state.CipherSuite))
		
		return conn, nil
	} else {
		conn, err := net.Dial("tcp", address)
		if err != nil {
			return nil, fmt.Errorf("TCP connection failed: %v", err)
		}
		
		logMsg(threadName, fmt.Sprintf("Connected to %s:%d (TLS: %t)", host, port, useTLS))
		return conn, nil
	}
}

func handshake(conn net.Conn, threadName string) error {
	protocolVersionByte := []byte{0x01}
	_, err := conn.Write(protocolVersionByte)
	if err != nil {
		return fmt.Errorf("failed to send protocol version: %v", err)
	}
	
	recv := make([]byte, 1)
	_, err = io.ReadFull(conn, recv)
	if err != nil {
		return fmt.Errorf("failed to receive protocol version: %v", err)
	}
	
	logMsg(threadName, fmt.Sprintf("Received protocol version %s", asHexStream(recv)))
	
	if recv[0] != 1 {
		return fmt.Errorf("unsupported protocol version received")
	}
	
	return nil
}

func writeDatagram(conn net.Conn, datagram []byte, threadName string) error {
	datagramSize := make([]byte, 2)
	binary.BigEndian.PutUint16(datagramSize, uint16(len(datagram)))
	
	header := append(HEADER_PREFIX, datagramSize...)
	frame := append(header, datagram...)
	
	logMsg(threadName, fmt.Sprintf("Writing frame %s", asHexStream(frame)))
	
	_, err := conn.Write(frame)
	return err
}

func writeToken(conn net.Conn, token, threadName string) error {
	logMsg(threadName, fmt.Sprintf("Writing token %s", token))
	datagramType := []byte{0x01}
	data := []byte(token)
	datagram := append(datagramType, data...)
	return writeDatagram(conn, datagram, threadName)
}

func writeKeepalive(conn net.Conn, threadName string) error {
	logMsg(threadName, "Writing keep alive")
	datagramType := []byte{0x00}
	data := []byte{}
	datagram := append(datagramType, data...)
	return writeDatagram(conn, datagram, threadName)
}

func writeTimestampResponse(conn net.Conn, timestampT0, timestampT1 int64, threadName string) error {
	timestampT2 := currentTimestamp()
	logMsg(threadName, fmt.Sprintf("Writing timestamp response (t0: %d, t1: %d, t2: %d)", timestampT0, timestampT1, timestampT2))
	
	datagram := make([]byte, 25)
	datagram[0] = 0x07
	binary.BigEndian.PutUint64(datagram[1:9], uint64(timestampT0))
	binary.BigEndian.PutUint64(datagram[9:17], uint64(timestampT1))
	binary.BigEndian.PutUint64(datagram[17:25], uint64(timestampT2))
	
	return writeDatagram(conn, datagram, threadName)
}

func writePayloadWithIdentifier(conn net.Conn, identifier string, payloadType byte, payload []byte, threadName string) error {
	logMsg(threadName, fmt.Sprintf("Writing payload with identifier (identifier: %s, payload_type: %s): %s", 
		identifier, asHexStream([]byte{payloadType}), asHexStream(payload)))
	
	datagram := []byte{0x05}
	datagram = append(datagram, []byte(identifier)...)
	
	// Add payload type and timestamp
	payloadTypeAndTimestamp := make([]byte, 9)
	payloadTypeAndTimestamp[0] = payloadType
	binary.BigEndian.PutUint64(payloadTypeAndTimestamp[1:9], uint64(currentTimestamp()))
	datagram = append(datagram, payloadTypeAndTimestamp...)
	datagram = append(datagram, payload...)
	
	return writeDatagram(conn, datagram, threadName)
}

func readDatagram(conn net.Conn, threadName string) ([]byte, error) {
	// Set read timeout for non-blocking behavior
	conn.SetReadDeadline(time.Now().Add(10 * time.Millisecond))
	
	header := make([]byte, 4)
	n, err := io.ReadFull(conn, header)
	if err != nil {
		if netErr, ok := err.(net.Error); ok && netErr.Timeout() {
			return nil, nil // No data available, return nil
		}
		if err == io.EOF {
			return nil, fmt.Errorf("socket disconnected")
		}
		return nil, err
	}
	
	if n == 0 {
		return nil, fmt.Errorf("socket disconnected")
	}
	
	logMsg(threadName, fmt.Sprintf("Received header %s", asHexStream(header)))
	
	if n != 4 {
		return nil, fmt.Errorf("received partial header (implementation does not support partial receives yet)")
	}
	
	prefix := header[0:2]
	if !bytes.Equal(prefix, HEADER_PREFIX) {
		return nil, fmt.Errorf("framing error: header prefix %s != %s", asHexStream(prefix), asHexStream(HEADER_PREFIX))
	}
	
	size := binary.BigEndian.Uint16(header[2:4])
	logMsg(threadName, fmt.Sprintf("Trying to read %d bytes datagram", size))
	
	// Remove timeout for reading the full datagram
	conn.SetReadDeadline(time.Time{})
	datagram := make([]byte, size)
	n, err = io.ReadFull(conn, datagram)
	if err != nil {
		return nil, err
	}
	
	logMsg(threadName, fmt.Sprintf("Received datagram %s", asHexStream(datagram)))
	
	if n != int(size) {
		return nil, fmt.Errorf("received partial datagram (implementation does not support partial receives yet)")
	}
	
	return datagram, nil
}

func handleKeepalive(conn net.Conn, threadName string) {
	logMsg(threadName, "Keep alive received")
}

func handleBye(conn net.Conn, datagram []byte, threadName string) {
	logMsg(threadName, "Bye received")
	reason := datagram[1:]
	logMsg(threadName, fmt.Sprintf("Bye reason: %s", string(reason)))
}

func handlePayloadWithIdentifier(conn net.Conn, datagram []byte, payloadReceivedCallback func(string, byte, int64, []byte), threadName string) {
	logMsg(threadName, "Payload with identifier received")
	identifier := string(datagram[1:9])
	payloadType := datagram[9]
	originTimestamp := int64(binary.BigEndian.Uint64(datagram[10:18]))
	payload := datagram[18:]
	
	logMsg(threadName, fmt.Sprintf("Payload received (identifier: %s, payload_type: %s, origin_timestamp: %d): %s", 
		identifier, asHexStream([]byte{payloadType}), originTimestamp, asHexStream(payload)))
	
	payloadReceivedCallback(identifier, payloadType, originTimestamp, payload)
}

func handleTimestampRequest(conn net.Conn, datagram []byte, threadName string) {
	logMsg(threadName, "Timestamp request received")
	timestampT0 := int64(binary.BigEndian.Uint64(datagram[1:]))
	timestampT1 := currentTimestamp()
	logMsg(threadName, fmt.Sprintf("Timestamp request delta: %dms", timestampT1-timestampT0))
	writeTimestampResponse(conn, timestampT0, timestampT1, threadName)
}

func handleDatagram(conn net.Conn, datagram []byte, payloadReceivedCallback func(string, byte, int64, []byte), threadName string) {
	datagramType := datagram[0]
	
	switch datagramType {
	case 0x00:
		handleKeepalive(conn, threadName)
	case 0x02:
		handleBye(conn, datagram, threadName)
	case 0x05:
		handlePayloadWithIdentifier(conn, datagram, payloadReceivedCallback, threadName)
	case 0x06:
		handleTimestampRequest(conn, datagram, threadName)
	default:
		logMsg(threadName, fmt.Sprintf("Unknown/unimplemented datagram type %s received", asHexStream([]byte{datagramType})))
	}
}

func runStreamingClient(host string, port int, sessionToken string, useTLS bool, payloadReceivedCallback func(string, byte, int64, []byte), loopCallback func(net.Conn), threadName string) error {
	conn, err := connect(host, port, useTLS, threadName)
	if err != nil {
		return err
	}
	defer conn.Close()
	
	if err := handshake(conn, threadName); err != nil {
		return err
	}
	
	if err := writeToken(conn, sessionToken, threadName); err != nil {
		return err
	}
	
	for {
		datagram, err := readDatagram(conn, threadName)
		if err != nil {
			return err
		}
		
		if datagram != nil {
			handleDatagram(conn, datagram, payloadReceivedCallback, threadName)
		} else {
			time.Sleep(10 * time.Millisecond)
		}
		
		loopCallback(conn)
	}
}

// ======== PRODUCER ========
func runProducer(wg *sync.WaitGroup) {
	defer wg.Done()
	threadName := "producer"
	
	// Step 1: Create a session using the REST API
	host, port, token, err := createSession("TLC", STREAMING_API_TLC_TOKEN, STREAMING_API_BASEURL, STREAMING_API_SECURITY_MODE, STREAMING_API_IDENTIFIER, threadName)
	if err != nil {
		logMsg(threadName, fmt.Sprintf("Failed to create session: %v", err))
		return
	}
	
	// Step 2: Connect to the TCP Streaming Node
	lastWrite := currentTimestamp()
	writeCallback := func(conn net.Conn) {
		now := currentTimestamp()
		// Write a random payload every second
		if now-lastWrite > 1000 {
			lastWrite = now
			payload := make([]byte, 100)
			rand.Read(payload)
			writePayloadWithIdentifier(conn, STREAMING_API_IDENTIFIER, 0x02, payload, threadName)
		}
	}
	
	readPayloadCallback := func(identifier string, payloadType byte, originTimestamp int64, payload []byte) {
		logMsg(threadName, fmt.Sprintf("Producer received payload from %s: type=%#04x, timestamp=%d, size=%d", 
			identifier, payloadType, originTimestamp, len(payload)))
	}
	
	useTLS := (STREAMING_API_SECURITY_MODE == SECURITY_MODE_TLS)
	if err := runStreamingClient(host, port, token, useTLS, readPayloadCallback, writeCallback, threadName); err != nil {
		logMsg(threadName, fmt.Sprintf("Streaming client error: %v", err))
	}
}

// ======== CONSUMER ========
func runConsumer(wg *sync.WaitGroup) {
	defer wg.Done()
	threadName := "consumer"
	
	// Step 1: Create a session using the REST API
	host, port, token, err := createSession("Broker", STREAMING_API_BROKER_TOKEN, STREAMING_API_BASEURL, STREAMING_API_SECURITY_MODE, STREAMING_API_IDENTIFIER, threadName)
	if err != nil {
		logMsg(threadName, fmt.Sprintf("Failed to create session: %v", err))
		return
	}
	
	// Step 2: Connect to the TCP Streaming Node
	lastWrite := currentTimestamp()
	writeCallback := func(conn net.Conn) {
		now := currentTimestamp()
		// Write a keepalive every 5 seconds
		if now-lastWrite > 5000 {
			lastWrite = now
			writeKeepalive(conn, threadName)
		}
	}
	
	readPayloadCallback := func(identifier string, payloadType byte, originTimestamp int64, payload []byte) {
		latency := currentTimestamp() - originTimestamp
		logMsg(threadName, fmt.Sprintf("Consumer received payload from %s: type=%#04x, timestamp=%d, latency=%dms, size=%d", 
			identifier, payloadType, originTimestamp, latency, len(payload)))
	}
	
	useTLS := (STREAMING_API_SECURITY_MODE == SECURITY_MODE_TLS)
	if err := runStreamingClient(host, port, token, useTLS, readPayloadCallback, writeCallback, threadName); err != nil {
		logMsg(threadName, fmt.Sprintf("Streaming client error: %v", err))
	}
}

// ======== STARTUP AND RUN LOOP ========
func dumpConfig() {
	log.Printf("STREAMING_API_BASEURL: '%s'", STREAMING_API_BASEURL)
	log.Printf("STREAMING_API_TLC_TOKEN: '%s'", STREAMING_API_TLC_TOKEN)
	log.Printf("STREAMING_API_BROKER_TOKEN: '%s'", STREAMING_API_BROKER_TOKEN)
	log.Printf("STREAMING_API_DOMAIN: '%s'", STREAMING_API_DOMAIN)
	log.Printf("STREAMING_API_SECURITY_MODE: '%s'", STREAMING_API_SECURITY_MODE)
}

func configureLogging() {
	log.SetFlags(log.LstdFlags)
}

func main() {
	configureLogging()
	dumpConfig()
	
	var wg sync.WaitGroup
	
	wg.Add(2)
	go runProducer(&wg)
	go runConsumer(&wg)
	
	wg.Wait()
}