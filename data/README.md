# Eightbitsaxlounge Data API

A Go-based REST API data layer that provides CouchDB integration through Kubernetes services with NGINX ingress routing.

## Architecture Overview

```
External Client → NGINX Ingress → K8s Service → Go API Pods → CouchDB
```

The data layer consists of:
- **Go REST API**: Handles HTTP requests and CouchDB operations
- **Kubernetes Service**: Load balances across API replicas (ClusterIP)
- **NGINX Ingress**: Routes external traffic on `/data/*` path
- **CouchDB Integration**: Connects to existing `db-service:5984`

## API Development
The API routes requests to the /data endpoint through appropriate handler and CRUD service operation. The service instantiated in main.go and injected in routes.go. A handler for each route decodes the url and calls the relevant CRUD service operation.

1. Define routes in routes.go e.g. GET/POST and link to handler
2. Create handler in handlers.go to implement route capturing url params
3. Create method in service interface couchservice.go
4. Implement method in service code couchdb.go
5. Add handler and service tests for new method

## API Routes

### Database Operations
| Method | Endpoint | Description |
|--------|----------|-------------|
| `PUT` | `/data/{dbname}` | Create a new database |

### Document Operations
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/data/{dbname}/{id}` | Get document by ID |
| `POST` | `/data/{dbname}` | Create new document |
| `PUT` | `/data/{dbname}/{id}` | Update existing document |
| `DELETE` | `/data/{dbname}/{id}` | Delete document by ID |

### Example Usage
```bash
# Create database
curl -X PUT http://<ingress-ip>/data/mydb

# Create document
curl -X POST http://<ingress-ip>/data/mydb \
  -H "Content-Type: application/json" \
  -d '{"name": "example", "value": 123}'

# Get document
curl -X GET http://<ingress-ip>/data/mydb/doc123

# Update document
curl -X PUT http://<ingress-ip>/data/mydb/doc123 \
  -H "Content-Type: application/json" \
  -d '{"_id": "doc123", "_rev": "1-abc", "name": "updated"}'

# Delete document
curl -X DELETE http://<ingress-ip>/data/mydb/doc123
```

## Requirements
- CouchDB layer deployed with ClusterIP service `db-service:5984`
- Kubernetes secret `secret-db-couchdb` containing CouchDB password
- NGINX ingress controller installed in cluster

## Deployment
Deploy using the provided Ansible playbook:
```bash
ansible-playbook data-go-couchdb.yaml
```

## Testing
```bash
cd data/go
go test -v
```