"""NATS publisher service for broadcasting overlay events."""

import json
import logging
import nats

from config.settings import settings

logger = logging.getLogger(__name__)


class NatsPublisher:
    """Publishes overlay events to NATS for the overlay service to consume."""

    def __init__(self):
        self._nc = None

    async def connect(self) -> None:
        """Connect to NATS server."""
        opts = {"servers": settings.nats_url}
        if settings.nats_user:
            opts["user"] = settings.nats_user
        if settings.nats_pass:
            opts["password"] = settings.nats_pass
        self._nc = await nats.connect(**opts)
        logger.info(f"Connected to NATS at {settings.nats_url} as '{settings.nats_user}'")

    async def publish(self, subject: str, value: str) -> None:
        """Publish a value to an overlay NATS subject.

        Args:
            subject: Full NATS subject e.g. 'overlay.engine'
            value: The value to broadcast to the overlay
        """
        if not self._nc or self._nc.is_closed:
            logger.warning("NATS not connected, skipping publish to %s", subject)
            return
        payload = json.dumps({"value": value}).encode()
        await self._nc.publish(subject, payload)
        logger.info("Published overlay event %s = %s", subject, value)

    async def close(self) -> None:
        """Close the NATS connection."""
        if self._nc and not self._nc.is_closed:
            await self._nc.close()
            logger.info("NATS connection closed")
