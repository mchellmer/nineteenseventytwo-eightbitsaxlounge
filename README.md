# EightBitSaxLounge

A deliberately overengineered Kubernetes-based platform for live music streaming with interactive audience controls. Built for fun, learning, and exploring DevOps/cloud-native patterns.

## What is 8 Bit Sax Lounge?

8 Bit Sax Lounge (8bsl) is a live music stream where viewers can interact with the performance by controlling audio effects in real-time through chat commands. This repository contains the full stack of services powering that experience.

**Is this overengineered?** Absolutely, and intentionally so! This project serves as a hands-on learning platform for Kubernetes, microservices, CI/CD, infrastructure as code, and cloud-native development patterns. What could be a simple script is instead a distributed system running across multiple Raspberry Pis and a PC.

## Architecture Overview

The system is composed of five main layers, each running as containerized services in a Kubernetes cluster:

### **UI Layer** ([ui/](ui/))
Python-based Twitch chatbot that monitors chat and responds to viewer commands. Supports case-insensitive commands like `!engine`, `!time`, `!predelay` to control audio effects. Communicates with the MIDI layer to translate chat commands into hardware control signals.

### **MIDI Layer** ([midi/](midi/))
.NET Minimal API that manages MIDI device communication and abstracts hardware control. Provides RESTful endpoints for controlling audio equipment (currently Ventris Dual Reverb). Handles device state management and MIDI message formatting.

### **Data Layer** ([data/](data/))
Go-based data service providing a RESTful API for MIDI device configurations, presets, and state. Acts as the application's data access layer, abstracting CouchDB operations for other services.

### **DB Layer** ([db/](db/))
CouchDB instance serving as the source of truth for device configurations, presets, and application state. Ensures consistent state across UI, MIDI devices, and chat interactions.

### **Server Layer** ([server/](server/))
Ansible-based infrastructure as code managing the Kubernetes cluster across Raspberry Pi nodes and a PC. Handles cluster provisioning, configuration, deployments, and maintenance.

For architectural diagrams and visual overviews, see the [diagrams/](diagrams/) folder.

## Infrastructure

- **Kubernetes Cluster**: Self-hosted K8s cluster
- **Hardware**: 
  - Multiple Raspberry Pi nodes (ARM64)
  - PC node (x86_64) for MIDI hardware connectivity
- **CI/CD**: GitHub Actions with self-hosted runners
- **Deployment**: Ansible playbooks + Kubernetes manifests
- **Networking**: Ingress-nginx, MetalLB for load balancing
- **Container Registry**: GitHub Container Registry (ghcr.io)

Each layer has its own build/test/deploy pipeline, with releases triggered by version.txt updates.

## Getting Started

Each layer has detailed documentation in its respective README:
- [UI Layer Documentation](ui/README.md)
- [MIDI Layer Documentation](midi/README.md)
- [Data Layer Documentation](data/README.md)
- [DB Layer Documentation](db/README.md)
- [Server Layer Documentation](server/README.md)

## Feature Roadmap
UI Layer
- ensure dev/prod services not both accessible at once
- service to update obs resources

Midi Layer
- remove device dependency on PC connection
- make proxy mode more modular

Data Layer
- requests for midi details handled with appropriate response

Db layer
- source of true state -> UI and device track

Monitoring layer
- grafana
  - all containers enforced probes for health monitoring
  - dashboard - service versions/states, device state
  - vulnerabilities
  - logging

Server layer
- service mesh for finer tuned monitoring
- shared ansible role for common work among layers
- handle k8s updates

CI/CD
- linting and scanning - on a schedule and isolated to a single runner so it doesn't block build/release
- maintain scripts separately rather than inline
- add flags for skipping tasks e.g. only deploy config, not app etc

Security
- end to end review
- security scanning and monitoring integrated with pipelines
- secrets managed by ci/cd service vs ansible secrets?

Cloud replication
- capability to spin-up/down infrastructure outside of midi layer in AWS/Azure/gcp

State Layer?
- unified state to ensure consistent: UI and db match midi and chat states
- default state stored and applied as needed
- enforce db as true state of UI and midi device