"""
Twitch bot implementation for EightBitSaxLounge.
Uses TwitchIO to interact with Twitch chat.
Implements StreamingBot interface.
"""

import logging
from twitchio.ext import commands

from config.settings import settings
from commands.command_registry import CommandRegistry
from services.twitch_client import TwitchClient
from bots.streaming_bot import StreamingBot

logger = logging.getLogger(__name__)


class TwitchBot(StreamingBot):
    """TwitchIO StreamingBot"""
    
    def __init__(self):
        self.twitchio = commands.Bot(
            token=settings.twitch_token,
            client_id=settings.twitch_client_id,
            nick=settings.bot_name.lower(),
            prefix=settings.twitch_prefix,
            initial_channels=[settings.twitch_channel],
            case_insensitive=True
        )
        
        self.command_registry = CommandRegistry()
        self._shutdown = False
        self._connected = False
        self.token_validator = TwitchClient(settings.twitch_client_id)
        
        # Wire up TwitchIO event handlers to TwitchBot methods
        self.twitchio.event(self._on_ready)
        self.twitchio.event(self._on_message)
        self.twitchio.event(self._on_command_error)
        
        # Register 8bsl channel commands with TwitchIO
        self.twitchio.add_command(commands.Command(name='engine', func=self.engine_command))
        self.twitchio.add_command(commands.Command(name='time', func=self.time_command))
        self.twitchio.add_command(commands.Command(name='predelay', func=self.predelay_command))
        self.twitchio.add_command(commands.Command(name='control1', func=self.control1_command))
        self.twitchio.add_command(commands.Command(name='control2', func=self.control2_command))
        self.twitchio.add_command(commands.Command(name='help', func=self.help_command))
    
    # StreamingBot interface implementation
    async def start(self) -> None:
        """Start the bot and connect to Twitch."""
        try:
            await self.token_validator.validate_and_warn(settings.twitch_token.replace('oauth:', ''))
        except Exception as e:
            logger.warning(f"Token validation failed: {e}")
        
        await self.twitchio.start()
    
    async def send_message(self, channel: str, message: str) -> None:
        """Send a message to the specified channel."""
        channel_obj = self.twitchio.get_channel(channel)
        if channel_obj:
            await channel_obj.send(message)
        else:
            logger.warning(f"Channel {channel} not found or not connected")
    
    async def shutdown(self):
        """Graceful shutdown."""
        logger.info('Shutting down bot...')
        self._shutdown = True
        self._connected = False
        await self.twitchio.close()
    
    async def engine_command(self, ctx, *args):
        """Handle !engine commands."""
        await self._execute_command('engine', list(args), ctx)
    
    async def time_command(self, ctx, *args):
        """Handle !time commands."""
        await self._execute_command('time', list(args), ctx)
    
    async def predelay_command(self, ctx, *args):
        """Handle !predelay commands."""
        await self._execute_command('predelay', list(args), ctx)
    
    async def control1_command(self, ctx, *args):
        """Handle !control1 commands."""
        await self._execute_command('control1', list(args), ctx)
    
    async def control2_command(self, ctx, *args):
        """Handle !control2 commands."""
        await self._execute_command('control2', list(args), ctx)
    
    async def help_command(self, ctx, *args):
        """Handle !help command."""
        await self._execute_command('help', list(args), ctx)
    
    @property
    def is_connected(self) -> bool:
        """Check if the bot is currently connected to Twitch."""
        return self._connected and not self._shutdown
    
    @property
    def bot_name(self) -> str:
        """Get the bot's username/nickname."""
        return self.twitchio.nick
    
    @property
    def primary_channel(self) -> str:
        """Get the primary channel the bot is monitoring."""
        return settings.twitch_channel
    
    @property
    def service_name(self) -> str:
        """Get the name of the streaming service."""
        return "Twitch"
    
    async def _execute_command(self, command: str, args: list, ctx):
        """Execute a command through the command registry."""
        try:
            import asyncio
            user = ctx.author.name if hasattr(ctx, 'author') else 'unknown'
            logger.info(f'Executing !{command} command from {user} with args: {args}')
            
            response = await self.command_registry.execute_command(command, args, ctx)
            
            # Handle both single string responses and list of messages
            if isinstance(response, list):
                logger.info(f'Sending {len(response)} messages for !{command} command')
                for i, message in enumerate(response):
                    logger.debug(f'Sending message {i+1}/{len(response)}: {message[:50]}...')
                    await ctx.send(message)
                    # Add small delay between messages to avoid rate limiting
                    if i < len(response) - 1:
                        await asyncio.sleep(0.5)
                logger.info(f'Successfully sent all {len(response)} messages for !{command}')
            else:
                await ctx.send(response)
                logger.info(f'Successfully executed !{command} command')
            
        except Exception as e:
            logger.error(f'Error executing command {command}: {e}')
            await ctx.send('❌ An error occurred while processing your command.')

    async def _on_ready(self):
        """Called when the bot is ready."""
        self._connected = True
        logger.info(f'Bot {self.twitchio.nick} is online and connected to #{settings.twitch_channel}!')
    
    async def _on_message(self, message):
        """Called when a message is received."""
        if message.echo:
            return
        
        # Log commands at INFO level, regular messages at DEBUG level
        if message.content.startswith(settings.twitch_prefix):
            logger.info(f'Command received from {message.author.name}: {message.content}')
        else:
            logger.debug(f'Message from {message.author.name}: {message.content}')
        
        await self.twitchio.handle_commands(message)
    
    async def _on_command_error(self, context, error):
        """Called when a command raises an error."""
        logger.error(f'Command error in {context.command}: {error}')
        await context.send('❌ An error occurred while processing your command.')
