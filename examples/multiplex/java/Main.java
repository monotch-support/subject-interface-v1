import java.io.*;
import java.net.*;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.security.SecureRandom;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.logging.*;
import javax.net.ssl.*;
import org.json.JSONArray;
import org.json.JSONObject;

public class Main {
    // ======== CONSTANTS ========
    private static final byte[] HEADER_PREFIX = {(byte) 0xAA, (byte) 0xBB};
    private static final String SECURITY_MODE_NONE = "NONE";
    private static final String SECURITY_MODE_TLS = "TLSv1.2";
    
    // ======== CONFIGURATION ========
    private static final String STREAMING_API_BASEURL = System.getenv("STREAMING_API_BASEURL") != null 
        ? System.getenv("STREAMING_API_BASEURL") : "https://localhost/api";
    private static final String STREAMING_API_TLC_TOKEN = System.getenv("STREAMING_API_TLC_TOKEN") != null 
        ? System.getenv("STREAMING_API_TLC_TOKEN") : "your-tlc-auth-token";
    private static final String STREAMING_API_BROKER_TOKEN = System.getenv("STREAMING_API_BROKER_TOKEN") != null 
        ? System.getenv("STREAMING_API_BROKER_TOKEN") : "your-broker-auth-token";
    private static final String STREAMING_API_DOMAIN = System.getenv("STREAMING_API_DOMAIN") != null 
        ? System.getenv("STREAMING_API_DOMAIN") : "dev_001";
    private static final String STREAMING_API_SECURITY_MODE = System.getenv("STREAMING_API_SECURITY_MODE") != null 
        ? System.getenv("STREAMING_API_SECURITY_MODE") : "TLSv1.2";
    private static final String STREAMING_API_IDENTIFIER = System.getenv("STREAMING_API_IDENTIFIER") != null 
        ? System.getenv("STREAMING_API_IDENTIFIER") : "sub00001";
    
    // ======== UTILITY FUNCTIONS ========
    private static final Logger logger = Logger.getLogger(Main.class.getName());
    private static final SecureRandom random = new SecureRandom();

    private static void log(String threadName, String message) {
        logger.info(threadName + ": " + message);
    }
    
    private static void logJSON(String threadName, String message, JSONObject json) {
        log(threadName, message + ": " + json.toString(2));
    }
    
    private static String asHexStream(byte[] data) {
        StringBuilder sb = new StringBuilder("0x");
        for (byte b : data) {
            sb.append(String.format("%02X", b));
        }
        return sb.toString();
    }
    
    private static long currentTimestamp() {
        return System.currentTimeMillis();
    }
    
    // ======== REST API FUNCTIONS ========
    private static class SessionInfo {
        String host;
        int port;
        String token;
        
        SessionInfo(String host, int port, String token) {
            this.host = host;
            this.port = port;
            this.token = token;
        }
    }
    
    private static SessionInfo createSession(String type, String token, String apiUrl, 
            String securityMode, String identifier, String threadName) throws Exception {
        String url = apiUrl + "/v1/sessions";
        
        JSONObject requestData = new JSONObject();
        requestData.put("domain", STREAMING_API_DOMAIN);
        requestData.put("type", type);
        requestData.put("protocol", "TCPStreaming_Multiplex");
        
        JSONObject details = new JSONObject();
        details.put("securityMode", securityMode);
        details.put("tlcIdentifiers", new JSONArray().put(identifier));
        requestData.put("details", details);
        
        // Disable SSL verification for localhost (development only)
        if (apiUrl.contains("localhost")) {
            disableSSLVerification();
        }
        
        HttpURLConnection conn = (HttpURLConnection) new URL(url).openConnection();
        conn.setRequestMethod("POST");
        conn.setRequestProperty("X-Authorization", token);
        conn.setRequestProperty("Content-Type", "application/json");
        conn.setDoOutput(true);
        
        try (OutputStreamWriter writer = new OutputStreamWriter(conn.getOutputStream())) {
            writer.write(requestData.toString());
        }
        
        int responseCode = conn.getResponseCode();
        if (responseCode != HttpURLConnection.HTTP_OK) {
            throw new Exception("Request failed with status code " + responseCode);
        }
        
        StringBuilder response = new StringBuilder();
        try (BufferedReader reader = new BufferedReader(new InputStreamReader(conn.getInputStream()))) {
            String line;
            while ((line = reader.readLine()) != null) {
                response.append(line);
            }
        }
        
        JSONObject responseData = new JSONObject(response.toString());
        logJSON(threadName, "Session created successfully", responseData);
        
        String sessionToken = responseData.getString("token");
        String host = responseData.getJSONObject("details").getJSONObject("listener").getString("host");
        int port = responseData.getJSONObject("details").getJSONObject("listener").getInt("port");
        
        log(threadName, "Token: " + sessionToken);
        log(threadName, "Host: " + host);
        log(threadName, "Port: " + port);
        
        return new SessionInfo(host, port, sessionToken);
    }
    
