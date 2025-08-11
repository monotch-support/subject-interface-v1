use std::env;
use std::io::{Read, Write};
use std::net::TcpStream;
use std::sync::{Arc, Mutex};
use std::sync::atomic::{AtomicBool, Ordering};
use std::thread;
use std::time::{Duration, SystemTime, UNIX_EPOCH};

use native_tls::{TlsConnector, TlsStream};
use rand::RngCore;
use serde::{Deserialize, Serialize};
use serde_json::Value;

// ======== CONSTANTS ========
const HEADER_PREFIX: [u8; 2] = [0xAA, 0xBB];
const SECURITY_MODE_NONE: &str = "NONE";
const SECURITY_MODE_TLS: &str = "TLSv1.2";

// ======== CONFIGURATION ========
fn get_env(key: &str, default: &str) -> String {
    env::var(key).unwrap_or_else(|_| default.to_string())
}

lazy_static::lazy_static! {
    static ref STREAMING_API_BASEURL: String = get_env("STREAMING_API_BASEURL", "https://localhost/api");
    static ref STREAMING_API_TLC_TOKEN: String = get_env("STREAMING_API_TLC_TOKEN", "your-tlc-auth-token");
    static ref STREAMING_API_BROKER_TOKEN: String = get_env("STREAMING_API_BROKER_TOKEN", "your-broker-auth-token");
    static ref STREAMING_API_DOMAIN: String = get_env("STREAMING_API_DOMAIN", "dev_001");
    static ref STREAMING_API_SECURITY_MODE: String = get_env("STREAMING_API_SECURITY_MODE", "TLSv1.2");
    static ref STREAMING_API_IDENTIFIER: String = get_env("STREAMING_API_IDENTIFIER", "sub00001");
}

// ======== UTILITY FUNCTIONS ========
fn log_msg(thread_name: &str, msg: &str) {
    log::info!("{}: {}", thread_name, msg);
}

fn log_json(thread_name: &str, message: &str, data: &Value) {
    log::info!("{}: {}: {}", thread_name, message, serde_json::to_string_pretty(data).unwrap());
}

fn as_hex_stream(data: &[u8]) -> String {
    format!("0x{}", data.iter().map(|b| format!("{:02X}", b)).collect::<String>())
}

fn current_timestamp() -> u64 {
    SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_millis() as u64
}

// ======== REST API FUNCTIONS ========
#[derive(Serialize)]
struct SessionRequest {
    domain: String,
    #[serde(rename = "type")]
    session_type: String,
    protocol: String,
    details: SessionDetails,
}

#[derive(Serialize)]
struct SessionDetails {
    #[serde(rename = "securityMode")]
    security_mode: String,
    #[serde(rename = "tlcIdentifiers")]
    tlc_identifiers: Vec<String>,
}

#[derive(Deserialize)]
struct SessionResponse {
    token: String,
    details: SessionResponseDetails,
}

#[derive(Deserialize)]
struct SessionResponseDetails {
    listener: ListenerDetails,
}

#[derive(Deserialize)]
struct ListenerDetails {
    host: String,
    port: u16,
}

async fn create_session(
    session_type: &str,
    token: &str,
    api_url: &str,
    security_mode: &str,
    identifier: &str,
    thread_name: &str,
) -> Result<(String, u16, String), Box<dyn std::error::Error + Send + Sync>> {
    let url = format!("{}/v1/sessions", api_url);
    
    let request_data = SessionRequest {
        domain: STREAMING_API_DOMAIN.clone(),
        session_type: session_type.to_string(),
        protocol: "TCPStreaming_Multiplex".to_string(),
        details: SessionDetails {
            security_mode: security_mode.to_string(),
            tlc_identifiers: vec![identifier.to_string()],
        },
    };
    
    let client = reqwest::Client::new();
    let response = client
        .post(&url)
        .header("X-Authorization", token)
        .header("Content-Type", "application/json")
        .json(&request_data)
        .send()
        .await?;
    
    if !response.status().is_success() {
        return Err(format!("Request failed with status code {}", response.status()).into());
    }
    
    let response_data: Value = response.json().await?;
    log_json(thread_name, "Session created successfully", &response_data);
    
    let session_response: SessionResponse = serde_json::from_value(response_data)?;
    
    log_msg(thread_name, &format!("Token: {}", session_response.token));
    log_msg(thread_name, &format!("Host: {}", session_response.details.listener.host));
    log_msg(thread_name, &format!("Port: {}", session_response.details.listener.port));
    
    Ok((
        session_response.details.listener.host,
        session_response.details.listener.port,
        session_response.token,
    ))
}

