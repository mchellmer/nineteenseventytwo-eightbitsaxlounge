package gofiles

import (
	"encoding/json"
	"net/http"

	"github.com/go-chi/chi/v5"
)

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
