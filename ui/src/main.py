import asyncio
import logging
import sys

from bots.twitch.bot import Bot as StreamingBot
from config.logging_config import configure_logging
from config.settings import settings
from services.health_server import HealthServer

configure_logging(settings.log_level)
logger = logging.getLogger(__name__)

async def main() -> None:
    """
    Main function to run the bot and health server.
    - Starts the health server first, then the implementation of StreamingBot
    """

    try:
        bot = StreamingBot()
        health_server = HealthServer(port=8080, bot_instance=bot)

        await health_server.start()
        await bot.start()
    except KeyboardInterrupt:
        logger.warning("Shutting down due to KeyboardInterrupt")
    except Exception as e:
        logger.error(f'Fatal error: {e}')
        await shutdown_all(bot, health_server)
        sys.exit(1)
    finally:
        await shutdown_all(bot, health_server)

async def shutdown_all(bot, health_server):
    """Gracefully shutdown bot and health server."""
    await bot.shutdown()
    await health_server.stop()

if __name__ == "__main__":
    asyncio.run(main())
