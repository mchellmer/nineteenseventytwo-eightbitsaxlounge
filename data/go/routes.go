package gofiles

import (
	"net/http"

	"github.com/go-chi/chi/v5"
)

func SetupRoutes(svc CouchService) http.Handler {
	r := chi.NewRouter()

	// Database-level CRUD (e.g., create or delete a database)
	r.Put("/data/{dbname}", CreateDbHandler(svc))
	// r.Delete("/data/{dbname}", DeleteDbHandler)
	// r.Get("/data/{dbname}", GetDbInfoHandler)

	// Document-level CRUD (in a specific database)
	r.Get("/data/{dbname}/{id}", GetDocHandler(svc))
	r.Post("/data/{dbname}", CreateDocHandler(svc))
	r.Put("/data/{dbname}/{id}", UpdateDocHandler(svc))
	r.Delete("/data/{dbname}/{id}", DeleteDocHandler(svc))

	return r
}
