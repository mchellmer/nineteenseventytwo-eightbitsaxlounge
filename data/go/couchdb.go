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

func GetDoc(dbname string, id string) (map[string]interface{}, error) {
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

func CreateDoc(dbname string, doc map[string]interface{}) error {
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

func UpdateDoc(dbname string, id string, doc map[string]interface{}) error {
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

func DeleteDoc(dbname string, id string) error {
	doc, err := GetDoc(dbname, id)
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

func CreateDb(dbName string) error {
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

func couchURL() string {
	return fmt.Sprintf(
		"http://%s:%s@%s",
		os.Getenv("COUCHDB_USER"),
		os.Getenv("COUCHDB_PASSWORD"),
		os.Getenv("COUCHDB_ENDPOINT"),
	)
}