    private static void disableSSLVerification() throws Exception {
        TrustManager[] trustAllCerts = new TrustManager[] {
            new X509TrustManager() {
                public java.security.cert.X509Certificate[] getAcceptedIssuers() { return null; }
                public void checkClientTrusted(java.security.cert.X509Certificate[] certs, String authType) {}
                public void checkServerTrusted(java.security.cert.X509Certificate[] certs, String authType) {}
            }
        };
        
        SSLContext sc = SSLContext.getInstance("SSL");
        sc.init(null, trustAllCerts, new java.security.SecureRandom());
        HttpsURLConnection.setDefaultSSLSocketFactory(sc.getSocketFactory());
        HttpsURLConnection.setDefaultHostnameVerifier((hostname, session) -> true);
    }
    
    // ======== TCP STREAMING FUNCTIONS ========
    private static Socket connect(String host, int port, boolean tls, String threadName) throws Exception {
        Socket socket;
        
        if (tls) {
            SSLSocketFactory sslSocketFactory = (SSLSocketFactory) SSLSocketFactory.getDefault();
            SSLSocket sslSocket = (SSLSocket) sslSocketFactory.createSocket(host, port);
            sslSocket.startHandshake();
            
            log(threadName, String.format("Connected to %s:%d (TLS: %b)", host, port, tls));
            
            SSLSession session = sslSocket.getSession();
            log(threadName, String.format("TLS handshake successful - Protocol: %s, Cipher: %s", 
                session.getProtocol(), session.getCipherSuite()));
            
            socket = sslSocket;
        } else {
            socket = new Socket(host, port);
            log(threadName, String.format("Connected to %s:%d (TLS: %b)", host, port, tls));
        }
        
        return socket;
    }
    
    private static void handshake(Socket socket, String threadName) throws Exception {
        byte[] protocolVersionByte = {0x01};
        socket.getOutputStream().write(protocolVersionByte);
        socket.getOutputStream().flush();
        
        byte[] recv = new byte[1];
        int bytesRead = socket.getInputStream().read(recv);
        if (bytesRead != 1) {
            throw new Exception("Failed to receive protocol version");
        }
        
        log(threadName, "Received protocol version " + asHexStream(recv));
        
        if (recv[0] != 1) {
            throw new Exception("Unsupported protocol version received");
        }
    }
    
    private static void writeDatagram(Socket socket, byte[] datagram, String threadName) throws Exception {
        ByteBuffer buffer = ByteBuffer.allocate(4 + datagram.length);
        buffer.put(HEADER_PREFIX);
        buffer.putShort((short) datagram.length);
        buffer.put(datagram);
        
        byte[] frame = buffer.array();
        log(threadName, "Writing frame " + asHexStream(frame));
        
        socket.getOutputStream().write(frame);
        socket.getOutputStream().flush();
    }
    