// ======== TCP STREAMING FUNCTIONS ========
enum Connection {
    Plain(TcpStream),
    Tls(TlsStream<TcpStream>),
}

impl Connection {
    fn read(&mut self, buf: &mut [u8]) -> std::io::Result<usize> {
        match self {
            Connection::Plain(stream) => stream.read(buf),
            Connection::Tls(stream) => stream.read(buf),
        }
    }
    
    fn write(&mut self, buf: &[u8]) -> std::io::Result<usize> {
        match self {
            Connection::Plain(stream) => stream.write(buf),
            Connection::Tls(stream) => stream.write(buf),
        }
    }
    
    fn set_nonblocking(&self, nonblocking: bool) -> std::io::Result<()> {
        match self {
            Connection::Plain(stream) => stream.set_nonblocking(nonblocking),
            Connection::Tls(stream) => stream.get_ref().set_nonblocking(nonblocking),
        }
    }
}

fn connect(host: &str, port: u16, use_tls: bool, thread_name: &str) -> Result<Connection, Box<dyn std::error::Error + Send + Sync>> {
    let stream = TcpStream::connect(format!("{}:{}", host, port))?;
    
    if use_tls {
        let connector = TlsConnector::new()?;
        let tls_stream = connector.connect(host, stream)?;
        
        log_msg(thread_name, &format!("Connected to {}:{} (TLS: {})", host, port, use_tls));
        log_msg(thread_name, "TLS handshake successful");
        
        Ok(Connection::Tls(tls_stream))
    } else {
        log_msg(thread_name, &format!("Connected to {}:{} (TLS: {})", host, port, use_tls));
        Ok(Connection::Plain(stream))
    }
}

fn handshake(conn: &mut Connection, thread_name: &str) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    let protocol_version_byte = [0x01];
    conn.write(&protocol_version_byte)?;
    
    let mut recv = [0u8; 1];
    conn.read(&mut recv)?;
    
    log_msg(thread_name, &format!("Received protocol version {}", as_hex_stream(&recv)));
    
    if recv[0] != 1 {
        return Err("Unsupported protocol version received".into());
    }
    
    Ok(())
}

fn write_datagram(conn: &mut Connection, datagram: &[u8], thread_name: &str) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    let mut header = Vec::new();
    header.extend_from_slice(&HEADER_PREFIX);
    header.extend_from_slice(&(datagram.len() as u16).to_be_bytes());
    
    let mut frame = header;
    frame.extend_from_slice(datagram);
    
    log_msg(thread_name, &format!("Writing frame {}", as_hex_stream(&frame)));
    
    conn.write(&frame)?;
    Ok(())
}

fn write_token(conn: &mut Connection, token: &str, thread_name: &str) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    log_msg(thread_name, &format!("Writing token {}", token));
    let mut datagram = vec![0x01];
    datagram.extend_from_slice(token.as_bytes());
    write_datagram(conn, &datagram, thread_name)
}

fn write_keepalive(conn: &mut Connection, thread_name: &str) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    log_msg(thread_name, "Writing keep alive");
    let datagram = [0x00];
    write_datagram(conn, &datagram, thread_name)
}

