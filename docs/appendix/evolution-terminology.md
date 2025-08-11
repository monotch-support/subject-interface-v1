# Terminology Reference

## Legacy Terminology Mapping

The Subject Interface API uses legacy terminology from its original Traffic Light Controller (TLC) implementation for backward compatibility. The table below shows the mapping between legacy API terminology and the generic terminology that reflects the actual functionality:

| Legacy API Terminology | Generic Terminology | Functional Purpose |
|------------------------|------------------------|-------------------|
| **TLC** (entity) | **SUBJECT** | Any entity or bi-directional communication channel |
| **TLC** (session type) | **OWNER** | Session type for entities that own and manage subject data |
| **BROKER** (session type) | **PARTICIPANT** | Session type for data exchange and distribution |
| **MONITOR** (session type) | **LISTENER** | Session type for data observation and analytics |
| **TLC_ADMIN** (role) | **OWNER_ADMIN** | Administrative role for subject owners |
| **TLC_SYSTEM** (role) | **OWNER_SYSTEM** | Operational role for subject data production |
| **TLC_ANALYST** (role) | **OWNER_ANALYST** | Analytical role for subject data analysis |
| **BROKER_ADMIN** (role) | **PARTICIPANT_ADMIN** | Administrative role for data distribution |
| **BROKER_SYSTEM** (role) | **PARTICIPANT_SYSTEM** | Operational role for data distribution |
| **BROKER_ANALYST** (role) | **PARTICIPANT_ANALYST** | Analytical role for distribution analysis |
| **MONITOR_ADMIN** (role) | **LISTENER_ADMIN** | Administrative role for monitoring |
| **MONITOR_SYSTEM** (role) | **LISTENER_SYSTEM** | Operational role for data observation |
| **MONITOR_ANALYST** (role) | **LISTENER_ANALYST** | Analytical role for monitoring analysis |

## Legacy Entity Names in API

| Legacy API Entity | Actually Represents |
|------------|------------|
| **TLC** | Any connected subject (traffic controller, sensor, barrier, display, etc.) |
| **AuthorizationTLC** | Authorization scope limited to specific subjects |
| **TLC Registration** | Registration of any connected entity within a domain |