import socket
import ssl
import struct
import requests
import time
import os
import threading
import errno
import logging
import json

# ======== CONSTANTS ========
SSL_CONTEXT = ssl.create_default_context(ssl.Purpose.SERVER_AUTH)
HEADER_PREFIX = b'\xAA\xBB'
SECURITY_MODE_NONE = 'NONE'
SECURITY_MODE_TLS = 'TLSv1.2'

# ======== CONFIGURATION ========
STREAMING_API_BASEURL = os.environ.get("STREAMING_API_BASEURL", "https://localhost/api")
STREAMING_API_TLC_TOKEN = os.environ.get("STREAMING_API_TLC_TOKEN", "your-tlc-auth-token")
STREAMING_API_BROKER_TOKEN = os.environ.get("STREAMING_API_BROKER_TOKEN", "your-broker-auth-token")
STREAMING_API_DOMAIN = os.environ.get("STREAMING_API_DOMAIN", "dev_001")
STREAMING_API_SECURITY_MODE = os.environ.get("STREAMING_API_SECURITY_MODE", "TLSv1.2")
STREAMING_API_IDENTIFIER = os.environ.get("STREAMING_API_IDENTIFIER", "sub00001")


# ======== UTILITY FUNCTIONS ========
def log(str):
    logging.info(f"{threading.current_thread().name}: {str}")


def log_json(message, json_dict):
    log("%s: %s" % (message, json.dumps(json_dict, indent=2)))


def as_hexstream(data):
    return "0x" + ''.join(format(x, '02x') for x in data).upper()


def current_timestamp():
    return int(time.time() * 1000)


# ======== REST API FUNCTIONS ========
def create_session(type, token, api_url, security_mode, identifier): 
    url = f"{api_url}/v1/sessions"
    headers = {
        'X-Authorization': f"{token}",
        'Content-Type': 'application/json'
    }
    
    data = {
        "domain": f"{STREAMING_API_DOMAIN}",
        "type": type,
        "protocol": "TCPStreaming_Multiplex",
        "details": {
            "securityMode": security_mode,
            "tlcIdentifiers": [identifier]
        }
    }

    response = requests.post(url, headers=headers, json=data)

    if response.ok:
        response_data = response.json()
        log_json("Session created successfully", response_data)
        token = response_data.get('token')
        host = response_data['details']['listener']['host']
        port = response_data['details']['listener']['port']
        log(f"Token: {token}")
        log(f"Host: {host}")
        log(f"Port: {port}")
        return host, port, token
    else:
        log(f"Request failed with status code {response.status_code}")


# ======== TCP STREAMING FUNCTIONS ========
def connect(host, port, tls):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect((host, port))

    log(f"Connected to {host}:{port} (TLS: {tls})")
    
    if tls:
        # Wrap the socket in an SSL context and perform the TLS handshake
        sslsock = SSL_CONTEXT.wrap_socket(sock, server_hostname=host)
        sslsock.do_handshake()

        peer_name = sslsock.getpeername()
        peer_cert = sslsock.getpeercert()
        (cipher_name, cipher_version, cipher_bits) = sslsock.cipher()

        log(f"TLS handshake successful - Peer: {peer_name}, Certificate: {peer_cert}, Cipher: {cipher_name} {cipher_version} {cipher_bits} bits")
        
        return sslsock
    else:
        return sock


def handshake(sock):
    protocol_version_byte = b'\x01'
    sock.setblocking(True)
    sock.sendall(protocol_version_byte)
    recv = sock.recv(1)
    log(f"Received protocol version {as_hexstream(recv)}")
    if recv[0] != 1:
        raise Exception("Unsupported protocol version received")


def write_datagram(sock, datagram):
    datagram_size = len(datagram).to_bytes(2, 'big')
    header = HEADER_PREFIX + datagram_size
    frame = header + datagram
    log(f"Writing frame {as_hexstream(frame)}")
    sock.setblocking(True)
    sock.sendall(frame)