fn write_timestamp_response(
    conn: &mut Connection,
    timestamp_t0: u64,
    timestamp_t1: u64,
    thread_name: &str,
) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    let timestamp_t2 = current_timestamp();
    log_msg(
        thread_name,
        &format!("Writing timestamp response (t0: {}, t1: {}, t2: {})", timestamp_t0, timestamp_t1, timestamp_t2),
    );
    
    let mut datagram = vec![0x07];
    datagram.extend_from_slice(&timestamp_t0.to_be_bytes());
    datagram.extend_from_slice(&timestamp_t1.to_be_bytes());
    datagram.extend_from_slice(&timestamp_t2.to_be_bytes());
    
    write_datagram(conn, &datagram, thread_name)
}

fn write_payload_with_identifier(
    conn: &mut Connection,
    identifier: &str,
    payload_type: u8,
    payload: &[u8],
    thread_name: &str,
) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    log_msg(
        thread_name,
        &format!(
            "Writing payload with identifier (identifier: {}, payload_type: {}): {}",
            identifier,
            as_hex_stream(&[payload_type]),
            as_hex_stream(payload)
        ),
    );
    
    let mut datagram = vec![0x05];
    datagram.extend_from_slice(identifier.as_bytes());
    datagram.push(payload_type);
    datagram.extend_from_slice(&current_timestamp().to_be_bytes());
    datagram.extend_from_slice(payload);
    
    write_datagram(conn, &datagram, thread_name)
}

fn read_datagram(conn: &mut Connection, thread_name: &str) -> Result<Option<Vec<u8>>, Box<dyn std::error::Error + Send + Sync>> {
    conn.set_nonblocking(true)?;
    
    let mut header = [0u8; 4];
    match conn.read(&mut header) {
        Ok(0) => return Err("Socket disconnected".into()),
        Ok(n) if n != 4 => return Err("Received partial header (implementation does not support partial receives yet)".into()),
        Ok(_) => {},
        Err(e) if e.kind() == std::io::ErrorKind::WouldBlock => return Ok(None),
        Err(e) => return Err(e.into()),
    }
    
    log_msg(thread_name, &format!("Received header {}", as_hex_stream(&header)));
    
    if header[0..2] != HEADER_PREFIX {
        return Err(format!("Framing error: header prefix {} != {}", 
                          as_hex_stream(&header[0..2]), 
                          as_hex_stream(&HEADER_PREFIX)).into());
    }
    
    let size = u16::from_be_bytes([header[2], header[3]]) as usize;
    log_msg(thread_name, &format!("Trying to read {} bytes datagram", size));
    
    conn.set_nonblocking(false)?;
    let mut datagram = vec![0u8; size];
    conn.read(&mut datagram)?;
    
    log_msg(thread_name, &format!("Received datagram {}", as_hex_stream(&datagram)));
    
    Ok(Some(datagram))
}

fn handle_keepalive(_conn: &mut Connection, thread_name: &str) {
    log_msg(thread_name, "Keep alive received");
}

fn handle_bye(_conn: &mut Connection, datagram: &[u8], thread_name: &str) {
    log_msg(thread_name, "Bye received");
    let reason = String::from_utf8_lossy(&datagram[1..]);
    log_msg(thread_name, &format!("Bye reason: {}", reason));
}

fn handle_payload_with_identifier(
    _conn: &mut Connection,
    datagram: &[u8],
    payload_received_callback: &dyn Fn(&str, u8, u64, &[u8]),
    thread_name: &str,
) {
    log_msg(thread_name, "Payload with identifier received");
    let identifier = String::from_utf8_lossy(&datagram[1..9]);
    let payload_type = datagram[9];
    let origin_timestamp = u64::from_be_bytes([
        datagram[10], datagram[11], datagram[12], datagram[13],
        datagram[14], datagram[15], datagram[16], datagram[17],
    ]);
    let payload = &datagram[18..];
    
    log_msg(
        thread_name,
        &format!(
            "Payload received (identifier: {}, payload_type: {}, origin_timestamp: {}): {}",
            identifier,
            as_hex_stream(&[payload_type]),
            origin_timestamp,
            as_hex_stream(payload)
        ),
    );
    
    payload_received_callback(&identifier, payload_type, origin_timestamp, payload);
}

