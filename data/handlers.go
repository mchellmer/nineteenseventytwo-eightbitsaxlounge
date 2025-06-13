package main

import (
	"encoding/json"
	"net/http"

	"github.com/go-chi/chi/v5"
)

func GetDoc(w http.ResponseWriter, r *http.Request) {
	dbname := chi.URLParam(r, "dbname")
	id := chi.URLParam(r, "id")
	doc, err := GetDocHandler(dbname, id)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	json.NewEncoder(w).Encode(doc)
}

func CreateDb(w http.ResponseWriter, r *http.Request) {
	dbname := chi.URLParam(r, "dbname")
	err := CreateDbHandler(dbname)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	w.WriteHeader(http.StatusCreated)
}

func CreateDoc(w http.ResponseWriter, r *http.Request) {
	dbname := chi.URLParam(r, "dbname")
	var doc map[string]interface{}
	json.NewDecoder(r.Body).Decode(&doc)
	err := CreateDocHandler(dbname, doc)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	w.WriteHeader(http.StatusCreated)
}

func UpdateDoc(w http.ResponseWriter, r *http.Request) {
	dbname := chi.URLParam(r, "dbname")
	id := chi.URLParam(r, "id")
	var doc map[string]interface{}
	json.NewDecoder(r.Body).Decode(&doc)
	err := UpdateDocHandler(dbname, id, doc)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	w.WriteHeader(http.StatusOK)
}

func DeleteDoc(w http.ResponseWriter, r *http.Request) {
	dbname := chi.URLParam(r, "dbname")
	id := chi.URLParam(r, "id")
	err := DeleteDocHandler(dbname, id)
	if err != nil {
		http.Error(w, err.Error(), 500)
		return
	}
	w.WriteHeader(http.StatusOK)
}
