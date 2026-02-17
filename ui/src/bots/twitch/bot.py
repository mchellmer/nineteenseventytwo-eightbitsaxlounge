import asqlite
import logging
from typing import TYPE_CHECKING
from twitchio import eventsub
if TYPE_CHECKING:
    import sqlite3

from config.settings import settings
from bots.streaming_bot import StreamingBot
import bots.twitch.twitchio_autobot as twitchio_autobot

logger = logging.getLogger(__name__)


class Bot(StreamingBot):
    """
    Twitch bot implementation for EightBitSaxLounge.
    Implements StreamingBot interface and sets up token database for Twitchio token management.
    """
    
    def __init__(self) -> None:
        
        self._shutdown = False
        self._connected = False
    
    # TODO: Implement with v3
    async def send_message(self, channel: str, message: str) -> None:
        """Send a message to the specified channel."""
        # Removed in v3
        # channel_obj = self.twitchio.get_channel(channel)
        # if channel_obj:
        #     await channel_obj.send(message)
        # else:
        #     logger.warning(f"Channel {channel} not found or not connected")
        pass

    async def start(self) -> None:
        """Start the bot and connect to Twitch."""
        async def runner() -> None:
            async with asqlite.create_pool("tokens.db") as tdb:
                tokens, subs = await self._setup_database(tdb)
                logger.info(f"Loaded {len(tokens)} tokens and {len(subs)} subscriptions from the database")

                async with twitchio_autobot.TwitchioAutoBot(token_database=tdb, subs=subs) as bot:
                    for pair in tokens:
                        await bot.add_token(*pair)

                    await bot.start(load_tokens=False)

        await runner()

    # TODO: check v3 for shutdown implementation, may not be needed
    async def shutdown(self):
        """Graceful shutdown."""
        logger.info('Shutting down bot...')
        self._shutdown = True
        self._connected = False
    
    @property
    def is_connected(self) -> bool:
        """Check if the bot is currently connected to Twitch."""
        return self._connected and not self._shutdown
    
    @property
    def bot_name(self) -> str:
        """Get the bot's username/nickname."""
        return settings.bot_name
    
    @property
    def primary_channel(self) -> str:
        """Get the primary channel the bot is monitoring."""
        return settings.twitch_channel
    
    @property
    def service_name(self) -> str:
        """Get the name of the streaming service."""
        return "Twitch"

    async def _setup_database(self, db: asqlite.Pool) -> tuple[list[tuple[str, str]], list[eventsub.SubscriptionPayload]]:
    # Create our token table, if it doesn't exist..
    # You should add the created files to .gitignore or potentially store them somewhere safer
    # This is just for example purposes...

        query = """CREATE TABLE IF NOT EXISTS tokens(user_id TEXT PRIMARY KEY, token TEXT NOT NULL, refresh TEXT NOT NULL)"""
        async with db.acquire() as connection:
            await connection.execute(query)

            # Fetch any existing tokens...
            rows: list[sqlite3.Row] = await connection.fetchall("""SELECT * from tokens""")

            tokens: list[tuple[str, str]] = []
            subs: list[eventsub.SubscriptionPayload] = []

            for row in rows:
                tokens.append((row["token"], row["refresh"]))

                if row["user_id"] == BOT_ID:
                    continue

                subs.extend([eventsub.ChatMessageSubscription(broadcaster_user_id=row["user_id"], user_id=BOT_ID)])

        return tokens, subs