    private static void writeToken(Socket socket, String token, String threadName) throws Exception {
        log(threadName, "Writing token " + token);
        ByteBuffer datagram = ByteBuffer.allocate(1 + token.length());
        datagram.put((byte) 0x01);
        datagram.put(token.getBytes(StandardCharsets.US_ASCII));
        writeDatagram(socket, datagram.array(), threadName);
    }
    
    private static void writeKeepalive(Socket socket, String threadName) throws Exception {
        log(threadName, "Writing keep alive");
        byte[] datagram = {0x00};
        writeDatagram(socket, datagram, threadName);
    }
    
    private static void writeTimestampResponse(Socket socket, long timestampT0, long timestampT1, String threadName) throws Exception {
        long timestampT2 = currentTimestamp();
        log(threadName, String.format("Writing timestamp response (t0: %d, t1: %d, t2: %d)", timestampT0, timestampT1, timestampT2));
        
        ByteBuffer datagram = ByteBuffer.allocate(25);
        datagram.put((byte) 0x07);
        datagram.putLong(timestampT0);
        datagram.putLong(timestampT1);
        datagram.putLong(timestampT2);
        writeDatagram(socket, datagram.array(), threadName);
    }
    
    private static void writePayloadWithIdentifier(Socket socket, String identifier, byte payloadType, 
            byte[] payload, String threadName) throws Exception {
        log(threadName, String.format("Writing payload with identifier (identifier: %s, payload_type: %s): %s",
            identifier, asHexStream(new byte[]{payloadType}), asHexStream(payload)));
        
        ByteBuffer datagram = ByteBuffer.allocate(1 + 8 + 1 + 8 + payload.length);
        datagram.put((byte) 0x05);
        
        // Pad identifier to 8 bytes
        byte[] identifierBytes = identifier.getBytes(StandardCharsets.US_ASCII);
        byte[] paddedIdentifier = new byte[8];
        System.arraycopy(identifierBytes, 0, paddedIdentifier, 0, Math.min(identifierBytes.length, 8));
        datagram.put(paddedIdentifier);
        
        datagram.put(payloadType);
        datagram.putLong(currentTimestamp());
        datagram.put(payload);
        
        writeDatagram(socket, datagram.array(), threadName);
    }
    
    private static byte[] readDatagram(Socket socket, String threadName) throws Exception {
        InputStream is = socket.getInputStream();
        
        // Try non-blocking read
        socket.setSoTimeout(10);
        
        byte[] header = new byte[4];
        try {
            int bytesRead = 0;
            while (bytesRead < 4) {
                int n = is.read(header, bytesRead, 4 - bytesRead);
                if (n == -1) {
                    throw new Exception("Socket disconnected");
                }
                bytesRead += n;
            }
        } catch (SocketTimeoutException e) {
            return null; // No data available
        }
        
        log(threadName, "Received header " + asHexStream(header));
        
        if (header[0] != HEADER_PREFIX[0] || header[1] != HEADER_PREFIX[1]) {
            throw new Exception(String.format("Framing error: header prefix %s != %s", 
                asHexStream(new byte[]{header[0], header[1]}), asHexStream(HEADER_PREFIX)));
        }
        
        int size = ((header[2] & 0xFF) << 8) | (header[3] & 0xFF);
        log(threadName, String.format("Trying to read %d bytes datagram", size));
        
        // Set blocking for reading the full datagram
        socket.setSoTimeout(0);
        
        byte[] datagram = new byte[size];
        int bytesRead = 0;
        while (bytesRead < size) {
            int n = is.read(datagram, bytesRead, size - bytesRead);
            if (n == -1) {
                throw new Exception("Socket disconnected");
            }
            bytesRead += n;
        }
        
        log(threadName, "Received datagram " + asHexStream(datagram));
        return datagram;
    }
    
    private static void handleKeepalive(Socket socket, String threadName) {
        log(threadName, "Keep alive received");
    }
    
    private static void handleBye(Socket socket, byte[] datagram, String threadName) {
        log(threadName, "Bye received");
        if (datagram.length > 1) {
            String reason = new String(datagram, 1, datagram.length - 1, StandardCharsets.UTF_8);
            log(threadName, "Bye reason: " + reason);
        }
    }
    
