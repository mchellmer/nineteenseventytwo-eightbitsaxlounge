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
)

// SetupRoutes returns a configured chi.Router.
//   - Injects CRUD service to pass to handlers.
//   - Registers endpoints.
//
// Notes:
//   - chi path params are segment-based (e.g., /data/{dbname}/{id}).
//   - Handlers read params with chi.URLParam(r, "dbname") and "id".
//   - Kubernetes Ingress configured to route external /data/* paths to this api.
func SetupRoutes(svc CouchService) http.Handler {
	r := chi.NewRouter()

	// Database-level endpoints
	r.Put("/data/{dbname}", CreateDatabaseByNameHandler(svc))
	r.Get("/data/{dbname}", GetDatabaseByNameHandler(svc))
	r.Delete("/data/{dbname}", DeleteDatabaseByNameHandler(svc))

	// Document-level CRUD within a specific database
	// List all docs endpoint placed before {id} to avoid param capture
	r.Get("/data/{dbname}/docs", GetDocumentsByDatabaseNameHandler(svc))
	r.Get("/data/{dbname}/{id}", GetDocumentByDatabaseNameAndDocumentIdHandler(svc))
	r.Post("/data/{dbname}", CreateDocumentByDatabaseNameHandler(svc))
	r.Put("/data/{dbname}/{id}", UpdateDocumentByDatabaseNameAndDocumentIdHandler(svc))
	r.Delete("/data/{dbname}/{id}", DeleteDocumentByDatabaseNameAndDocumentIdHandler(svc))

	return r
}
