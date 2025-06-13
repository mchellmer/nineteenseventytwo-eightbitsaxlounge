package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io/ioutil"
	"net/http"
	"os"
)

var couchEndpoint = os.Getenv("COUCHDB_ENDPOINT")
var couchUsername = os.Getenv("COUCHDB_USER")
var couchPassword = os.Getenv("COUCHDB_PASSWORD")
var couchURL = fmt.Sprintf("http://%s:%s@%s", couchUsername, couchPassword, couchEndpoint)

func CreateDbHandler(dbName string) error {
	req, err := http.NewRequest(http.MethodPut, fmt.Sprintf("%s/%s", couchURL, dbName), nil)
	if err != nil {
		return fmt.Errorf("failed to create PUT request: %w", err)
	}

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send PUT request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusCreated && resp.StatusCode != http.StatusPreconditionFailed {
		// 412 Precondition Failed means the DB already exists
		body, _ := ioutil.ReadAll(resp.Body)
		return fmt.Errorf("failed to create database, status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

func CreateDocHandler(dbname string, doc map[string]interface{}) error {
	b, err := json.Marshal(doc)
	if err != nil {
		return fmt.Errorf("failed to marshal document: %w", err)
	}

	dbURL := fmt.Sprintf("%s/%s", couchURL, dbname)

	resp, err := http.Post(dbURL, "application/json", bytes.NewReader(b))
	if err != nil {
		return fmt.Errorf("failed to send POST request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusCreated {
		body, _ := ioutil.ReadAll(resp.Body)
		return fmt.Errorf("POST request failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

func DeleteDocHandler(dbname string, id string) error {
	// Step 1: Retrieve the document to get its _rev
	doc, err := GetDocHandler(dbname, id)
	if err != nil {
		return fmt.Errorf("failed to retrieve document for deletion: %w", err)
	}

	rev, ok := doc["_rev"].(string)
	if !ok {
		return fmt.Errorf("document does not contain a valid _rev field")
	}

	dbUrl := fmt.Sprintf("%s/%s", couchURL, dbname)

	// Step 2: Construct the DELETE request URL with the _rev query parameter
	url := fmt.Sprintf("%s/%s?rev=%s", dbUrl, id, rev)

	req, err := http.NewRequest(http.MethodDelete, url, nil)
	if err != nil {
		return fmt.Errorf("failed to create DELETE request: %w", err)
	}

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send DELETE request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := ioutil.ReadAll(resp.Body)
		return fmt.Errorf("DELETE request failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

func GetDocHandler(dbname string, id string) (map[string]interface{}, error) {
	dbURL := fmt.Sprintf("%s/%s", couchURL, dbname)

	resp, err := http.Get(fmt.Sprintf("%s/%s", dbURL, id))
	if err != nil {
		return nil, fmt.Errorf("failed to send GET request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := ioutil.ReadAll(resp.Body)
		return nil, fmt.Errorf("GET request failed with status %d: %s", resp.StatusCode, string(body))
	}

	var result map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, fmt.Errorf("failed to decode response body: %w", err)
	}
	return result, nil
}

func UpdateDocHandler(dbname string, id string, doc map[string]interface{}) error {
	b, err := json.Marshal(doc)
	if err != nil {
		return fmt.Errorf("failed to marshal document: %w", err)
	}

	dbURL := fmt.Sprintf("%s/%s", couchURL, dbname)

	req, err := http.NewRequest(http.MethodPut, fmt.Sprintf("%s/%s", dbURL, id), bytes.NewReader(b))
	if err != nil {
		return fmt.Errorf("failed to create PUT request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send PUT request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := ioutil.ReadAll(resp.Body)
		return fmt.Errorf("PUT request failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}