fn handle_timestamp_request(conn: &mut Connection, datagram: &[u8], thread_name: &str) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    log_msg(thread_name, "Timestamp request received");
    let timestamp_t0 = u64::from_be_bytes([
        datagram[1], datagram[2], datagram[3], datagram[4],
        datagram[5], datagram[6], datagram[7], datagram[8],
    ]);
    let timestamp_t1 = current_timestamp();
    log_msg(thread_name, &format!("Timestamp request delta: {}ms", timestamp_t1 - timestamp_t0));
    write_timestamp_response(conn, timestamp_t0, timestamp_t1, thread_name)
}

fn handle_datagram(
    conn: &mut Connection,
    datagram: &[u8],
    payload_received_callback: &dyn Fn(&str, u8, u64, &[u8]),
    thread_name: &str,
) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    if datagram.is_empty() {
        return Ok(());
    }
    
    match datagram[0] {
        0x00 => handle_keepalive(conn, thread_name),
        0x02 => handle_bye(conn, datagram, thread_name),
        0x05 => handle_payload_with_identifier(conn, datagram, payload_received_callback, thread_name),
        0x06 => handle_timestamp_request(conn, datagram, thread_name)?,
        _ => log_msg(thread_name, &format!("Unknown/unimplemented datagram type {} received", as_hex_stream(&[datagram[0]]))),
    }
    
    Ok(())
}

fn run_streaming_client(
    host: &str,
    port: u16,
    session_token: &str,
    use_tls: bool,
    payload_received_callback: Box<dyn Fn(&str, u8, u64, &[u8]) + Send>,
    loop_callback: Box<dyn Fn(&mut Connection) + Send>,
    thread_name: &str,
    shutdown_flag: Arc<AtomicBool>,
) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    let mut conn = connect(host, port, use_tls, thread_name)?;
    
    handshake(&mut conn, thread_name)?;
    write_token(&mut conn, session_token, thread_name)?;
    
    while !shutdown_flag.load(Ordering::Relaxed) {
        match read_datagram(&mut conn, thread_name)? {
            Some(datagram) => {
                handle_datagram(&mut conn, &datagram, &*payload_received_callback, thread_name)?;
            }
            None => {
                thread::sleep(Duration::from_millis(10));
            }
        }
        
        loop_callback(&mut conn);
    }
    
    log_msg(thread_name, "Shutdown signal received, terminating");
    Ok(())
}

// ======== PRODUCER ========
fn run_producer(shutdown_flag: Arc<AtomicBool>) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    let thread_name = "producer";
    
    // Step 1: Create a session using the REST API
    let rt = tokio::runtime::Runtime::new()?;
    let (host, port, token) = rt.block_on(create_session(
        "TLC",
        &STREAMING_API_TLC_TOKEN,
        &STREAMING_API_BASEURL,
        &STREAMING_API_SECURITY_MODE,
        &STREAMING_API_IDENTIFIER,
        thread_name,
    ))?;
    
    // Step 2: Connect to the TCP Streaming Node
    let last_write = Arc::new(Mutex::new(current_timestamp()));
    let identifier = STREAMING_API_IDENTIFIER.clone();
    
    let last_write_clone = last_write.clone();
    let write_callback = Box::new(move |conn: &mut Connection| {
        let mut last_write_guard = last_write_clone.lock().unwrap();
        let now = current_timestamp();
        // Write a random payload every second
        if now - *last_write_guard > 1000 {
            *last_write_guard = now;
            let mut payload = vec![0u8; 100];
            rand::thread_rng().fill_bytes(&mut payload);
            let _ = write_payload_with_identifier(conn, &identifier, 0x02, &payload, thread_name);
        }
    });
    
    let read_payload_callback = Box::new(move |identifier: &str, payload_type: u8, origin_timestamp: u64, payload: &[u8]| {
        log_msg(
            thread_name,
            &format!(
                "Producer received payload from {}: type={:#04x}, timestamp={}, size={}",
                identifier, payload_type, origin_timestamp, payload.len()
            ),
        );
    });
    
    let use_tls = *STREAMING_API_SECURITY_MODE == SECURITY_MODE_TLS;
    run_streaming_client(&host, port, &token, use_tls, read_payload_callback, write_callback, thread_name, shutdown_flag)?;
    
    Ok(())
}

