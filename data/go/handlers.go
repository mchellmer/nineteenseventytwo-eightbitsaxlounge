// Handlers implement the HTTP endpoints.
//
// Pattern used here:
//   - Each function returns an http.HandlerFunc closure that captures a CouchService CRUD service.
//   - Route params (e.g., {dbname}, {id}) are read with chi.URLParam.
//   - Request bodies are JSON-decoded, responses JSON-encoded where relevant.
//   - Handlers set appropriate HTTP status codes (e.g., 201 Created, 404 Not Found).
//
// See routes.go for the paths and HTTP methods that invoke these handlers.
// See couchdb.go for the CouchService implementation.
package gofiles

import (
	"encoding/json"
	"net/http"

	"github.com/go-chi/chi/v5"
)

// GetDocHandler handles:
//
//	GET /data/{dbname}/{id}
//
// Looks up a document by id and writes it as JSON, or 404 if not found.
func GetDocHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		id := chi.URLParam(r, "id")
		doc, err := svc.GetDoc(dbname, id)
		if err != nil {
			http.Error(w, "not found", http.StatusNotFound)
			return
		}
		json.NewEncoder(w).Encode(doc)
	}
}

// CreateDbHandler handles:
//
//	PUT /data/{dbname}
//
// Creates a database. On success responds with 201 Created.
func CreateDbHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		if err := svc.CreateDb(dbname); err != nil {
			http.Error(w, "failed to create db", http.StatusInternalServerError)
			return
		}
		w.WriteHeader(http.StatusCreated)
	}
}

// CreateDocHandler handles:
//
//	POST /data/{dbname}
//
// Creates a new document (server assigns id). Expects a JSON body.
// On success responds with 201 Created.
func CreateDocHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		var doc map[string]interface{}
		if err := json.NewDecoder(r.Body).Decode(&doc); err != nil {
			http.Error(w, "invalid json", http.StatusBadRequest)
			return
		}
		if err := svc.CreateDoc(dbname, doc); err != nil {
			http.Error(w, "failed to create doc", http.StatusInternalServerError)
			return
		}
		w.WriteHeader(http.StatusCreated)
	}
}

// UpdateDocHandler handles:
//
//	PUT /data/{dbname}/{id}
//
// Creates or updates a document with the provided id. Expects a JSON body.
// Responds with 200 OK on success.
func UpdateDocHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		id := chi.URLParam(r, "id")
		var doc map[string]interface{}
		if err := json.NewDecoder(r.Body).Decode(&doc); err != nil {
			http.Error(w, "invalid json", http.StatusBadRequest)
			return
		}
		if err := svc.UpdateDoc(dbname, id, doc); err != nil {
			http.Error(w, "failed to update doc", http.StatusInternalServerError)
			return
		}
		w.WriteHeader(http.StatusOK)
	}
}

// DeleteDocHandler handles:
//
//	DELETE /data/{dbname}/{id}
//
// Deletes a document by id. Responds with 200 OK on success.
func DeleteDocHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		id := chi.URLParam(r, "id")
		if err := svc.DeleteDoc(dbname, id); err != nil {
			http.Error(w, "failed to delete doc", http.StatusInternalServerError)
			return
		}
		w.WriteHeader(http.StatusOK)
	}
}

// GetDbHandler handles:
//
//	GET /data/{dbname}
//
// Returns basic database info as JSON, or 404 if the database is not found.
func GetDbHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		dbInfo, err := svc.GetDb(dbname)
		if err != nil {
			http.Error(w, "not found", http.StatusNotFound)
			return
		}
		json.NewEncoder(w).Encode(dbInfo)
	}
}

// DeleteDbHandler handles:
//
//	DELETE /data/{dbname}
//
// Deletes a database. Responds with 200 OK on success.
func DeleteDbHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		if err := svc.DeleteDb(dbname); err != nil {
			http.Error(w, "failed to delete db", http.StatusInternalServerError)
			return
		}
		w.WriteHeader(http.StatusOK)
	}
}
