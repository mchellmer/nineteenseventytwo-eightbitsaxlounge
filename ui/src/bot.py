"""
Twitch bot implementation for EightBitSaxLounge.
Contains the Twitch-specific bot implementation using TwitchIO.
"""

import asyncio
import logging
from typing import Optional
import twitchio
from twitchio.ext import commands
from prometheus_client import start_http_server, Counter, Histogram

from .config import settings
from .commands import CommandRegistry
from .interfaces import StreamingBot

# Metrics
COMMANDS_PROCESSED = Counter('bot_commands_processed_total', 'Total commands processed', ['command', 'status'])
COMMAND_DURATION = Histogram('bot_command_duration_seconds', 'Command execution time', ['command'])
MESSAGES_RECEIVED = Counter('bot_messages_received_total', 'Total messages received')

logger = logging.getLogger(__name__)


class TwitchBot(StreamingBot, commands.Bot):
    """Twitch-specific bot implementation using TwitchIO."""
    
    def __init__(self):
        commands.Bot.__init__(
            self,
            token=settings.twitch_token,
            client_id=settings.twitch_client_id,
            nick=settings.bot_name.lower(),
            prefix=settings.twitch_prefix,
            initial_channels=[settings.twitch_channel]
        )
        
        self.command_registry = CommandRegistry()
        self._shutdown = False
        self._connected = False
    
    async def event_ready(self):
        """Called when the bot is ready."""
        self._connected = True
        logger.info(f'Bot {self.nick} is online and connected to #{settings.twitch_channel}!')
        
        # Start metrics server if enabled
        if settings.metrics_enabled:
            start_http_server(settings.metrics_port)
            logger.info(f'Metrics server started on port {settings.metrics_port}')
    
    # StreamingBot interface implementation
    async def start(self) -> None:
        """Start the bot and connect to Twitch."""
        await commands.Bot.start(self)
    
    async def send_message(self, channel: str, message: str) -> None:
        """Send a message to the specified channel."""
        channel_obj = self.get_channel(channel)
        if channel_obj:
            await channel_obj.send(message)
        else:
            logger.warning(f"Channel {channel} not found or not connected")
    
    @property
    def is_connected(self) -> bool:
        """Check if the bot is currently connected to Twitch."""
        return self._connected and not self._shutdown
    
    @property
    def bot_name(self) -> str:
        """Get the bot's username/nickname."""
        return self.nick
    
    @property
    def primary_channel(self) -> str:
        """Get the primary channel the bot is monitoring."""
        return settings.twitch_channel
    
    async def event_message(self, message):
        """Called when a message is received."""
        # Ignore messages from the bot itself
        if message.echo:
            return
        
        MESSAGES_RECEIVED.inc()
        
        # Log the message for debugging
        logger.debug(f'Message from {message.author.name}: {message.content}')
        
        # Handle commands
        await self.handle_commands(message)
    
    async def event_command_error(self, context, error):
        """Called when a command raises an error."""
        logger.error(f'Command error in {context.command}: {error}')
        await context.send(f'❌ An error occurred while processing your command.')
    
    @commands.command(name='engine')
    async def engine_command(self, ctx, *args):
        """Handle !engine commands."""
        await self._execute_command('engine', list(args), ctx)
    
    @commands.command(name='help')
    async def help_command(self, ctx, *args):
        """Handle !help commands."""
        await self._execute_command('help', list(args), ctx)
    
    @commands.command(name='status')
    async def status_command(self, ctx, *args):
        """Handle !status commands."""
        await self._execute_command('status', list(args), ctx)
    
    async def _execute_command(self, command: str, args: list, ctx):
        """Execute a command through the command registry."""
        with COMMAND_DURATION.labels(command=command).time():
            try:
                response = await self.command_registry.execute_command(command, args, ctx)
                await ctx.send(response)
                COMMANDS_PROCESSED.labels(command=command, status='success').inc()
                
            except Exception as e:
                logger.error(f'Error executing command {command}: {e}')
                await ctx.send(f'❌ An error occurred while processing your command.')
                COMMANDS_PROCESSED.labels(command=command, status='error').inc()
    
    async def shutdown(self):
        """Graceful shutdown."""
        logger.info('Shutting down bot...')
        self._shutdown = True
        self._connected = False
        await self.close()


# Backward compatibility alias
EightBitSaxBot = TwitchBot