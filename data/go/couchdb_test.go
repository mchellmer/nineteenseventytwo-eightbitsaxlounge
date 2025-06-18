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

func TestGetFromCouch_Success(t *testing.T) {
	body := io.NopCloser(bytes.NewReader([]byte(`{"_id":"123","foo":"bar"}`)))
	resp := &http.Response{
		StatusCode: 200,
		Body:       body,
	}
	setMockClient(resp, nil)

	doc, err := GetDoc("testdb", "123")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if doc["_id"] != "123" {
		t.Errorf("expected _id 123, got %v", doc["_id"])
	}
}

func TestSaveToCouch_Success(t *testing.T) {
	body := io.NopCloser(bytes.NewReader([]byte(`{"ok":true,"id":"123","rev":"1-xyz"}`)))
	resp := &http.Response{
		StatusCode: 201,
		Body:       body,
	}
	setMockClient(resp, nil)

	doc := map[string]interface{}{"foo": "bar"}
	err := CreateDoc("testdb", doc)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
}

func TestUpdateInCouch_Success(t *testing.T) {
	body := io.NopCloser(bytes.NewReader([]byte(`{"ok":true,"id":"123","rev":"2-xyz"}`)))
	resp := &http.Response{
		StatusCode: 200,
		Body:       body,
	}
	setMockClient(resp, nil)

	doc := map[string]interface{}{"_id": "123", "_rev": "1-xyz", "foo": "baz"}
	err := UpdateDoc("testdb", "123", doc)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
}

func TestDeleteFromCouch_Success(t *testing.T) {
	// First, mock GetFromCouch to return a doc with _rev
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

	err := DeleteDoc("testdb", "123")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
}

func TestCreateDb_Success(t *testing.T) {
	body := io.NopCloser(bytes.NewReader([]byte(`{"ok":true}`)))
	resp := &http.Response{
		StatusCode: 201,
		Body:       body,
	}
	setMockClient(resp, nil)

	err := CreateDb("testdb")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
}

// Helper to allow custom logic in RoundTrip for DeleteFromCouch
type roundTripperFunc func(req *http.Request) (*http.Response, error)

func (f roundTripperFunc) RoundTrip(req *http.Request) (*http.Response, error) {
	return f(req)
}
