"""Prometheus metrics for monitoring bot activity."""

import logging
from prometheus_client import Counter, Histogram, start_http_server

logger = logging.getLogger(__name__)

# Define Prometheus metrics
COMMANDS_PROCESSED = Counter(
    'bot_commands_processed_total',
    'Total number of commands processed',
    ['command', 'status']
)

COMMAND_DURATION = Histogram(
    'bot_command_duration_seconds',
    'Time spent processing commands',
    ['command']
)

MESSAGES_RECEIVED = Counter(
    'bot_messages_received_total',
    'Total number of messages received'
)


def start_metrics_server(port: int = 9090):
    """
    Start the Prometheus metrics HTTP server.
    
    Args:
        port: Port number for the metrics server (default: 9090)
    """
    try:
        start_http_server(port)
        logger.info(f"Metrics server started on port {port}")
    except Exception as e:
        logger.error(f"Failed to start metrics server: {e}")
        raise
