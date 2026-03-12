"""Command error types."""


class CommandError(Exception):
    """Raised by command handlers when a command cannot be executed.

    The exception message is a user-facing string suitable for sending
    directly to Twitch chat.  The component catches this and skips any
    downstream side-effects (e.g. NATS overlay publishing).
    """
