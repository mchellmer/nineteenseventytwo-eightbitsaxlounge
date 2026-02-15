import asyncio
from config.settings import settings
# from commands.command_registry import CommandRegistry -> Moved to component
from bots.streaming_bot import StreamingBot
from bots.twitch.twitch_autobot import TwitchAutoBot

import asqlite
import logging
from typing import TYPE_CHECKING
from twitchio import eventsub

if TYPE_CHECKING:
    import sqlite3

logger = logging.getLogger(__name__)


class TwitchBot(StreamingBot):
    """
    Twitch bot implementation for EightBitSaxLounge.
    Implements StreamingBot interface and sets up token database.
    """
    
    def __init__(self) -> None:
        
        # v2 init
        # self.twitchio = commands.Bot(
        #     client_id=settings.twitch_client_id,
        #     client_secret=settings.twitch_client_secret,
        #     bot_id=settings.twitch_bot_id,
        #     owner_id=settings.twitch_owner_id,
        #     prefix=settings.twitch_prefix,
        #     # initial_channels=[settings.twitch_channel], moved to event sub
        #     case_insensitive=True
        # )

        # self.command_registry = CommandRegistry() # Moved to component
        self._shutdown = False
        self._connected = False
        # self.token_validator = TwitchClient(settings.twitch_client_id) # v3 handles tokens
        
        # Wire up TwitchIO event handlers to TwitchBot methods
        # Depracated as this uses irc
        # self.twitchio.event(self._on_ready)
        # self.twitchio.event(self._on_message) # handled in component now
        # self.twitchio.event(self._on_command_error)
        
        # Moved to component
        # Register 8bsl channel commands with TwitchIO
        # self.twitchio.add_command(commands.Command(self.engine_command, name='engine'))
        # self.twitchio.add_command(commands.Command(self.time_command, name='time'))
        # self.twitchio.add_command(commands.Command(self.predelay_command, name='predelay'))
        # self.twitchio.add_command(commands.Command(self.control1_command, name='control1'))
        # self.twitchio.add_command(commands.Command(self.control2_command, name='control2'))
        # self.twitchio.add_command(commands.Command(self.help_command, name='help'))
    
    async def start(self) -> None:
        """Start the bot and connect to Twitch."""
        async def runner() -> None:
            async with asqlite.create_pool("tokens.db") as tdb:
                tokens, subs = await self._setup_database(tdb)

                async with TwitchAutoBot(token_database=tdb, subs=subs) as bot:
                    for pair in tokens:
                        await bot.add_token(*pair)

                    await bot.start(load_tokens=False)

        await runner()
    
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

    # TODO: check v3 for shutdown implementation, may not be needed
    async def shutdown(self):
        """Graceful shutdown."""
        logger.info('Shutting down bot...')
        self._shutdown = True
        self._connected = False
    
    # MIGRATED to component
    # async def engine_command(self, ctx, *args):
    #     """Handle !engine commands."""
    #     await self._execute_command('engine', list(args), ctx)

    # MIGRATE to component
    # async def time_command(self, ctx, *args):
    #     """Handle !time commands."""
    #     await self._execute_command('time', list(args), ctx)
    
    # async def predelay_command(self, ctx, *args):
    #     """Handle !predelay commands."""
    #     await self._execute_command('predelay', list(args), ctx)
    
    # async def control1_command(self, ctx, *args):
    #     """Handle !control1 commands."""
    #     await self._execute_command('control1', list(args), ctx)
    
    # async def control2_command(self, ctx, *args):
    #     """Handle !control2 commands."""
    #     await self._execute_command('control2', list(args), ctx)
    
    # async def help_command(self, ctx, *args):
    #     """Handle !help command."""
    #     await self._execute_command('help', list(args), ctx)
    
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
    
    # Moved to Component
    # async def _execute_command(self, command: str, args: list, ctx):
    #     """Execute a command through the command registry."""
    #     try:
    #         import asyncio
    #         user = ctx.author.name if hasattr(ctx, 'author') else 'unknown'
    #         logger.info(f'Executing !{command} command from {user} with args: {args}')
            
    #         response = await self.command_registry.execute_command(command, args, ctx)
            
    #         # Handle both single string responses and list of messages
    #         if isinstance(response, list):
    #             logger.info(f'Sending {len(response)} messages for !{command} command')
    #             for i, message in enumerate(response):
    #                 logger.debug(f'Sending message {i+1}/{len(response)}: {message[:50]}...')
    #                 await ctx.send(message)
    #                 # Add delay between messages to avoid Twitch rate limiting
    #                 # Twitch drops messages if sent too quickly
    #                 if i < len(response) - 1:
    #                     await asyncio.sleep(1.5)
    #             logger.info(f'Successfully sent all {len(response)} messages for !{command}')
    #         else:
    #             await ctx.send(response)
    #             logger.info(f'Successfully executed !{command} command')
            
    #     except Exception as e:
    #         logger.error(f'Error executing command {command}: {e}')
    #         await ctx.send('❌ An error occurred while processing your command.')

    # No more nick in v3
    # async def _on_ready(self):
    #     """Called when the bot is ready."""
    #     self._connected = True
    #     logger.info(f'Bot {self.twitchio.nick} is online and connected to #{settings.twitch_channel}!')
    
    # Handled in component now
    # async def _on_message(self, message):
    #     """Called when a message is received."""
    #     if message.echo:
    #         return
        
    #     # Log commands at INFO level, regular messages at DEBUG level
    #     if message.content.startswith(settings.twitch_prefix):
    #         logger.info(f'Command received from {message.author.name}: {message.content}')
    #     else:
    #         logger.debug(f'Message from {message.author.name}: {message.content}')
        
    #     await self.twitchio.handle_commands(message)
    
    # async def _on_command_error(self, context, error):
    #     """Called when a command raises an error."""
    #     logger.error(f'Command error in {context.command}: {error}')
    #     await context.send('❌ An error occurred while processing your command.')

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

                if row["user_id"] == self.bot_id:
                    continue

                subs.extend([eventsub.ChatMessageSubscription(broadcaster_user_id=row["user_id"], user_id=self.bot_id)])

        return tokens, subs