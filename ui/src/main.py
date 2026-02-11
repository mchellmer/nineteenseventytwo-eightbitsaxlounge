"""
Main entry point for a streaming chatbot.
Handles application startup, signal handling, and bot lifecycle management.

Implements StreamingBot interface. Options: TwitchBot
"""

import asyncio
import logging
import signal
import sys

from config.settings import settings
from bots.twitch_bot import TwitchBot
from services.health_server import HealthServer

logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


async def main():
    """Main function to run the bot and health server."""
    bot = TwitchBot()
    health_server = HealthServer(port=8080, bot_instance=bot)
    
    def signal_handler(signum, frame):
        logger.info(f'Received signal {signum}, shutting down...')
        asyncio.create_task(shutdown_all(bot, health_server))
    
    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)
    
    try:
        # Start health server first
        logger.info('Starting health check server...')
        await health_server.start()
        
        # Then start the bot
        logger.info(f'Starting {bot.service_name} bot...')
        await bot.start()
    except KeyboardInterrupt:
        logger.info('Received keyboard interrupt, shutting down...')
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


if __name__ == '__main__':
    asyncio.run(main())