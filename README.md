# Load Balancer Test Solution

## Problem Statement

It's 1999. You, a software engineer working at a rapidly growing scale-up.

The company has outgrown its start-up era, single server setup. Things are starting to fail rapidly. You are tasked with designing and building a software-based load balancer to allow multiple machines to handle the load.

## Solution Overview

This is a basic, software-based **Layer 4 TCP load balancer** implementation in C# (.NET 8) with the following capabilities:

### Core Features
- **Accept traffic from many clients** - Handles multiple concurrent client connections
- **Balance traffic across multiple backend services** - Distributes load using configurable routing strategies
- **Remove services if they go offline** - Health checking with automatic failover
- **Routing Strategies** - Round Robin and Least Connections algorithms
- **Health Monitoring** - Periodic health checks with configurable thresholds
- **Connection Management** - Proper connection tracking and cleanup
- **Logging & Monitoring** - Comprehensive logging and periodic status summaries

### Architecture
```
[Clients] → [Load Balancer:9000] → [Backend Pool: 7001, 7002, 7003]
                                    ↓
                              [TCP Echo Servers]
```

The load balancer operates as a transparent TCP proxy, accepting client connections and forwarding them to healthy backend services using the configured routing strategy.

## Project Structure

- **`loadbalancer-test/`** - Main load balancer application
- **`tcp-echo-server/`** - Simple TCP echo server for testing backends
- **`loadbalancer-test.Tests/`** - Unit tests

## Configuration

Configuration is managed through `appsettings.json`:

```json
{
  "LoadBalancer": {
    "Listener": {
      "Ip": "0.0.0.0",      // Listen on all interfaces
      "Port": 9000           // Frontend port for clients
    },
    "Backends": [
      { "Host": "127.0.0.1", "Port": 7001 },
      { "Host": "127.0.0.1", "Port": 7002 }, 
      { "Host": "127.0.0.1", "Port": 7003 }
    ],
    "HealthCheck": {
      "IntervalSeconds": 3,      // Check every 3 seconds
      "TimeoutSeconds": 2,       // 2 second timeout
      "FailureThreshold": 2      // Mark unhealthy after 2 failures
    },
    "RoutingStrategy": "RoundRobin"  // or "LeastConnections"
  }
}
```

## Quick Start Guide

### 1. Start Backend TCP Echo Servers

Based on the configuration in `appsettings.json`, start the TCP echo servers on the configured backend ports:

```powershell
# Terminal 1 - Backend Server 1
cd tcp-echo-server
dotnet run -- 7001

# Terminal 2 - Backend Server 2  
cd tcp-echo-server
dotnet run -- 7002

# Terminal 3 - Backend Server 3
cd tcp-echo-server
dotnet run -- 7003
```

You should see output like:
```
Echo server listening on 127.0.0.1:7001
Echo server listening on 127.0.0.1:7002  
Echo server listening on 127.0.0.1:7003
```

### 2. Start the Load Balancer

```powershell
# Terminal 4 - Load Balancer
cd loadbalancer-test
dotnet run
```

You should see output like:
```
Load balancer listening on 0.0.0.0:9000 using RoundRobin
```

### 3. Test the Setup

#### Test Handshake Connection
```powershell
Test-NetConnection 127.0.0.1 -Port 9000
```

Expected output:
```
ComputerName     : 127.0.0.1
RemoteAddress    : 127.0.0.1  
RemotePort       : 9000
InterfaceAlias   : Loopback Pseudo-Interface 1
SourceAddress    : 127.0.0.1
TcpTestSucceeded : True
```

#### Test Data Echo (using telnet or nc)
```powershell
# If you have telnet enabled
telnet 127.0.0.1 9000

# Type some text and it should echo back through the load balancer
```

#### Client connection test script (PowerShell)
Use the provided script to open a TCP connection to the load balancer and verify the backend banner and echo round-trip.

```powershell
# Open powershell or terminal to the repo root and run 
`./scripts/client-connection-test.ps1`  

Expected output includes the backend banner and echoed text, for example:

```
Connected to: echo-backend 127.0.0.1:7002
Server echoed: hello through load balancer connected to echo-backend 127.0.0.1:7003
```

## Testing Load Balancing

1. Start multiple backend servers as shown above
2. Start the load balancer  
3. Make multiple connections and observe the load balancer logs to see traffic being distributed across backends
4. Stop one of the backend servers to test failover behavior
5. Monitor the health check logs to see unhealthy backends being removed from rotation

