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

// writeJSONError writes a consistent JSON error with service details.
func writeJSONError(w http.ResponseWriter, status int, code string, err error) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(map[string]string{
		"error":   code,
		"message": err.Error(),
	})
}

// HealthCheckHandler handles:
//
//	GET /health
//
// Returns 200 OK if service is healthy and can connect to CouchDB.
// Used by Kubernetes liveness and readiness probes.
func HealthCheckHandler(svc CouchService) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		// Check if we can connect to CouchDB using _up endpoint
		err := svc.CheckHealth()
		if err != nil {
			w.WriteHeader(http.StatusServiceUnavailable)
			w.Write([]byte("Service Unavailable\n"))
			return
		}

		w.WriteHeader(http.StatusOK)
		w.Write([]byte("OK\n"))
	}
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
		correlationID := GetCorrelationID(r.Context())
		doc, err := svc.GetDocumentByDatabaseNameAndDocumentId(dbname, id)
		if err != nil {
			Error("operation=get_document db=%s id=%s status=error error=%v correlationID=%s", dbname, id, err, correlationID)
			writeJSONError(w, http.StatusNotFound, "document_not_found", err)
			return
		}
		Info("operation=get_document db=%s id=%s status=success correlationID=%s", dbname, id, correlationID)
		if err := json.NewEncoder(w).Encode(doc); err != nil {
			Error("operation=encode_response status=error error=%v correlationID=%s", err, correlationID)
		}
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
		correlationID := GetCorrelationID(r.Context())
		if err := svc.CreateDatabaseByName(dbname); err != nil {
			Error("operation=create_database db=%s status=error error=%v correlationID=%s", dbname, err, correlationID)
			writeJSONError(w, http.StatusInternalServerError, "create_database_failed", err)
			return
		}
		Info("operation=create_database db=%s status=success correlationID=%s", dbname, correlationID)
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
		correlationID := GetCorrelationID(r.Context())
		var doc map[string]interface{}
		if err := json.NewDecoder(r.Body).Decode(&doc); err != nil {
			writeJSONError(w, http.StatusBadRequest, "invalid_json", err)
			return
		}
		if err := svc.CreateDocumentByDatabaseName(dbname, doc); err != nil {
			Error("operation=create_document db=%s status=error error=%v correlationID=%s", dbname, err, correlationID)
			writeJSONError(w, http.StatusInternalServerError, "create_document_failed", err)
			return
		}
		Info("operation=create_document db=%s status=success correlationID=%s", dbname, correlationID)
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
		correlationID := GetCorrelationID(r.Context())
		var doc map[string]interface{}
		if err := json.NewDecoder(r.Body).Decode(&doc); err != nil {
			writeJSONError(w, http.StatusBadRequest, "invalid_json", err)
			return
		}
		if err := svc.UpdateDocumentByDatabaseNameAndDocumentId(dbname, id, doc); err != nil {
			Error("operation=update_document db=%s id=%s status=error error=%v correlationID=%s", dbname, id, err, correlationID)
			// Later: distinguish 409 conflict by parsing err.Error()
			writeJSONError(w, http.StatusInternalServerError, "update_document_failed", err)
			return
		}
		Info("operation=update_document db=%s id=%s status=success correlationID=%s", dbname, id, correlationID)
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
		correlationID := GetCorrelationID(r.Context())
		if err := svc.DeleteDocumentByDatabaseNameAndDocumentId(dbname, id); err != nil {
			Error("operation=delete_document db=%s id=%s status=error error=%v correlationID=%s", dbname, id, err, correlationID)
			writeJSONError(w, http.StatusInternalServerError, "delete_document_failed", err)
			return
		}
		Info("operation=delete_document db=%s id=%s status=success correlationID=%s", dbname, id, correlationID)
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
		correlationID := GetCorrelationID(r.Context())
		dbInfo, err := svc.GetDatabaseByName(dbname)
		if err != nil {
			Error("operation=get_database db=%s status=error error=%v correlationID=%s", dbname, err, correlationID)
			writeJSONError(w, http.StatusNotFound, "database_not_found", err)
			return
		}
		Info("operation=get_database db=%s status=success correlationID=%s", dbname, correlationID)
		if err := json.NewEncoder(w).Encode(dbInfo); err != nil {
			Error("operation=encode_response status=error error=%v correlationID=%s", err, correlationID)
		}
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
		correlationID := GetCorrelationID(r.Context())
		if err := svc.DeleteDatabaseByName(dbname); err != nil {
			Error("operation=delete_database db=%s status=error error=%v correlationID=%s", dbname, err, correlationID)
			writeJSONError(w, http.StatusInternalServerError, "delete_database_failed", err)
			return
		}
		Info("operation=delete_database db=%s status=success correlationID=%s", dbname, correlationID)
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
		correlationID := GetCorrelationID(r.Context())
		docs, err := svc.GetDocumentsByDatabaseName(dbname)
		if err != nil {
			Error("operation=list_documents db=%s status=error error=%v correlationID=%s", dbname, err, correlationID)
			writeJSONError(w, http.StatusInternalServerError, "list_documents_failed", err)
			return
		}
		Info("operation=list_documents db=%s status=success correlationID=%s", dbname, correlationID)
		w.Header().Set("Content-Type", "application/json")
		if err := json.NewEncoder(w).Encode(docs); err != nil {
			Error("operation=encode_response status=error error=%v correlationID=%s", err, correlationID)
		}
	}
}