// ======== CONSUMER ========
fn run_consumer(shutdown_flag: Arc<AtomicBool>) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    let thread_name = "consumer";
    
    // Step 1: Create a session using the REST API
    let rt = tokio::runtime::Runtime::new()?;
    let (host, port, token) = rt.block_on(create_session(
        "Broker",
        &STREAMING_API_BROKER_TOKEN,
        &STREAMING_API_BASEURL,
        &STREAMING_API_SECURITY_MODE,
        &STREAMING_API_IDENTIFIER,
        thread_name,
    ))?;
    
    // Step 2: Connect to the TCP Streaming Node
    let last_write = Arc::new(Mutex::new(current_timestamp()));
    
    let last_write_clone = last_write.clone();
    let write_callback = Box::new(move |conn: &mut Connection| {
        let mut last_write_guard = last_write_clone.lock().unwrap();
        let now = current_timestamp();
        // Write a keepalive every 5 seconds
        if now - *last_write_guard > 5000 {
            *last_write_guard = now;
            let _ = write_keepalive(conn, thread_name);
        }
    });
    
    let read_payload_callback = Box::new(move |identifier: &str, payload_type: u8, origin_timestamp: u64, payload: &[u8]| {
        let latency = current_timestamp() - origin_timestamp;
        log_msg(
            thread_name,
            &format!(
                "Consumer received payload from {}: type={:#04x}, timestamp={}, latency={}ms, size={}",
                identifier, payload_type, origin_timestamp, latency, payload.len()
            ),
        );
    });
    
    let use_tls = *STREAMING_API_SECURITY_MODE == SECURITY_MODE_TLS;
    run_streaming_client(&host, port, &token, use_tls, read_payload_callback, write_callback, thread_name, shutdown_flag)?;
    
    Ok(())
}

// ======== STARTUP AND RUN LOOP ========
fn dump_config() {
    log::info!("STREAMING_API_BASEURL: '{}'", *STREAMING_API_BASEURL);
    log::info!("STREAMING_API_TLC_TOKEN: '{}'", *STREAMING_API_TLC_TOKEN);
    log::info!("STREAMING_API_BROKER_TOKEN: '{}'", *STREAMING_API_BROKER_TOKEN);
    log::info!("STREAMING_API_DOMAIN: '{}'", *STREAMING_API_DOMAIN);
    log::info!("STREAMING_API_SECURITY_MODE: '{}'", *STREAMING_API_SECURITY_MODE);
}

fn configure_logging() {
    env_logger::init();
}

fn main() -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    configure_logging();
    dump_config();
    
    let shutdown_flag = Arc::new(AtomicBool::new(false));
    
    // Set up CTRL-C signal handler
    let shutdown_flag_signal = shutdown_flag.clone();
    ctrlc::set_handler(move || {
        log::info!("Received CTRL-C signal, shutting down...");
        shutdown_flag_signal.store(true, Ordering::Relaxed);
    }).expect("Error setting Ctrl-C handler");
    
    let shutdown_flag_producer = shutdown_flag.clone();
    let producer_handle = thread::spawn(|| {
        if let Err(e) = run_producer(shutdown_flag_producer) {
            log::error!("Producer error: {}", e);
        }
    });
    
    let shutdown_flag_consumer = shutdown_flag.clone();
    let consumer_handle = thread::spawn(|| {
        if let Err(e) = run_consumer(shutdown_flag_consumer) {
            log::error!("Consumer error: {}", e);
        }
    });
    
    producer_handle.join().map_err(|_| "Producer thread panicked")?;
    consumer_handle.join().map_err(|_| "Consumer thread panicked")?;
    
    log::info!("Application terminated gracefully");
    Ok(())
}