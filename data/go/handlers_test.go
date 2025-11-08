// Test strategy for handler layer.
//
// Goal: verify that each HTTP handler returns the correct status code and
// JSON behavior given success, bad input (invalid JSON), or underlying service
// errors—without performing real I/O.
//
// Approach:
//   - A lightweight mockCouchService implements CouchService and can be
//     toggled to return errors (returnError bool) to simulate backend failures.
//   - chi route parameters ({dbname}, {id}) are injected using a helper that
//     creates a RouteContext and attaches it to the request’s context.
//   - Handlers are invoked directly with httptest.NewRecorder to capture
//     response status and body, keeping tests fast and deterministic.
//   - For JSON bodies we marshal maps; for malformed JSON we supply broken
//     input and assert 400 Bad Request.
//
// What is confirmed:
//   - Status codes: 200 (GET/PUT/DELETE success), 201 (create), 400 (bad JSON),
//     404 (doc not found), 500 (service error).
//   - Basic response payload structure for successful GET document.
//
// Not covered (yet):
//   - Middleware (auth, logging, tracing).
//   - Concurrency or large payload performance.
//   - Detailed response bodies for failures beyond status code mapping.
//
// Extension ideas:
//   - Table-driven tests enumerating multiple error types.
//   - Property tests for JSON round-trip handling.
//   - Adding benchmarks for high-volume doc updates.
package gofiles

import (
	"bytes"
	"context"
	"encoding/json"
	"errors"
	"net/http"
	"net/http/httptest"
	"testing"

	"github.com/go-chi/chi/v5"
)

// Mock implementation of CouchService for handler tests
type mockCouchService struct {
	// Optionally add fields to control mock behavior
	returnError bool
}

func (m *mockCouchService) GetDocumentByDatabaseNameAndDocumentId(dbname, id string) (map[string]interface{}, error) {
	if m.returnError {
		return nil, errors.New("not found")
	}
	return map[string]interface{}{"_id": id, "foo": "bar"}, nil
}
func (m *mockCouchService) CreateDocumentByDatabaseName(dbname string, doc map[string]interface{}) error {
	if m.returnError {
		return errors.New("create error")
	}
	return nil
}
func (m *mockCouchService) UpdateDocumentByDatabaseNameAndDocumentId(dbname, id string, doc map[string]interface{}) error {
	if m.returnError {
		return errors.New("update error")
	}
	return nil
}
func (m *mockCouchService) DeleteDocumentByDatabaseNameAndDocumentId(dbname, id string) error {
	if m.returnError {
		return errors.New("delete error")
	}
	return nil
}

func (m *mockCouchService) GetDatabaseByName(dbname string) (map[string]interface{}, error) {
	if m.returnError {
		return nil, errors.New("get db error")
	}
	return map[string]interface{}{"db_name": dbname, "doc_count": 0}, nil
}
func (m *mockCouchService) CreateDatabaseByName(dbname string) error {
	if m.returnError {
		return errors.New("create db error")
	}
	return nil
}
func (m *mockCouchService) DeleteDatabaseByName(dbname string) error {
	if m.returnError {
		return errors.New("delete db error")
	}
	return nil
}

func (m *mockCouchService) GetDocumentsByDatabaseName(dbname string) ([]map[string]interface{}, error) {
	if m.returnError {
		return nil, errors.New("list docs error")
	}
	return []map[string]interface{}{
		{"_id": "1", "type": "note"},
		{"_id": "2", "type": "note"},
	}, nil
}

func TestGetDocumentByDatabaseNameAndDocumentIdHandler_Success(t *testing.T) {
	req := httptest.NewRequest("GET", "/testdb/123", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := GetDocumentByDatabaseNameAndDocumentIdHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rr.Code)
	}
	var doc map[string]interface{}
	if err := json.NewDecoder(rr.Body).Decode(&doc); err != nil {
		t.Fatalf("failed to decode response: %v", err)
	}
	if doc["_id"] != "123" {
		t.Errorf("expected _id 123, got %v", doc["_id"])
	}
}

func TestGetDocumentByDatabaseNameAndDocumentIdHandler_NotFound(t *testing.T) {
	req := httptest.NewRequest("GET", "/testdb/404", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "404"})
	rr := httptest.NewRecorder()

	handler := GetDocumentByDatabaseNameAndDocumentIdHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusNotFound {
		t.Fatalf("expected 404, got %d", rr.Code)
	}
}

func TestCreateDocumentByDatabaseNameHandler_Success(t *testing.T) {
	doc := map[string]interface{}{"foo": "bar"}
	body, _ := json.Marshal(doc)
	req := httptest.NewRequest("POST", "/testdb", bytes.NewReader(body))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := CreateDocumentByDatabaseNameHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusCreated {
		t.Fatalf("expected 201, got %d", rr.Code)
	}
}

func TestCreateDocumentByDatabaseNameHandler_BadJSON(t *testing.T) {
	req := httptest.NewRequest("POST", "/testdb", bytes.NewReader([]byte("{bad json")))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := CreateDocumentByDatabaseNameHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d", rr.Code)
	}
}

