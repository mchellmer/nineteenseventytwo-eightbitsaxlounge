package gofiles

import (
	"net/http"

	"github.com/go-chi/chi/v5"
)

func SetupRoutes(svc CouchService) http.Handler {
	r := chi.NewRouter()

	// Database-level CRUD (e.g., create or delete a database)
	// r.Post("/data/{dbname}", CreateDbHandler)
	// r.Delete("/data/{dbname}", DeleteDbHandler)
	// r.Get("/data/{dbname}", GetDbInfoHandler)

	// Document-level CRUD (in a specific database)
	r.Get("/data/{dbname}/{id}", GetDocHandler(svc))
	// r.Post("/data/{dbname}", CreateDocHandler)
	// r.Put("/data/{dbname}/{id}", UpdateDocHandler)
	// r.Delete("/data/{dbname}/{id}", DeleteDocHandler)

	return r
}
