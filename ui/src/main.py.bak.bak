"""An example of connecting to a conduit and subscribing to EventSub when a User Authorizes the application.

This bot can be restarted as many times without needing to subscribe or worry about tokens:
- Tokens are stored in '.tio.tokens.json' by default
- Subscriptions last 72 hours after the bot is disconnected and refresh when the bot starts.

Consider reading through the documentation for AutoBot for more in depth explanations.
"""

import asyncio
import logging
import random
from typing import TYPE_CHECKING

import asqlite

import twitchio
from twitchio import eventsub
from twitchio.ext import commands
from config.settings import settings


if TYPE_CHECKING:
    import sqlite3


LOGGER: logging.Logger = logging.getLogger("Bot")

# Consider using a .env or another form of Configuration file!
CLIENT_ID: str = settings.twitch_client_id  # The CLIENT ID from the Twitch Dev Console
CLIENT_SECRET: str = settings.twitch_client_secret  # The CLIENT SECRET from the Twitch Dev Console
BOT_ID = "1424580736"  # The Account ID of the bot user...
OWNER_ID = "896950964"  # Your personal User ID..


class Bot(commands.AutoBot):
    def __init__(self, *, token_database: asqlite.Pool, subs: list[eventsub.SubscriptionPayload]) -> None:
        self.token_database = token_database

        super().__init__(
            client_id=CLIENT_ID,
            client_secret=CLIENT_SECRET,
            bot_id=BOT_ID,
            owner_id=OWNER_ID,
            prefix="!",
            subscriptions=subs,
            force_subscribe=True,
        )

    async def setup_hook(self) -> None:
        # Add our component which contains our commands...
        await self.add_component(MyComponent(self))

    async def event_oauth_authorized(self, payload: twitchio.authentication.UserTokenPayload) -> None:
        await self.add_token(payload.access_token, payload.refresh_token)

        if not payload.user_id:
            return

        if payload.user_id == self.bot_id:
            # We usually don't want subscribe to events on the bots channel...
            return

        # A list of subscriptions we would like to make to the newly authorized channel...
        subs: list[eventsub.SubscriptionPayload] = [
            eventsub.ChatMessageSubscription(broadcaster_user_id=payload.user_id, user_id=self.bot_id),
        ]

        resp: twitchio.MultiSubscribePayload = await self.multi_subscribe(subs)
        if resp.errors:
            LOGGER.warning("Failed to subscribe to: %r, for user: %s", resp.errors, payload.user_id)

    async def add_token(self, token: str, refresh: str) -> twitchio.authentication.ValidateTokenPayload:
        # Make sure to call super() as it will add the tokens interally and return us some data...
        resp: twitchio.authentication.ValidateTokenPayload = await super().add_token(token, refresh)

        # Store our tokens in a simple SQLite Database when they are authorized...
        query = """
        INSERT INTO tokens (user_id, token, refresh)
        VALUES (?, ?, ?)
        ON CONFLICT(user_id)
        DO UPDATE SET
            token = excluded.token,
            refresh = excluded.refresh;
        """

        async with self.token_database.acquire() as connection:
            await connection.execute(query, (resp.user_id, token, refresh))

        LOGGER.info("Added token to the database for user: %s", resp.user_id)
        return resp

    async def event_ready(self) -> None:
        LOGGER.info("Successfully logged in as: %s", self.bot_id)


