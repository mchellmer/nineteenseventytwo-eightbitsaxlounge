// Service implementing CRUD operations against CouchDB over HTTP.
//
// General structure:
//   - Each method constructs the CouchDB REST URL (using couchURL + path).
//   - It sends the appropriate HTTP method (GET/POST/PUT/DELETE).
//   - It checks the expected status code and decodes/returns JSON when needed.
//   - Errors include the HTTP status and response body to aid diagnostics.
//
// Configuration/testing:
//   - httpClient is a package-level var so tests can replace it with a mock.
//   - couchURL is built from environment variables: COUCHDB_USER,
//     COUCHDB_PASSWORD, COUCHDB_ENDPOINT.
package gofiles

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
)

var httpClient = http.DefaultClient

type ProdCouchService struct{}

// GetDoc performs: GET /{dbname}/{id}
// Returns the JSON-decoded document on 200 OK; otherwise an error containing
// status and body. The map includes CouchDB metadata fields like _id and _rev.
func (s *ProdCouchService) GetDoc(dbname string, id string) (map[string]interface{}, error) {
	docURL := fmt.Sprintf("%s/%s/%s", couchURL(), dbname, id)
	resp, err := httpClient.Get(fmt.Sprintf("%s/%s", docURL, id))
	if err != nil {
		return nil, fmt.Errorf("failed to send GET request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("GET request failed with status %d: %s", resp.StatusCode, string(body))
	}

	var result map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, fmt.Errorf("failed to decode response body: %w", err)
	}
	return result, nil
}

// CreateDoc performs: POST /{dbname}
// Expects a JSON document in the request body; responds with 201 Created on
// success. Returns error with status/body if creation fails.
func (s *ProdCouchService) CreateDoc(dbname string, doc map[string]interface{}) error {
	b, err := json.Marshal(doc)
	if err != nil {
		return fmt.Errorf("failed to marshal document: %w", err)
	}

	dbURL := fmt.Sprintf("%s/%s", couchURL(), dbname)

	req, err := http.NewRequest(http.MethodPost, dbURL, bytes.NewReader(b))
	if err != nil {
		return fmt.Errorf("failed to create POST request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send POST request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusCreated {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("POST request failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

// UpdateDoc performs: PUT /{dbname}/{id}
// Expects a JSON document in the request body; responds with 200 OK on
// success. The caller should include the correct _rev within the document if
// using CouchDB's MVCC (not enforced here).
func (s *ProdCouchService) UpdateDoc(dbname string, id string, doc map[string]interface{}) error {
	b, err := json.Marshal(doc)
	if err != nil {
		return fmt.Errorf("failed to marshal document: %w", err)
	}

	dbURL := fmt.Sprintf("%s/%s", couchURL(), dbname)

	req, err := http.NewRequest(http.MethodPut, fmt.Sprintf("%s/%s", dbURL, id), bytes.NewReader(b))
	if err != nil {
		return fmt.Errorf("failed to create PUT request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send PUT request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("PUT request failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

// DeleteDoc performs: DELETE /{dbname}/{id}?rev={rev}
// It first fetches the doc to retrieve its current _rev, then issues the
// DELETE with that revision. Returns 200 OK on success.
func (s *ProdCouchService) DeleteDoc(dbname string, id string) error {
	doc, err := s.GetDoc(dbname, id)
	if err != nil {
		return fmt.Errorf("failed to retrieve document for deletion: %w", err)
	}

	rev, ok := doc["_rev"].(string)
	if !ok {
		return fmt.Errorf("document does not contain a valid _rev field")
	}

	dbURL := fmt.Sprintf("%s/%s", couchURL(), dbname)

	url := fmt.Sprintf("%s/%s?rev=%s", dbURL, id, rev)

	req, err := http.NewRequest(http.MethodDelete, url, nil)
	if err != nil {
		return fmt.Errorf("failed to create DELETE request: %w", err)
	}

	resp, err := httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send DELETE request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("DELETE request failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

// GetDb performs: GET /{dbname}
// Returns database info (e.g., doc count) on 200 OK; otherwise error.
func (s *ProdCouchService) GetDb(dbName string) (map[string]interface{}, error) {
	req, err := http.NewRequest(http.MethodGet, fmt.Sprintf("%s/%s", couchURL(), dbName), nil)
	if err != nil {
		return nil, fmt.Errorf("failed to create GET request: %w", err)
	}

	resp, err := httpClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("failed to send GET request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("GET request failed with status %d: %s", resp.StatusCode, string(body))
	}

	var result map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, fmt.Errorf("failed to decode response body: %w", err)
	}
	return result, nil
}

// CreateDb performs: PUT /{dbname}
// Treats 201 Created as success, and 412 Precondition Failed (already exists)
// as a no-op success to keep the operation idempotent.
func (s *ProdCouchService) CreateDb(dbName string) error {
	req, err := http.NewRequest(http.MethodPut, fmt.Sprintf("%s/%s", couchURL(), dbName), nil)
	if err != nil {
		return fmt.Errorf("failed to create PUT request: %w", err)
	}

	resp, err := httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send PUT request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusCreated && resp.StatusCode != http.StatusPreconditionFailed {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("failed to create database, status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

// DeleteDb performs: DELETE /{dbname}
// Treats 200 OK as success. Returns error for other status codes.
func (s *ProdCouchService) DeleteDb(dbName string) error {
	req, err := http.NewRequest(http.MethodDelete, fmt.Sprintf("%s/%s", couchURL(), dbName), nil)
	if err != nil {
		return fmt.Errorf("failed to create DELETE request: %w", err)
	}

	resp, err := httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send DELETE request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("DELETE request failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

// couchURL builds the base CouchDB URL with embedded basic-auth credentials.
//
//	http://COUCHDB_USER:COUCHDB_PASSWORD@COUCHDB_ENDPOINT
func couchURL() string {
	return fmt.Sprintf(
		"http://%s:%s@%s",
		os.Getenv("COUCHDB_USER"),
		os.Getenv("COUCHDB_PASSWORD"),
		os.Getenv("COUCHDB_ENDPOINT"),
	)
}
