import logging
from twitchio.ext import commands
import twitchio

from commands.command_registry import CommandRegistry
from services.nats_publisher import NatsPublisher

logger = logging.getLogger(__name__)

# Commands that emit overlay events after successful execution
# Maps command name -> NATS subject
OVERLAY_SUBJECTS: dict[str, str] = {
    "engine": "overlay.engine",
    "time":   "overlay.time",
    "delay":  "overlay.delay",
    "dial1":  "overlay.dial1",
    "dial2":  "overlay.dial2",
}

class EightBitSaxLoungeComponent(commands.Component):
    """Main component for the EightBitSaxLounge Twitch bot."""

    def __init__(self, **kwargs) -> None:
        super().__init__()
        self._nats = NatsPublisher()
        self._nats_connected = False

    async def _ensure_nats(self) -> None:
        """Lazily connect to NATS on first use."""
        if not self._nats_connected:
            try:
                await self._nats.connect()
                self._nats_connected = True
            except Exception as e:
                logger.error("Failed to connect to NATS: %s", e)

    # TwitchIO event listener for incoming chat messages
    @commands.Component.listener()
    async def event_message(self, payload: twitchio.ChatMessage) -> None:
        logger.info(f"[{payload.broadcaster.name}] - chat from {payload.chatter.name}: {payload.text}")

    # TwitchIO commands
    @commands.command()
    async def engine(self, ctx: commands.Context, *args) -> None:
        """Handle !engine commands."""
        await self._execute_command('engine', list(args), ctx)

    @commands.command()
    async def time(self, ctx: commands.Context, *args) -> None:
        """Handle !time commands."""
        await self._execute_command('time', list(args), ctx)
    
    @commands.command()
    async def delay(self, ctx: commands.Context, *args) -> None:
        """Handle !delay commands."""
        await self._execute_command('delay', list(args), ctx)
    
    @commands.command()
    async def dial1(self, ctx: commands.Context, *args) -> None:
        """Handle !dial1 commands."""
        await self._execute_command('dial1', list(args), ctx)
    
    @commands.command()
    async def dial2(self, ctx: commands.Context, *args) -> None:
        """Handle !dial2 commands."""
        await self._execute_command('dial2', list(args), ctx)
    
    @commands.command()
    async def help(self, ctx: commands.Context, *args) -> None:
        """Handle !help command."""
        await self._execute_command('help', list(args), ctx)

    @commands.command()
    async def player(self, ctx: commands.Context, *args) -> None:
        """Handle !player <3-char-string> command. Updates the player panel on the overlay."""
        if not args:
            await ctx.send("❌ Usage: !player <name> (3 characters)")
            return
        value = args[0]
        if len(value) != 3:
            await ctx.send(f"❌ Player name must be exactly 3 characters, got {len(value)}: '{value}'")
            return
        await self._ensure_nats()
        try:
            await self._nats.publish("overlay.player", value.upper())
            await ctx.send(f"🎵 Player updated: {value.upper()}")
            logger.info(f"Player overlay updated to '{value.upper()}' by {ctx.author.name}")
        except Exception as e:
            logger.error(f"Failed to publish player overlay event: {e}")
            await ctx.send("❌ An error occurred while updating the player.")

    async def _execute_command(self, command: str, args: list, ctx):
        """Execute a command through the command registry and emit overlay event on success."""
        try:
            import asyncio
            user = ctx.author.name if hasattr(ctx, 'author') else 'unknown'
            logger.info(f'Executing !{command} command from {user} with args: {args}')

            command_registry = CommandRegistry()
            
            response = await command_registry.execute_command(command, args, ctx)
            
            # Handle both single string responses and list of messages
            if isinstance(response, list):
                logger.info(f'Sending {len(response)} messages for !{command} command')
                for i, message in enumerate(response):
                    logger.debug(f'Sending message {i+1}/{len(response)}: {message[:50]}...')
                    await ctx.send(message)
                    if i < len(response) - 1:
                        await asyncio.sleep(1.5)
                logger.info(f'Successfully sent all {len(response)} messages for !{command}')
            else:
                await ctx.send(response)
                logger.info(f'Successfully executed !{command} command')

            # Emit overlay event if this command has a subject mapping
            if command in OVERLAY_SUBJECTS and args:
                await self._ensure_nats()
                try:
                    await self._nats.publish(OVERLAY_SUBJECTS[command], args[0])
                except Exception as e:
                    logger.error(f"Failed to publish overlay event for !{command}: {e}")

        except Exception as e:
            logger.error(f'Error executing command {command}: {e}')
            await ctx.send('❌ An error occurred while processing your command.')