    @FunctionalInterface
    interface PayloadCallback {
        void onPayload(String identifier, byte payloadType, long originTimestamp, byte[] payload);
    }
    
    private static void handlePayloadWithIdentifier(Socket socket, byte[] datagram, 
            PayloadCallback callback, String threadName) {
        log(threadName, "Payload with identifier received");
        
        String identifier = new String(datagram, 1, 8, StandardCharsets.US_ASCII).trim();
        byte payloadType = datagram[9];
        long originTimestamp = ByteBuffer.wrap(datagram, 10, 8).getLong();
        byte[] payload = new byte[datagram.length - 18];
        System.arraycopy(datagram, 18, payload, 0, payload.length);
        
        log(threadName, String.format("Payload received (identifier: %s, payload_type: %s, origin_timestamp: %d): %s",
            identifier, asHexStream(new byte[]{payloadType}), originTimestamp, asHexStream(payload)));
        
        callback.onPayload(identifier, payloadType, originTimestamp, payload);
    }
    
    private static void handleTimestampRequest(Socket socket, byte[] datagram, String threadName) throws Exception {
        log(threadName, "Timestamp request received");
        long timestampT0 = ByteBuffer.wrap(datagram, 1, 8).getLong();
        long timestampT1 = currentTimestamp();
        log(threadName, String.format("Timestamp request delta: %dms", timestampT1 - timestampT0));
        writeTimestampResponse(socket, timestampT0, timestampT1, threadName);
    }
    
    private static void handleDatagram(Socket socket, byte[] datagram, PayloadCallback callback, String threadName) throws Exception {
        byte datagramType = datagram[0];
        
        switch (datagramType) {
            case 0x00:
                handleKeepalive(socket, threadName);
                break;
            case 0x02:
                handleBye(socket, datagram, threadName);
                break;
            case 0x05:
                handlePayloadWithIdentifier(socket, datagram, callback, threadName);
                break;
            case 0x06:
                handleTimestampRequest(socket, datagram, threadName);
                break;
            default:
                log(threadName, String.format("Unknown/unimplemented datagram type %s received", 
                    asHexStream(new byte[]{datagramType})));
        }
    }
    
    @FunctionalInterface
    interface LoopCallback {
        void onLoop(Socket socket) throws Exception;
    }
    
    private static void runStreamingClient(String host, int port, String sessionToken, boolean tls,
            PayloadCallback payloadCallback, LoopCallback loopCallback, String threadName) throws Exception {
        Socket socket = connect(host, port, tls, threadName);
        
        try {
            handshake(socket, threadName);
            writeToken(socket, sessionToken, threadName);
            
            while (!socket.isClosed()) {
                byte[] datagram = readDatagram(socket, threadName);
                if (datagram != null) {
                    handleDatagram(socket, datagram, payloadCallback, threadName);
                } else {
                    Thread.sleep(10);
                }
                
                loopCallback.onLoop(socket);
            }
        } finally {
            socket.close();
        }
    }
    
