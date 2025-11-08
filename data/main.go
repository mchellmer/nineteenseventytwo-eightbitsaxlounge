// Construct the CouchDB service, register HTTP routes, and serve HTTP
// on port 8080. See go/ for handlers, routes, and service implementations.
package main

import (
	gofiles "go-couchdb-api/go"
	"log"
	"net/http"
)

// Wire the service into the HTTP router and starts the HTTP server.
func main() {
	// Instantiate the CouchDB-backed service.
	svc := &gofiles.ProdCouchService{}

	// Register all API routes, injecting the service into handlers.
	r := gofiles.SetupRoutes(svc)

	// Start the HTTP server and block until it exits.
	log.Println("Starting server on :8080...")
	err := http.ListenAndServe(":8080", r)
	if err != nil {
		log.Fatalf("Server failed: %s", err)
	}
}
