// HTTP routing using the chi router.
//
// How it works:
//   - A chi.Router (compatible with net/http) is created and returned.
//   - chi matches requests by HTTP method + path, then calls the handler.
//   - Handlers read params via chi.URLParam and delegate to a service.
package gofiles

import (
	"net/http"

	"github.com/go-chi/chi/v5"
	"github.com/go-chi/chi/v5/middleware"
)

// SetupRoutes returns a configured chi.Router.
//   - Injects CRUD service to pass to handlers.
//   - Registers endpoints.
//
// Notes:
//   - chi path params are segment-based (e.g., /{dbname}/{id}).
//   - Handlers read params with chi.URLParam(r, "dbname") and "id".
//   - Kubernetes Ingress now uses host-based routing (data[-dev].<ip>.sslip.io) and
//     sends all requests at path root '/' here, so no /data prefix is required.
func SetupRoutes(svc CouchService) http.Handler {
	r := chi.NewRouter()

	// Logging middleware - logs all requests
	r.Use(middleware.Logger)
	r.Use(middleware.Recoverer)

	// Health endpoint for Kubernetes probes
	r.Get("/health", HealthCheckHandler(svc))

	// Database-level endpoints (host root based)
	r.Put("/{dbname}", CreateDatabaseByNameHandler(svc))
	r.Get("/{dbname}", GetDatabaseByNameHandler(svc))
	r.Delete("/{dbname}", DeleteDatabaseByNameHandler(svc))

	// Document-level CRUD within a specific database
	// List all docs endpoint placed before {id} to avoid param capture
	r.Get("/{dbname}/docs", GetDocumentsByDatabaseNameHandler(svc))
	r.Get("/{dbname}/{id}", GetDocumentByDatabaseNameAndDocumentIdHandler(svc))
	r.Post("/{dbname}", CreateDocumentByDatabaseNameHandler(svc))
	r.Put("/{dbname}/{id}", UpdateDocumentByDatabaseNameAndDocumentIdHandler(svc))
	r.Delete("/{dbname}/{id}", DeleteDocumentByDatabaseNameAndDocumentIdHandler(svc))

	return r
}