func TestCreateDocumentByDatabaseNameHandler_Error(t *testing.T) {
	doc := map[string]interface{}{"foo": "bar"}
	body, _ := json.Marshal(doc)
	req := httptest.NewRequest("POST", "/testdb", bytes.NewReader(body))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := CreateDocumentByDatabaseNameHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d", rr.Code)
	}
}

func TestUpdateDocumentByDatabaseNameAndDocumentIdHandler_Success(t *testing.T) {
	doc := map[string]interface{}{"foo": "baz"}
	body, _ := json.Marshal(doc)
	req := httptest.NewRequest("PUT", "/testdb/123", bytes.NewReader(body))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := UpdateDocumentByDatabaseNameAndDocumentIdHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rr.Code)
	}
}

func TestUpdateDocumentByDatabaseNameAndDocumentIdHandler_BadJSON(t *testing.T) {
	req := httptest.NewRequest("PUT", "/testdb/123", bytes.NewReader([]byte("{bad json")))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := UpdateDocumentByDatabaseNameAndDocumentIdHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d", rr.Code)
	}
}

func TestUpdateDocumentByDatabaseNameAndDocumentIdHandler_Error(t *testing.T) {
	doc := map[string]interface{}{"foo": "baz"}
	body, _ := json.Marshal(doc)
	req := httptest.NewRequest("PUT", "/testdb/123", bytes.NewReader(body))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := UpdateDocumentByDatabaseNameAndDocumentIdHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d", rr.Code)
	}
}

func TestDeleteDocumentByDatabaseNameAndDocumentIdHandler_Success(t *testing.T) {
	req := httptest.NewRequest("DELETE", "/testdb/123", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := DeleteDocumentByDatabaseNameAndDocumentIdHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rr.Code)
	}
}

func TestDeleteDocumentByDatabaseNameAndDocumentIdHandler_Error(t *testing.T) {
	req := httptest.NewRequest("DELETE", "/testdb/123", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := DeleteDocumentByDatabaseNameAndDocumentIdHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d", rr.Code)
	}
}

// Helper to set chi URL params in request context
func setChiURLParams(req *http.Request, params map[string]string) *http.Request {
	rctx := chi.NewRouteContext()
	for k, v := range params {
		rctx.URLParams.Add(k, v)
	}
	return req.WithContext(context.WithValue(req.Context(), chi.RouteCtxKey, rctx))
}

func TestCreateDatabaseByNameHandler_Success(t *testing.T) {
	req := httptest.NewRequest("PUT", "/testdb", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := CreateDatabaseByNameHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusCreated {
		t.Fatalf("expected 201, got %d", rr.Code)
	}
}

func TestCreateDatabaseByNameHandler_Error(t *testing.T) {
	req := httptest.NewRequest("PUT", "/testdb", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := CreateDatabaseByNameHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d", rr.Code)
	}
}

func TestGetDocumentsByDatabaseNameHandler_Success(t *testing.T) {
	req := httptest.NewRequest("GET", "/testdb/docs", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()
	handler := GetDocumentsByDatabaseNameHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)
	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rr.Code)
	}
	var docs []map[string]interface{}
	if err := json.NewDecoder(rr.Body).Decode(&docs); err != nil {
		t.Fatalf("failed to decode docs: %v", err)
	}
	if len(docs) != 2 {
		t.Fatalf("expected 2 docs, got %d", len(docs))
	}
}

func TestGetDocumentsByDatabaseNameHandler_Error(t *testing.T) {
	req := httptest.NewRequest("GET", "/testdb/docs", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()
	handler := GetDocumentsByDatabaseNameHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)
	if rr.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d", rr.Code)
	}
}

func TestGetDatabaseByNameHandler_Success(t *testing.T) {
	req := httptest.NewRequest("GET", "/testdb", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := GetDatabaseByNameHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rr.Code)
	}
	var info map[string]interface{}
	if err := json.NewDecoder(rr.Body).Decode(&info); err != nil {
		t.Fatalf("failed to decode response: %v", err)
	}
	if info["db_name"] != "testdb" {
		t.Errorf("expected db_name testdb, got %v", info["db_name"])
	}
}

func TestGetDatabaseByNameHandler_NotFound(t *testing.T) {
	req := httptest.NewRequest("GET", "/missingdb", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "missingdb"})
	rr := httptest.NewRecorder()

	handler := GetDatabaseByNameHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusNotFound {
		t.Fatalf("expected 404, got %d", rr.Code)
	}
}

func TestDeleteDatabaseByNameHandler_Success(t *testing.T) {
	req := httptest.NewRequest("DELETE", "/testdb", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := DeleteDatabaseByNameHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rr.Code)
	}
}

func TestDeleteDatabaseByNameHandler_Error(t *testing.T) {
	req := httptest.NewRequest("DELETE", "/testdb", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := DeleteDatabaseByNameHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d", rr.Code)
	}
}