def write_token(sock, token):
    log(f"Writing token {token}")
    datagram_type = b'\x01'
    data = token.encode('ascii')
    datagram = datagram_type + data
    write_datagram(sock, datagram)


def write_keepalive(sock):
    log("Writing keep alive")
    datagram_type = b'\x00'
    data = b''
    datagram = datagram_type + data
    write_datagram(sock, datagram)


def write_timestamp_response(sock, timestamp_t0, timestamp_t1):
    timestamp_t2 = current_timestamp()
    log(f"Writing timestamp response (t0: {timestamp_t0}, t1: {timestamp_t1}, t2: {timestamp_t2})")
    datagram = struct.pack(">cQQQ", b'\x07', timestamp_t0, timestamp_t1, timestamp_t2)
    write_datagram(sock, datagram)


def write_payload_with_identifier(sock, identifier, payload_type, payload):
    log(f"Writing payload with identifier (identifier: {identifier}, payload_type: {as_hexstream(struct.pack('B', payload_type))}): {as_hexstream(payload)}")
    datagram = b'\x05' + bytes(identifier, 'ascii') + struct.pack(">BQ", payload_type, current_timestamp()) + payload
    write_datagram(sock, datagram)


def read_datagram(sock):
    sock.setblocking(False)
    try:
        header = sock.recv(4)
    except ssl.SSLWantReadError:
        # SSLSocket is not ready for reading, return None.
        return None
    except socket.error as e:
        if e.errno == errno.EAGAIN or e.errno == errno.EWOULDBLOCK:
            # No error, but socket is not ready for reading.
            return None
        else:
            raise e
    else:
        if len(header) == 0:
            raise Exception("Socked disconnected")
        log(f"Received header {as_hexstream(header)}")
        if len(header) != 4:
            raise Exception("Received partial header (implementation does not support partial receives yet)")
        prefix = header[0:2]
        if prefix != HEADER_PREFIX:
            raise Exception(f"Framing error: header prefix {as_hexstream(prefix)} != {as_hexstream(HEADER_PREFIX)}")
        size = struct.unpack(">H", header[2:4])[0]
        log(f"Trying to read {size} bytes datagram")
        sock.setblocking(True)
        datagram = sock.recv(size)
        log(f"Received datagram {as_hexstream(datagram)}")
        if len(datagram) != size:
            raise Exception("Received partial datagram (implementation does not support partial receives yet)")
        return datagram


def handle_keepalive(sock):
    log("Keep alive received")


def handle_bye(sock, datagram):
    log("Bye received")
    reason = datagram[1:]
    log(f"Bye reason: {reason}")


def handle_payload_with_identifier(sock, datagram, payload_received_callback):
    log("Payload with identifier received")
    identifier = datagram[1:9].decode('ascii')
    payload_type = datagram[9:10][0]
    origin_timestamp = struct.unpack(">Q", datagram[10:18])[0]
    payload = datagram[18:]
    log(f"Payload received (identifier: {identifier}, payload_type: {as_hexstream(struct.pack('B', payload_type))}, origin_timestamp: {origin_timestamp}): {as_hexstream(payload)}")
    payload_received_callback(identifier, payload_type, origin_timestamp, payload)


def handle_timestamp_request(sock, datagram):
    log("Timestamp request received")
    timestamp_t0 = struct.unpack(">Q", datagram[1:])[0]
    timestamp_t1 = current_timestamp()
    log(f"Timestamp request delta: {timestamp_t1 - timestamp_t0}ms")
    write_timestamp_response(sock, timestamp_t0, timestamp_t1)


def handle_datagram(sock, datagram, payload_received_callback):
    datagram_type = datagram[0:1]
    match datagram_type:
        case b'\x00':
            handle_keepalive(sock)
        case b'\x02':
            handle_bye(sock, datagram)
        case b'\x05':
            handle_payload_with_identifier(sock, datagram, payload_received_callback)
        case b'\x06':
            handle_timestamp_request(sock, datagram)
        case _:
            log(f"Unknown/unimplemented datagram type {as_hexstream(datagram_type)} received")


