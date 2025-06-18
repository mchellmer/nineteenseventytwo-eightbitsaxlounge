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

func (m *mockCouchService) GetDoc(dbname, id string) (map[string]interface{}, error) {
	if m.returnError {
		return nil, errors.New("not found")
	}
	return map[string]interface{}{"_id": id, "foo": "bar"}, nil
}
func (m *mockCouchService) CreateDoc(dbname string, doc map[string]interface{}) error {
	if m.returnError {
		return errors.New("create error")
	}
	return nil
}
func (m *mockCouchService) UpdateDoc(dbname, id string, doc map[string]interface{}) error {
	if m.returnError {
		return errors.New("update error")
	}
	return nil
}
func (m *mockCouchService) DeleteDoc(dbname, id string) error {
	if m.returnError {
		return errors.New("delete error")
	}
	return nil
}
func (m *mockCouchService) CreateDb(dbname string) error {
	if m.returnError {
		return errors.New("create db error")
	}
	return nil
}

func TestGetDocHandler_Success(t *testing.T) {
	req := httptest.NewRequest("GET", "/data/testdb/123", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := GetDocHandler(&mockCouchService{})
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

func TestGetDocHandler_NotFound(t *testing.T) {
	req := httptest.NewRequest("GET", "/data/testdb/404", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "404"})
	rr := httptest.NewRecorder()

	handler := GetDocHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusNotFound {
		t.Fatalf("expected 404, got %d", rr.Code)
	}
}

func TestCreateDbHandler_Success(t *testing.T) {
	req := httptest.NewRequest("POST", "/data/testdb", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := CreateDbHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusCreated {
		t.Fatalf("expected 201, got %d", rr.Code)
	}
}

func TestCreateDbHandler_Error(t *testing.T) {
	req := httptest.NewRequest("POST", "/data/testdb", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := CreateDbHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d", rr.Code)
	}
}

func TestCreateDocHandler_Success(t *testing.T) {
	doc := map[string]interface{}{"foo": "bar"}
	body, _ := json.Marshal(doc)
	req := httptest.NewRequest("POST", "/data/testdb", bytes.NewReader(body))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := CreateDocHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusCreated {
		t.Fatalf("expected 201, got %d", rr.Code)
	}
}

func TestCreateDocHandler_BadJSON(t *testing.T) {
	req := httptest.NewRequest("POST", "/data/testdb", bytes.NewReader([]byte("{bad json")))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := CreateDocHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d", rr.Code)
	}
}

func TestCreateDocHandler_Error(t *testing.T) {
	doc := map[string]interface{}{"foo": "bar"}
	body, _ := json.Marshal(doc)
	req := httptest.NewRequest("POST", "/data/testdb", bytes.NewReader(body))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb"})
	rr := httptest.NewRecorder()

	handler := CreateDocHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d", rr.Code)
	}
}

func TestUpdateDocHandler_Success(t *testing.T) {
	doc := map[string]interface{}{"foo": "baz"}
	body, _ := json.Marshal(doc)
	req := httptest.NewRequest("PUT", "/data/testdb/123", bytes.NewReader(body))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := UpdateDocHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rr.Code)
	}
}

func TestUpdateDocHandler_BadJSON(t *testing.T) {
	req := httptest.NewRequest("PUT", "/data/testdb/123", bytes.NewReader([]byte("{bad json")))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := UpdateDocHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d", rr.Code)
	}
}

func TestUpdateDocHandler_Error(t *testing.T) {
	doc := map[string]interface{}{"foo": "baz"}
	body, _ := json.Marshal(doc)
	req := httptest.NewRequest("PUT", "/data/testdb/123", bytes.NewReader(body))
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := UpdateDocHandler(&mockCouchService{returnError: true})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d", rr.Code)
	}
}

func TestDeleteDocHandler_Success(t *testing.T) {
	req := httptest.NewRequest("DELETE", "/data/testdb/123", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := DeleteDocHandler(&mockCouchService{})
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rr.Code)
	}
}

func TestDeleteDocHandler_Error(t *testing.T) {
	req := httptest.NewRequest("DELETE", "/data/testdb/123", nil)
	req = setChiURLParams(req, map[string]string{"dbname": "testdb", "id": "123"})
	rr := httptest.NewRecorder()

	handler := DeleteDocHandler(&mockCouchService{returnError: true})
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
