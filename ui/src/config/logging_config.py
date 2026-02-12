"""
Centralized logging configuration for the UI layer.

Provides:
- Automatic [ui] prefix for all log messages
- Correlation ID support via context variables
- Structured log formatting
"""

import logging
import uuid
from contextvars import ContextVar
from typing import Optional

# Context variable to store correlation ID per async context
correlation_id_var: ContextVar[Optional[str]] = ContextVar('correlation_id', default=None)


class UILogFilter(logging.Filter):
    """Filter that adds [ui] prefix and correlation ID to all log records."""
    
    def filter(self, record: logging.LogRecord) -> bool:
        """Add UI prefix and correlation ID to the log record."""
        correlation_id = correlation_id_var.get()
        
        # Map Python log levels to standard names
        level_map = {
            'DEBUG': '[Debug]',
            'INFO': '[Information]',
            'WARNING': '[Warning]',
            'ERROR': '[Error]',
            'CRITICAL': '[Critical]'
        }
        
        # Replace levelname with bracketed version
        record.levelname = level_map.get(record.levelname, f'[{record.levelname}]')
        
        # Format the message with [ui] prefix and correlation ID at end
        if correlation_id:
            record.msg = f"[ui] {record.msg} correlationID={correlation_id}"
        else:
            record.msg = f"[ui] {record.msg}"
        
        return True


def get_correlation_id() -> str:
    """Get or generate correlation ID for the current context."""
    correlation_id = correlation_id_var.get()
    if correlation_id is None:
        correlation_id = str(uuid.uuid4())
        correlation_id_var.set(correlation_id)
    return correlation_id


def set_correlation_id(correlation_id: str) -> None:
    """Set correlation ID for the current context."""
    correlation_id_var.set(correlation_id)


def clear_correlation_id() -> None:
    """Clear correlation ID from the current context."""
    correlation_id_var.set(None)


def configure_logging(log_level: str = "INFO") -> None:
    """
    Configure logging for the UI service with centralized formatting.
    
    Args:
        log_level: Logging level (DEBUG, INFO, WARNING, ERROR, CRITICAL)
    """
    # Create root logger configuration
    logging.basicConfig(
        level=getattr(logging, log_level.upper()),
        format='%(asctime)s %(levelname)s %(message)s',
        datefmt='%Y-%m-%d %H:%M:%S',
        force=True  # Override any existing configuration
    )
    
    # Add UI filter to all handlers
    ui_filter = UILogFilter()
    root_logger = logging.getLogger()
    for handler in root_logger.handlers:
        handler.addFilter(ui_filter)
    
    # Also add to any logger that's created later
    logging.getLogger().addFilter(ui_filter)