class MyComponent(commands.Component):
    # An example of a Component with some simple commands and listeners
    # You can use Components within modules for a more organized codebase and hot-reloading.

    def __init__(self, bot: Bot) -> None:
        # Passing args is not required...
        # We pass bot here as an example...
        self.bot = bot

    # An example of listening to an event
    # We use a listener in our Component to display the messages received.
    @commands.Component.listener()
    async def event_message(self, payload: twitchio.ChatMessage) -> None:
        print(f"[{payload.broadcaster.name}] - {payload.chatter.name}: {payload.text}")

    @commands.command()
    async def test(self, ctx: commands.Context, *args) -> None:
        """Handle !engine commands."""
        print(f"Received !test command from {ctx.author.name} with args: {args}")
        await self._execute_command('engine', list(args), ctx)

    @commands.command()
    async def hi(self, ctx: commands.Context) -> None:
        """Command that replies to the invoker with Hi <name>!

        !hi
        """
        await ctx.reply(f"Hi {ctx.chatter}!")

    @commands.command()
    async def say(self, ctx: commands.Context, *, message: str) -> None:
        """Command which repeats what the invoker sends.

        !say <message>
        """
        await ctx.send(message)

    @commands.command()
    async def add(self, ctx: commands.Context, left: int, right: int) -> None:
        """Command which adds to integers together.

        !add <number> <number>
        """
        await ctx.reply(f"{left} + {right} = {left + right}")

    @commands.command()
    async def choice(self, ctx: commands.Context, *choices: str) -> None:
        """Command which takes in an arbitrary amount of choices and randomly chooses one.

        !choice <choice_1> <choice_2> <choice_3> ...
        """
        await ctx.reply(f"You provided {len(choices)} choices, I choose: {random.choice(choices)}")

    @commands.command(aliases=["thanks", "thank"])
    async def give(self, ctx: commands.Context, user: twitchio.User, amount: int, *, message: str | None = None) -> None:
        """A more advanced example of a command which has makes use of the powerful argument parsing, argument converters and
        aliases.

        The first argument will be attempted to be converted to a User.
        The second argument will be converted to an integer if possible.
        The third argument is optional and will consume the reast of the message.

        !give <@user|user_name> <number> [message]
        !thank <@user|user_name> <number> [message]
        !thanks <@user|user_name> <number> [message]
        """
        msg = f"with message: {message}" if message else ""
        await ctx.send(f"{ctx.chatter.mention} gave {amount} thanks to {user.mention} {msg}")

    @commands.group(invoke_fallback=True)
    async def socials(self, ctx: commands.Context) -> None:
        """Group command for our social links.

        !socials
        """
        await ctx.send("discord.gg/..., youtube.com/..., twitch.tv/...")

    @socials.command(name="discord")
    async def socials_discord(self, ctx: commands.Context) -> None:
        """Sub command of socials that sends only our discord invite.

        !socials discord
        """
        await ctx.send("discord.gg/...")

    async def _execute_command(self, command: str, args: list, ctx):
        """Execute a command through the command registry."""
        try:
            import asyncio
            user = ctx.author.name if hasattr(ctx, 'author') else 'unknown'
            print(f'Executing !{command} command from {user} with args: {args}')
            
            response = "success"
            
            # Handle both single string responses and list of messages
            if isinstance(response, list):
                print(f'Sending {len(response)} messages for !{command} command')
                for i, message in enumerate(response):
                    print(f'Sending message {i+1}/{len(response)}: {message[:50]}...')
                    await ctx.send(message)
                    # Add delay between messages to avoid Twitch rate limiting
                    # Twitch drops messages if sent too quickly
                    if i < len(response) - 1:
                        await asyncio.sleep(1.5)
                print(f'Successfully sent all {len(response)} messages for !{command}')
            else:
                await ctx.send(response)
                print(f'Successfully executed !{command} command')
            
        except Exception as e:
            print(f'Error executing command {command}: {e}')
            await ctx.send('❌ An error occurred while processing your command.')

async def setup_database(db: asqlite.Pool) -> tuple[list[tuple[str, str]], list[eventsub.SubscriptionPayload]]:
    # Create our token table, if it doesn't exist..
    # You should add the created files to .gitignore or potentially store them somewhere safer
    # This is just for example purposes...

    create_query = """
        CREATE TABLE IF NOT EXISTS tokens(
            user_id TEXT PRIMARY KEY,
            token TEXT NOT NULL,
            refresh TEXT NOT NULL
        )
        """

    async with db.acquire() as connection:
        await connection.execute(create_query)

        # Check if table is empty
        row = await connection.fetchone("SELECT COUNT(*) as count FROM tokens")
        is_empty = row["count"] == 0

        if is_empty:
            print("tokens table empty — seeding initial tokens")

            # Seed BOT
            await _seed_database_connection(
                connection,
                "896950964",
                "1sab5fp1m2k8gb262tw1u8y8t8bbly",
                "g5crlxrwg295lyplr9w3vzczeuivt0ywzy212joyynwjk4pgrx",
            )

            # Seed CHANNEL
            await _seed_database_connection(
                connection,
                "1424580736",
                "qvqpfwna672szskw5rcq0beuctyktg",
                "6e6xzn8jnwbeddodapt9xsu5tgq2eo75rgl28l7555fn7zjchz",
            )

        # Now fetch rows (after potential seeding)
        rows: list[sqlite3.Row] = await connection.fetchall(
            "SELECT * FROM tokens"
        )

    tokens: list[tuple[str, str]] = []
    subs: list[eventsub.SubscriptionPayload] = []

    for row in rows:
        tokens.append((row["token"], row["refresh"]))

        if row["user_id"] == BOT_ID:
            continue

        subs.append(
            eventsub.ChatMessageSubscription(
                broadcaster_user_id=row["user_id"],
                user_id=OWNER_ID,
            )
        )

    return tokens, subs


async def _seed_database_connection(
    connection,
    user_id: str,
    token: str,
    refresh: str,) -> None:
    
    query = """
    INSERT INTO tokens(user_id, token, refresh)
    VALUES(?, ?, ?)
    ON CONFLICT(user_id) DO UPDATE SET
        token = excluded.token,
        refresh = excluded.refresh;
    """

    await connection.execute(query, (user_id, token, refresh))
    print(f"Seeded tokens.db for user: {user_id}")

# Our main entry point for our Bot
# Best to setup_logging here, before anything starts
def main() -> None:
    twitchio.utils.setup_logging(level=logging.INFO)

    async def runner() -> None:
        async with asqlite.create_pool("tokens.db") as tdb:
            tokens, subs = await setup_database(tdb)

            async with Bot(token_database=tdb, subs=subs) as bot:
                for pair in tokens:
                    await bot.add_token(*pair)

                await bot.start(load_tokens=False)

    try:
        asyncio.run(runner())
    except KeyboardInterrupt:
        LOGGER.warning("Shutting down due to KeyboardInterrupt")


if __name__ == "__main__":
    main()