def run_streaming_client(host, port, session_token, tls, payload_received_callback, loop_callback):
    sock = connect(host, port, tls)

    try:
        handshake(sock)
        write_token(sock, session_token)

        while True:
            datagram = read_datagram(sock)
            if datagram:
                handle_datagram(sock, datagram, payload_received_callback)
            else:
                time.sleep(0.01)
            
            loop_callback(sock)
    
    finally:
        sock.close()


# ======== PRODUCER ========
def run_producer():
    # Step 1: Create a session using the REST API
    host, port, token = create_session("TLC", STREAMING_API_TLC_TOKEN, STREAMING_API_BASEURL, STREAMING_API_SECURITY_MODE, STREAMING_API_IDENTIFIER)
    
    # Step 2: Connect to the TCP Streaming Node
    last_write = current_timestamp()
    def write_callback(sock):
        nonlocal last_write
        now = current_timestamp()
        # Write a random payload every second
        if now - last_write > 1000:
            last_write = now
            write_payload_with_identifier(sock, STREAMING_API_IDENTIFIER, 0x02, os.urandom(100))
    
    def read_payload_callback(identifier, payload_type, origin_timestamp, payload):
        log(f"Producer received payload from {identifier}: type={payload_type:#04x}, timestamp={origin_timestamp}, size={len(payload)}")
    
    tls = (STREAMING_API_SECURITY_MODE == SECURITY_MODE_TLS)
    run_streaming_client(host, port, token, tls, read_payload_callback, write_callback)


# ======== CONSUMER ========
def run_consumer():
    # Step 1: Create a session using the REST API
    host, port, token = create_session("Broker", STREAMING_API_BROKER_TOKEN, STREAMING_API_BASEURL, STREAMING_API_SECURITY_MODE, STREAMING_API_IDENTIFIER)
    
    # Step 2: Connect to the TCP Streaming Node
    last_write = current_timestamp()
    def write_callback(sock):
        nonlocal last_write
        now = current_timestamp()
        # Write a keepalive every 5 seconds
        if now - last_write > 5000:
            last_write = now
            write_keepalive(sock)
    
    def read_payload_callback(identifier, payload_type, origin_timestamp, payload):
        latency = current_timestamp() - origin_timestamp
        log(f"Consumer received payload from {identifier}: type={payload_type:#04x}, timestamp={origin_timestamp}, latency={latency}ms, size={len(payload)}")
    
    tls = (STREAMING_API_SECURITY_MODE == SECURITY_MODE_TLS)
    run_streaming_client(host, port, token, tls, read_payload_callback, write_callback)


# ======== STARTUP AND RUN LOOP ========
def dump_config():
    logging.info(f"STREAMING_API_BASEURL: '{STREAMING_API_BASEURL}'")
    logging.info(f"STREAMING_API_TLC_TOKEN: '{STREAMING_API_TLC_TOKEN}'")
    logging.info(f"STREAMING_API_BROKER_TOKEN: '{STREAMING_API_BROKER_TOKEN}'")
    logging.info(f"STREAMING_API_DOMAIN: '{STREAMING_API_DOMAIN}'")
    logging.info(f"STREAMING_API_SECURITY_MODE: '{STREAMING_API_SECURITY_MODE}'")


def configure_logging():
    logging.basicConfig(format="%(asctime)s %(levelname)s %(message)s", level=logging.DEBUG)
    logging.getLogger("proton").setLevel(logging.INFO)


if __name__ == "__main__":
    configure_logging()
    dump_config()

    producer_thread = threading.Thread(target=run_producer, name="producer")
    consumer_thread = threading.Thread(target=run_consumer, name="consumer")

    producer_thread.start()
    consumer_thread.start()

    producer_thread.join()
    consumer_thread.join()