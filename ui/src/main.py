"""
Main entry point for the streaming chatbot.
Handles application startup, signal handling, and bot lifecycle management.
"""

import asyncio
import logging
import signal
import sys

from .config.settings import settings
from .bots.factory import BotFactory

# Configure logging
logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


async def main():
    """Main function to run the bot."""
    # Create bot using factory (defaults to Twitch)
    streaming_service = getattr(settings, 'streaming_service', 'twitch')
    bot = BotFactory.create_bot(streaming_service)
    
    # Setup signal handlers for graceful shutdown
    def signal_handler(signum, frame):
        logger.info(f'Received signal {signum}, shutting down...')
        asyncio.create_task(bot.shutdown())
    
    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)
    
    try:
        logger.info(f'Starting {streaming_service} bot...')
        await bot.start()
    except KeyboardInterrupt:
        logger.info('Received keyboard interrupt, shutting down...')
    except Exception as e:
        logger.error(f'Fatal error: {e}')
        sys.exit(1)
    finally:
        await bot.shutdown()


if __name__ == '__main__':
    asyncio.run(main())