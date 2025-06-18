package gofiles

import (
	"encoding/json"
	"net/http"

	"github.com/go-chi/chi/v5"
)

func GetDocHandler(w http.ResponseWriter, r *http.Request) {
	dbname := chi.URLParam(r, "dbname")
	id := chi.URLParam(r, "id")
	doc, err := GetDoc(dbname, id)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	json.NewEncoder(w).Encode(doc)
}

func CreateDbHandler(w http.ResponseWriter, r *http.Request) {
	dbname := chi.URLParam(r, "dbname")
	err := CreateDb(dbname)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	w.WriteHeader(http.StatusCreated)
}

func CreateDocHandler(w http.ResponseWriter, r *http.Request) {
	dbname := chi.URLParam(r, "dbname")
	var doc map[string]interface{}
	json.NewDecoder(r.Body).Decode(&doc)
	err := CreateDoc(dbname, doc)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	w.WriteHeader(http.StatusCreated)
}

func UpdateDocHandler(w http.ResponseWriter, r *http.Request) {
	dbname := chi.URLParam(r, "dbname")
	id := chi.URLParam(r, "id")
	var doc map[string]interface{}
	json.NewDecoder(r.Body).Decode(&doc)
	err := UpdateDoc(dbname, id, doc)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	w.WriteHeader(http.StatusOK)
}

func DeleteDocHandler(w http.ResponseWriter, r *http.Request) {
	dbname := chi.URLParam(r, "dbname")
	id := chi.URLParam(r, "id")
	err := DeleteDoc(dbname, id)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	w.WriteHeader(http.StatusOK)
}
