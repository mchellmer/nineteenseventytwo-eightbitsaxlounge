package main

import (
	gofiles "go-couchdb-api/go"
	"log"
	"net/http"
)

func main() {
	svc := &gofiles.ProdCouchService{}

	r := gofiles.SetupRoutes(svc)

	log.Println("Starting server on :8080...")
	err := http.ListenAndServe(":8080", r)
	if err != nil {
		log.Fatalf("Server failed: %s", err)
	}
}
