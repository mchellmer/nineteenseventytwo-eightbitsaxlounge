// Test correlation ID middleware behavior.
//
// Goal: Verify that correlation IDs are properly:
//  1. Generated when not present in request headers
//  2. Forwarded when already present in X-Correlation-ID header
//  3. Stored in request context for retrieval
//  4. Added to response headers
//
// Approach:
//   - Test middleware with and without X-Correlation-ID header
//   - Verify UUID format for generated IDs
//   - Confirm context storage and retrieval
//   - Check response headers contain correlation ID
package gofiles

import (
	"net/http"
	"net/http/httptest"
	"regexp"
	"testing"
)

// UUID v4 regex pattern for validation
var uuidRegex = regexp.MustCompile(`^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$`)

func TestCorrelationIDMiddleware_GeneratesNewID(t *testing.T) {
	// Test that middleware generates a new UUID when no header is present
	handler := CorrelationIDMiddleware(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		correlationID := GetCorrelationID(r.Context())
		if correlationID == "" {
			t.Error("correlation ID should not be empty")
		}
		if !uuidRegex.MatchString(correlationID) {
			t.Errorf("correlation ID %q is not a valid UUID v4", correlationID)
		}
		// Verify it's also in the response header
		if w.Header().Get("X-Correlation-ID") != correlationID {
			t.Error("response header should contain correlation ID")
		}
	}))

	req := httptest.NewRequest("GET", "/test", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)
}

func TestCorrelationIDMiddleware_ForwardsExistingID(t *testing.T) {
	// Test that middleware uses existing X-Correlation-ID from request header
	expectedID := "test-correlation-id-12345"

	handler := CorrelationIDMiddleware(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		correlationID := GetCorrelationID(r.Context())
		if correlationID != expectedID {
			t.Errorf("expected correlation ID %q, got %q", expectedID, correlationID)
		}
		// Verify it's in the response header
		if w.Header().Get("X-Correlation-ID") != expectedID {
			t.Errorf("response header should contain correlation ID %q", expectedID)
		}
	}))

	req := httptest.NewRequest("GET", "/test", nil)
	req.Header.Set("X-Correlation-ID", expectedID)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)
}

func TestGetCorrelationID_WithContext(t *testing.T) {
	// Test retrieval of correlation ID from context
	expectedID := "context-test-id"

	handler := CorrelationIDMiddleware(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		correlationID := GetCorrelationID(r.Context())
		if correlationID != expectedID {
			t.Errorf("expected correlation ID %q from context, got %q", expectedID, correlationID)
		}
	}))

	req := httptest.NewRequest("GET", "/test", nil)
	req.Header.Set("X-Correlation-ID", expectedID)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)
}

func TestGetCorrelationID_WithoutContext(t *testing.T) {
	// Test that GetCorrelationID returns empty string when no correlation ID in context
	req := httptest.NewRequest("GET", "/test", nil)
	correlationID := GetCorrelationID(req.Context())

	if correlationID != "" {
		t.Errorf("expected empty correlation ID, got %q", correlationID)
	}
}

func TestCorrelationIDMiddleware_ResponseHeader(t *testing.T) {
	// Test that response always includes X-Correlation-ID header
	handler := CorrelationIDMiddleware(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
	}))

	req := httptest.NewRequest("GET", "/test", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	responseID := rr.Header().Get("X-Correlation-ID")
	if responseID == "" {
		t.Error("response should include X-Correlation-ID header")
	}
	if !uuidRegex.MatchString(responseID) {
		t.Errorf("response correlation ID %q is not a valid UUID v4", responseID)
	}
}

func TestCorrelationIDMiddleware_MultipleRequests(t *testing.T) {
	// Test that different requests get different correlation IDs
	var id1, id2 string

	handler := CorrelationIDMiddleware(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		// Capture correlation ID for comparison
	}))

	// First request
	req1 := httptest.NewRequest("GET", "/test", nil)
	rr1 := httptest.NewRecorder()
	handler.ServeHTTP(rr1, req1)
	id1 = rr1.Header().Get("X-Correlation-ID")

	// Second request
	req2 := httptest.NewRequest("GET", "/test", nil)
	rr2 := httptest.NewRecorder()
	handler.ServeHTTP(rr2, req2)
	id2 = rr2.Header().Get("X-Correlation-ID")

	if id1 == id2 {
		t.Error("different requests should get different correlation IDs")
	}
}
