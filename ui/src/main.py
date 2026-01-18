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

logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


async def main():
    """Main function to run the bot."""
    bot = TwitchBot()
    
    def signal_handler(signum, frame):
        logger.info(f'Received signal {signum}, shutting down...')
        asyncio.create_task(bot.shutdown())
    
    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)
    
    try:
        logger.info(f'Starting {bot.service_name} bot...')
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