// Logger provides centralized logging with [data] prefix for all messages.
//
// Usage:
//
//	logger := NewLogger()
//	logger.Info("operation=get_document db=%s id=%s status=success correlationID=%s", dbname, id, correlationID)
//	logger.Error("operation=get_document status=error error=%v correlationID=%s", err, correlationID)
package gofiles

import (
	"fmt"
	"log"
	"os"
	"time"
)

// Logger wraps the standard logger with automatic [data] prefix
type Logger struct {
	logger *log.Logger
}

// NewLogger creates a new Logger instance with custom format
func NewLogger() *Logger {
	return &Logger{
		logger: log.New(os.Stdout, "", 0), // No flags, we'll format ourselves
	}
}

// formatMessage formats log messages with timestamp, level, and service prefix
func (l *Logger) formatMessage(level, format string, v ...interface{}) string {
	timestamp := time.Now().Format("2006-01-02 15:04:05.000")
	message := fmt.Sprintf(format, v...)
	return fmt.Sprintf("%s %s [data] %s", timestamp, level, message)
}

// Info logs an informational message with [data] prefix
func (l *Logger) Info(format string, v ...interface{}) {
	l.logger.Println(l.formatMessage("[Information]", format, v...))
}

// Error logs an error message with [data] prefix
func (l *Logger) Error(format string, v ...interface{}) {
	l.logger.Println(l.formatMessage("[Error]", format, v...))
}

// Warn logs a warning message with [data] prefix
func (l *Logger) Warn(format string, v ...interface{}) {
	l.logger.Println(l.formatMessage("[Warning]", format, v...))
}

// Debug logs a debug message with [data] prefix
func (l *Logger) Debug(format string, v ...interface{}) {
	l.logger.Println(l.formatMessage("[Debug]", format, v...))
}

// Infof is an alias for Info for compatibility
func (l *Logger) Infof(format string, v ...interface{}) {
	l.Info(format, v...)
}

// Errorf is an alias for Error for compatibility
func (l *Logger) Errorf(format string, v ...interface{}) {
	l.Error(format, v...)
}

// Warnf is an alias for Warn for compatibility
func (l *Logger) Warnf(format string, v ...interface{}) {
	l.Warn(format, v...)
}

// Debugf is an alias for Debug for compatibility
func (l *Logger) Debugf(format string, v ...interface{}) {
	l.Debug(format, v...)
}

// Global logger instance
var defaultLogger = NewLogger()

// Info logs an informational message using the default logger
func Info(format string, v ...interface{}) {
	defaultLogger.Info(format, v...)
}

// Error logs an error message using the default logger
func Error(format string, v ...interface{}) {
	defaultLogger.Error(format, v...)
}

// Warn logs a warning message using the default logger
func Warn(format string, v ...interface{}) {
	defaultLogger.Warn(format, v...)
}

// Debug logs a debug message using the default logger
func Debug(format string, v ...interface{}) {
	defaultLogger.Debug(format, v...)
}

// Printf provides standard log.Printf interface with [data] prefix
func Printf(format string, v ...interface{}) {
	defaultLogger.Info(format, v...)
}

// Println provides standard log.Println interface with [data] prefix
func Println(v ...interface{}) {
	defaultLogger.Info("%s", fmt.Sprint(v...))
}
