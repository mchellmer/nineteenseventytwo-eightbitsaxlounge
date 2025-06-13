package main

import (
	"net/http"

	"github.com/go-chi/chi/v5"
)

func SetupRoutes() http.Handler {
	r := chi.NewRouter()

	// Database-level CRUD (e.g., create or delete a database)
	r.Post("/data/{dbname}", CreateDb)
	// r.Delete("/data/{dbname}", DeleteDbHandler)
	// r.Get("/data/{dbname}", GetDbInfoHandler)

	// Document-level CRUD (in a specific database)
	r.Get("/data/{dbname}/{id}", GetDoc)
	r.Post("/data/{dbname}", CreateDoc)
	r.Put("/data/{dbname}/{id}", UpdateDoc)
	r.Delete("/data/{dbname}/{id}", DeleteDoc)

	return r
}