    // ======== PRODUCER ========
    private static void runProducer() {
        String threadName = "producer";
        
        try {
            // Step 1: Create a session using the REST API
            SessionInfo session = createSession("TLC", STREAMING_API_TLC_TOKEN, STREAMING_API_BASEURL, 
                STREAMING_API_SECURITY_MODE, STREAMING_API_IDENTIFIER, threadName);
            
            // Step 2: Connect to the TCP Streaming Node
            final long[] lastWrite = {currentTimestamp()};
            
            LoopCallback writeCallback = (socket) -> {
                long now = currentTimestamp();
                // Write a random payload every second
                if (now - lastWrite[0] > 1000) {
                    lastWrite[0] = now;
                    byte[] payload = new byte[100];
                    random.nextBytes(payload);
                    writePayloadWithIdentifier(socket, STREAMING_API_IDENTIFIER, (byte) 0x02, payload, threadName);
                }
            };
            
            PayloadCallback readPayloadCallback = (identifier, payloadType, originTimestamp, payload) -> {
                log(threadName, String.format("Producer received payload from %s: type=0x%02x, timestamp=%d, size=%d",
                    identifier, payloadType, originTimestamp, payload.length));
            };
            
            boolean useTls = STREAMING_API_SECURITY_MODE.equals(SECURITY_MODE_TLS);
            runStreamingClient(session.host, session.port, session.token, useTls, 
                readPayloadCallback, writeCallback, threadName);
                
        } catch (Exception e) {
            log(threadName, "Error: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    // ======== CONSUMER ========
    private static void runConsumer() {
        String threadName = "consumer";
        
        try {
            // Step 1: Create a session using the REST API
            SessionInfo session = createSession("Broker", STREAMING_API_BROKER_TOKEN, STREAMING_API_BASEURL, 
                STREAMING_API_SECURITY_MODE, STREAMING_API_IDENTIFIER, threadName);
            
            // Step 2: Connect to the TCP Streaming Node
            final long[] lastWrite = {currentTimestamp()};
            
            LoopCallback writeCallback = (socket) -> {
                long now = currentTimestamp();
                // Write a keepalive every 5 seconds
                if (now - lastWrite[0] > 5000) {
                    lastWrite[0] = now;
                    writeKeepalive(socket, threadName);
                }
            };
            
            PayloadCallback readPayloadCallback = (identifier, payloadType, originTimestamp, payload) -> {
                long latency = currentTimestamp() - originTimestamp;
                log(threadName, String.format("Consumer received payload from %s: type=0x%02x, timestamp=%d, latency=%dms, size=%d",
                    identifier, payloadType, originTimestamp, latency, payload.length));
            };
            
            boolean useTls = STREAMING_API_SECURITY_MODE.equals(SECURITY_MODE_TLS);
            runStreamingClient(session.host, session.port, session.token, useTls, 
                readPayloadCallback, writeCallback, threadName);
                
        } catch (Exception e) {
            log(threadName, "Error: " + e.getMessage());
            e.printStackTrace();
        }
    }
    
    // ======== STARTUP AND RUN LOOP ========
    private static void dumpConfig() {
        logger.info("STREAMING_API_BASEURL: '" + STREAMING_API_BASEURL + "'");
        logger.info("STREAMING_API_TLC_TOKEN: '" + STREAMING_API_TLC_TOKEN + "'");
        logger.info("STREAMING_API_BROKER_TOKEN: '" + STREAMING_API_BROKER_TOKEN + "'");
        logger.info("STREAMING_API_DOMAIN: '" + STREAMING_API_DOMAIN + "'");
        logger.info("STREAMING_API_SECURITY_MODE: '" + STREAMING_API_SECURITY_MODE + "'");
    }
    
    private static void configureLogging() {
        Logger rootLogger = Logger.getLogger("");
        // Remove default handlers
        Handler[] handlers = rootLogger.getHandlers();
        for (Handler handler : handlers) {
            rootLogger.removeHandler(handler);
        }
        
        ConsoleHandler consoleHandler = new ConsoleHandler();
        consoleHandler.setFormatter(new SimpleFormatter() {
            @Override
            public String format(LogRecord record) {
                return String.format("%1$tF %1$tT %2$s%n",
                    record.getMillis(),
                    record.getMessage());
            }
        });
        rootLogger.addHandler(consoleHandler);
        rootLogger.setLevel(Level.INFO);
        consoleHandler.setLevel(Level.INFO);
    }
    
    public static void main(String[] args) {
        configureLogging();
        dumpConfig();
        
        Thread producerThread = new Thread(Main::runProducer, "producer");
        Thread consumerThread = new Thread(Main::runConsumer, "consumer");
        
        producerThread.start();
        consumerThread.start();
        
        try {
            producerThread.join();
            consumerThread.join();
        } catch (InterruptedException e) {
            logger.severe("Thread interrupted: " + e.getMessage());
        }
    }
}