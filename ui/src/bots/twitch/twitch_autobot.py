from bots.twitch.eightbitsaxlounge_component import EightBitSaxLoungeComponent
from config.settings import settings

import asqlite
import logging
import twitchio
from twitchio import eventsub
from twitchio.ext import commands

logger = logging.getLogger(__name__)


class TwitchAutoBot(commands.AutoBot):
    """
    TwitchIO AutoBot
    An implementation of TwitchIO Bot with auto token management and event subscription using asqlite for token storage.
    """
    
    def __init__(self, *, token_database: asqlite.Pool, subs: list[eventsub.SubscriptionPayload]) -> None:
        self.token_database = token_database
        # keep a local reference to the subscriptions we intend to have
        self._desired_subscriptions = subs
        
        super().__init__(
            client_id=settings.twitch_client_id,
            client_secret=settings.twitch_client_secret,
            bot_id=settings.twitch_bot_id,
            owner_id=settings.twitch_owner_id,
            prefix=settings.twitch_prefix,
            subscriptions=subs,
            force_subscribe=True,
        )
        
    async def add_token(self, token: str, refresh: str) -> twitchio.authentication.ValidateTokenPayload:
        """Add or update tokens in the database when authorized."""
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

        logger.info("Added token to the database for user: %s", resp.user_id)
        return resp

    async def event_oauth_authorized(self, payload: twitchio.authentication.UserTokenPayload) -> None:
        """
        Oauth - get/put tokens in database
        Subscribe to channel events for authorized channels
        """
        logger.info(f"Received oauth authorization for user_id: {payload.user_id}")
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
            logger.warning("Failed to subscribe to: %r, for user: %s", resp.errors, payload.user_id)

    async def event_ready(self) -> None:
        """Called when the bot is ready."""
        self._connected = True
        logger.info(f'Bot {self.bot_id} is online and connected to #{settings.twitch_channel}!')
        try:
            subs = await self.fetch_eventsub_subscriptions()
            logger.info("Fetched eventsub subscriptions: %s", subs)
        except Exception:
            logger.exception("Failed to fetch eventsub subscriptions for debug")

    async def setup_hook(self) -> None:
        """Add custom components to e.g. handle commands and events."""
        await self.add_component(EightBitSaxLoungeComponent(self))
    