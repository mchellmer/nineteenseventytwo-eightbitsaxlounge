// Correlation ID middleware for request tracking across services
package gofiles

import (
	"context"
	"net/http"

	"github.com/google/uuid"
)

type contextKey string

const correlationIDKey contextKey = "correlationID"

// CorrelationIDMiddleware adds or forwards correlation IDs
func CorrelationIDMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		// Check if correlation ID exists in headers (from upstream service)
		correlationID := r.Header.Get("X-Correlation-ID")
		if correlationID == "" {
			// Generate new ID if not present
			correlationID = uuid.New().String()
		}

		// Add to response headers so downstream can see it
		w.Header().Set("X-Correlation-ID", correlationID)

		// Store in context for handlers to use
		ctx := context.WithValue(r.Context(), correlationIDKey, correlationID)
		next.ServeHTTP(w, r.WithContext(ctx))
	})
}

// GetCorrelationID retrieves correlation ID from context
func GetCorrelationID(ctx context.Context) string {
	if id, ok := ctx.Value(correlationIDKey).(string); ok {
		return id
	}
	return ""
}
