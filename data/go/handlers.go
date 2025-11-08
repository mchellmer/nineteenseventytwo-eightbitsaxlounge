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
	"log"
	"net/http"

	"github.com/go-chi/chi/v5"
)

// writeJSONError writes a consistent JSON error with service details.
func writeJSONError(w http.ResponseWriter, status int, code string, err error) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(map[string]string{
		"error":   code,
		"message": err.Error(),
	})
}

// GetDocumentByDatabaseNameAndDocumentIdHandler handles:
//
//	GET /data/{dbname}/{id}
//
// Looks up a document by id and writes it as JSON, or 404 if not found.
func GetDocumentByDatabaseNameAndDocumentIdHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		id := chi.URLParam(r, "id")
		doc, err := svc.GetDocumentByDatabaseNameAndDocumentId(dbname, id)
		if err != nil {
			log.Printf("get doc db=%s id=%s error=%v", dbname, id, err)
			writeJSONError(w, http.StatusNotFound, "document_not_found", err)
			return
		}
		json.NewEncoder(w).Encode(doc)
	}
}

// CreateDatabaseByNameHandler handles:
//
//	PUT /data/{dbname}
//
// Creates a database. On success responds with 201 Created.
func CreateDatabaseByNameHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		if err := svc.CreateDatabaseByName(dbname); err != nil {
			log.Printf("create db db=%s error=%v", dbname, err)
			writeJSONError(w, http.StatusInternalServerError, "create_database_failed", err)
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
func CreateDocumentByDatabaseNameHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		var doc map[string]interface{}
		if err := json.NewDecoder(r.Body).Decode(&doc); err != nil {
			writeJSONError(w, http.StatusBadRequest, "invalid_json", err)
			return
		}
		if err := svc.CreateDocumentByDatabaseName(dbname, doc); err != nil {
			log.Printf("create doc db=%s error=%v", dbname, err)
			writeJSONError(w, http.StatusInternalServerError, "create_document_failed", err)
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
func UpdateDocumentByDatabaseNameAndDocumentIdHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		id := chi.URLParam(r, "id")
		var doc map[string]interface{}
		if err := json.NewDecoder(r.Body).Decode(&doc); err != nil {
			writeJSONError(w, http.StatusBadRequest, "invalid_json", err)
			return
		}
		if err := svc.UpdateDocumentByDatabaseNameAndDocumentId(dbname, id, doc); err != nil {
			log.Printf("update doc db=%s id=%s error=%v", dbname, id, err)
			// Later: distinguish 409 conflict by parsing err.Error()
			writeJSONError(w, http.StatusInternalServerError, "update_document_failed", err)
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
func DeleteDocumentByDatabaseNameAndDocumentIdHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		id := chi.URLParam(r, "id")
		if err := svc.DeleteDocumentByDatabaseNameAndDocumentId(dbname, id); err != nil {
			log.Printf("delete doc db=%s id=%s error=%v", dbname, id, err)
			writeJSONError(w, http.StatusInternalServerError, "delete_document_failed", err)
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
func GetDatabaseByNameHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		dbInfo, err := svc.GetDatabaseByName(dbname)
		if err != nil {
			log.Printf("get db db=%s error=%v", dbname, err)
			writeJSONError(w, http.StatusNotFound, "database_not_found", err)
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
func DeleteDatabaseByNameHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		if err := svc.DeleteDatabaseByName(dbname); err != nil {
			log.Printf("delete db db=%s error=%v", dbname, err)
			writeJSONError(w, http.StatusInternalServerError, "delete_database_failed", err)
			return
		}
		w.WriteHeader(http.StatusOK)
	}
}

// GetDocumentsByDbNameHandler handles:
//
//	GET /data/{dbname}/docs
//
// Returns all documents in the database as a JSON array.
func GetDocumentsByDatabaseNameHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		dbname := chi.URLParam(r, "dbname")
		docs, err := svc.GetDocumentsByDatabaseName(dbname)
		if err != nil {
			log.Printf("list docs db=%s error=%v", dbname, err)
			writeJSONError(w, http.StatusInternalServerError, "list_documents_failed", err)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(docs)
	}
}
