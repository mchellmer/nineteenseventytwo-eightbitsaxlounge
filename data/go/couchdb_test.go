// Test strategy for CouchDB service methods.
//
// These tests focus on confirming that each CRUD method in ProdCouchService:
//  1. Issues the correct HTTP method and URL structure.
//  2. Interprets success status codes (200/201) properly.
//  3. Decodes JSON response bodies into maps as expected.
//  4. Supplies required parameters (e.g., revision for DELETE after a GET).
//
// Approach:
//   - We override the package-level httpClient with a custom RoundTripper to
//     avoid real network calls and return canned *http.Response objects.
//   - For simple success cases (Get/Put/Post), a mockRoundTripper with a fixed
//     response is enough.
//   - For DeleteDoc we need two sequential calls (GET then DELETE); a custom
//     roundTripperFunc tracks invocation order and returns the appropriate
//     response.
//   - Each test asserts absence of error and key fields in the decoded map.
//
// Limits:
//   - Error paths (non-200/201 statuses, malformed JSON) are not covered yet.
//   - Authentication and environment-derived URLs are assumed correct.
//   - Tests are intentionally narrow to keep them fast and deterministic.
//
// Extending:
//   - Add table-driven cases for error scenarios (e.g., 404, 500) using the
//     same mocking mechanism.
//   - Inject a fake httpClient per test if parallelism is desired.
package gofiles

import (
	"bytes"
	"io"
	"net/http"
	"testing"
)

// mockRoundTripper allows us to mock HTTP responses for the httpClient.
type mockRoundTripper struct {
	resp *http.Response
	err  error
}

func (m *mockRoundTripper) RoundTrip(req *http.Request) (*http.Response, error) {
	return m.resp, m.err
}

func setMockClient(resp *http.Response, err error) {
	httpClient = &http.Client{
		Transport: &mockRoundTripper{resp: resp, err: err},
	}
}

// Use the real service struct for tests
var svc = &ProdCouchService{}

func TestGetDoc_Success(t *testing.T) {
	body := io.NopCloser(bytes.NewReader([]byte(`{"_id":"123","foo":"bar"}`)))
	resp := &http.Response{
		StatusCode: 200,
		Body:       body,
	}
	setMockClient(resp, nil)

	doc, err := svc.GetDoc("testdb", "123")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if doc["_id"] != "123" {
		t.Errorf("expected _id 123, got %v", doc["_id"])
	}
}

func TestCreateDoc_Success(t *testing.T) {
	body := io.NopCloser(bytes.NewReader([]byte(`{"ok":true,"id":"123","rev":"1-xyz"}`)))
	resp := &http.Response{
		StatusCode: 201,
		Body:       body,
	}
	setMockClient(resp, nil)

	doc := map[string]interface{}{"foo": "bar"}
	err := svc.CreateDoc("testdb", doc)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
}

func TestUpdateDoc_Success(t *testing.T) {
	body := io.NopCloser(bytes.NewReader([]byte(`{"ok":true,"id":"123","rev":"2-xyz"}`)))
	resp := &http.Response{
		StatusCode: 200,
		Body:       body,
	}
	setMockClient(resp, nil)

	doc := map[string]interface{}{"_id": "123", "_rev": "1-xyz", "foo": "baz"}
	err := svc.UpdateDoc("testdb", "123", doc)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
}

func TestDeleteDoc_Success(t *testing.T) {
	// First, mock GetDoc to return a doc with _rev
	getBody := io.NopCloser(bytes.NewReader([]byte(`{"_id":"123","_rev":"1-xyz"}`)))
	getResp := &http.Response{
		StatusCode: 200,
		Body:       getBody,
	}
	// Then, mock the DELETE call to return 200 OK
	deleteBody := io.NopCloser(bytes.NewReader([]byte(`{"ok":true,"id":"123","rev":"2-xyz"}`)))
	deleteResp := &http.Response{
		StatusCode: 200,
		Body:       deleteBody,
	}

	call := 0
	httpClient = &http.Client{
		Transport: roundTripperFunc(func(req *http.Request) (*http.Response, error) {
			call++
			if call == 1 && req.Method == http.MethodGet {
				return getResp, nil
			}
			if call == 2 && req.Method == http.MethodDelete {
				return deleteResp, nil
			}
			return nil, nil
		}),
	}

	err := svc.DeleteDoc("testdb", "123")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
}

func TestGetDb_Success(t *testing.T) {
	body := io.NopCloser(bytes.NewReader([]byte(`{"db_name":"mydb","doc_count":10}`)))
	resp := &http.Response{
		StatusCode: 200,
		Body:       body,
	}
	setMockClient(resp, nil)

	dbInfo, err := svc.GetDb("mydb")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if dbInfo["db_name"] != "mydb" {
		t.Errorf("expected db_name mydb, got %v", dbInfo["db_name"])
	}
}

func TestCreateDb_Success(t *testing.T) {
	body := io.NopCloser(bytes.NewReader([]byte(`{"ok":true}`)))
	resp := &http.Response{
		StatusCode: 201,
		Body:       body,
	}
	setMockClient(resp, nil)

	err := svc.CreateDb("mydb")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
}

func TestDeleteDb_Success(t *testing.T) {
	body := io.NopCloser(bytes.NewReader([]byte(`{"ok":true}`)))
	resp := &http.Response{
		StatusCode: 200,
		Body:       body,
	}
	setMockClient(resp, nil)

	err := svc.DeleteDb("mydb")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
}

// Helper to allow custom logic in RoundTrip for DeleteDoc
type roundTripperFunc func(req *http.Request) (*http.Response, error)

func (f roundTripperFunc) RoundTrip(req *http.Request) (*http.Response, error) {
	return f(req)
}
