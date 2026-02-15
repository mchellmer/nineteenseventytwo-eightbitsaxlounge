import logging
from twitchio.ext import commands
from twitchio.models.eventsub_ import ChatMessage as TwitchChatMessage

from commands.command_registry import CommandRegistry
from twitch.twitch_autobot import TwitchAutoBot

logger = logging.getLogger(__name__)

class EightBitSaxLoungeComponent(commands.Component):
    """Main component for the EightBitSaxLounge Twitch bot."""

    # TwitchIO event listener for incoming chat messages
    @commands.Component.listener()
    async def event_message(self, payload: TwitchChatMessage) -> None:
        logger.info(f"[{payload.broadcaster.name}] - {payload.chatter.name}: {payload.text}")

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
    async def predelay(self, ctx: commands.Context, *args) -> None:
        """Handle !predelay commands."""
        await self._execute_command('predelay', list(args), ctx)
    
    @commands.command()
    async def control1(self, ctx: commands.Context, *args) -> None:
        """Handle !control1 commands."""
        await self._execute_command('control1', list(args), ctx)
    
    @commands.command()
    async def control2(self, ctx: commands.Context, *args) -> None:
        """Handle !control2 commands."""
        await self._execute_command('control2', list(args), ctx)
    
    @commands.command()
    async def help(self, ctx: commands.Context, *args) -> None:
        """Handle !help command."""
        await self._execute_command('help', list(args), ctx)

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
                    # Add delay between messages to avoid Twitch rate limiting
                    # Twitch drops messages if sent too quickly
                    if i < len(response) - 1:
                        await asyncio.sleep(1.5)
                logger.info(f'Successfully sent all {len(response)} messages for !{command}')
            else:
                await ctx.send(response)
                logger.info(f'Successfully executed !{command} command')
            
        except Exception as e:
            logger.error(f'Error executing command {command}: {e}')
            await ctx.send('❌ An error occurred while processing your command.')