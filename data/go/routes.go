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
	r.Put("/data/{dbname}", CreateDbHandler(svc))
	r.Get("/data/{dbname}", GetDbHandler(svc))
	r.Delete("/data/{dbname}", DeleteDbHandler(svc))

	// Document-level CRUD within a specific database
	r.Get("/data/{dbname}/{id}", GetDocHandler(svc))
	r.Post("/data/{dbname}", CreateDocHandler(svc))
	r.Put("/data/{dbname}/{id}", UpdateDocHandler(svc))
	r.Delete("/data/{dbname}/{id}", DeleteDocHandler(svc))

	return r
}
