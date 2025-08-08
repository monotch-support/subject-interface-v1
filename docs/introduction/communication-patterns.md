# Communication Patterns

The Subject Interface supports two primary communication patterns that accommodate different C-ITS deployment scenarios and system topologies.

## Roadside to Central Communication

Direct communication between roadside infrastructure and central management systems forms the foundation of C-ITS deployments. This pattern supports bidirectional data exchange between field devices and central operations centers.

| Direction              | Examples                                                                |
| ---------------------- | ----------------------------------------------------------------------- |
| **Roadside → Central** | Signal states, detection data, sensor measurements, operational status  |
| **Central → Roadside** | Commands, timing updates, configuration changes, operational parameters |

## Central to Central Communication

Communication between central systems, service providers, and management platforms supports data sharing and coordination across organizational boundaries.

| Scenario                              | Examples                                                                                               |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| **Service Provider Integration**      | Data feeds for navigation, routing, and traveler information services                                  |
| **Multi-jurisdictional Coordination** | Cross-boundary traffic management, incident coordination                                               |
| **System-to-System Integration**      | Data exchange with external systems and platforms for visualization, data collection, analysis, etc... |

# Communication Characteristics

Both patterns support:

- Continuous bidirectional streaming
- Multiple concurrent connections
- Role-based access control
- Configurable security modes
- Message-agnostic payloads

# Application Domains

The communication patterns support various C-ITS applications, for example:

| Domain | Use Cases |
|--------|----------|
| **Traffic Management** | Signal optimization, incident response, transit priority |
| **Safety Applications** | Collision avoidance, vulnerable user protection, emergency preemption |
| **Mobility Services** | Green Light Optimal Speed Advisory (GLOSA), traffic information, route optimization |
| **Infrastructure Integration** | Smart city platforms, environmental monitoring, emergency coordination |