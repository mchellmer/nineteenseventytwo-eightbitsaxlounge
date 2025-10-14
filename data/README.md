# Overview
A data layer consisting of go api, cluster service, and nginx ingress integrates with the existing db layer. The api service handles external requests via ingress on path /data/*. This is currently configured to handle integration with CouchDb instances.

# Requirements
- db layer deployed exposing CouchdDb instance via ClusterIp service 'http://db-service:5984' in appropriate namespace.

# Test
```
cd /date/go
go test -v
```