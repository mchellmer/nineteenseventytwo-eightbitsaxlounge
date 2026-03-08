# Twitch Bot Setup

Quick guide for getting a Twitch configured before running the bot

## Overview

Twitchio v3 handles token lifecycle automatically so only a client id and secret are required.

## One-Time Setup Steps

### 1. Create Twitch Accounts

**Main Streaming Account**: Your existing Twitch account

**Bot Account** (recommended): Create a separate account at [twitch.tv](https://www.twitch.tv) with a bot-like username (e.g., "YourChannelBot")

### 2. Register Your Application

1. Log in to [dev.twitch.tv/console](https://dev.twitch.tv/console) with your **main account**
2. Click **"Register Your Application"**
3. Fill in:
   - **Name**: `EightBitSaxLoungeBot`
   - **OAuth Redirect URLs**: ` http://localhost:4343/oauth/callback`
   - **Category**: `Chat Bot`
4. Click **"Create"**
5. **Save** your **Client ID** -> provide in env on deployment e.g. via github workflow
6. Create a **Client Secret** -> ditto

## Initial Token Authorization

After deploying the bot to Kubernetes with a fresh PersistentVolumeClaim, you must authorize it once to generate and store tokens.

### Prerequisites

- Bot is running in Kubernetes with port-forward configured
- OAuth Redirect URL registered as `http://localhost:4343/oauth/callback` in your Twitch app
- PuTTY SSH session open to master node

### Authorization Steps

#### Step 1: Set up port forwarding
```bash
# In your PuTTY terminal on the master node:
kubectl port-forward -n eightbitsaxlounge-dev deployment/eightbitsaxlounge-chat 4343:4343
# Leave this running
```

#### Step 2: Configure PuTTY SSH tunnel (one time)
1. In PuTTY **Connection > SSH > Tunnels**:
   - Source port: `4343`
   - Destination: `localhost:4343`
   - Select **Local** → **Add**
2. Save the session

#### Step 3: Authorize bot account
Visit in your browser:
```
http://localhost:4343/oauth?scopes=user:read:chat+user:write:chat+channel:bot
```
- Click **Authorize** on the Twitch consent screen
- Browser shows "Success. You can leave this page."
- Check logs for: `Added token to the database for user: <bot_user_id>`

#### Step 4: Authorize broadcaster account
Repeat Step 3 with the same URL (or just refresh). The bot will detect a different user and authorize them.
- Check logs for: `Added token to the database for user: <broadcaster_user_id>`
- You should see: `Attempting to subscribe to 1 subscriptions...`

### Subscription Storage

- Tokens are stored in `/app/tokens/tokens.db` on the **PersistentVolumeClaim**
- They persist across pod restarts
- No re-authorization needed unless tokens expire or PVC is deleted

### Troubleshooting

**"409: subscription already exists"** - This is normal if you re-authorize. The subscription exists on Twitch's conduit.

**Commands not responding** - Verify:
- 2 tokens stored in database: `kubectl exec -it <pod> -- sqlite3 /app/tokens/tokens.db "select user_id from tokens;"`
- 1 EventSub subscription active: check logs for `Associated shards with ConduitInfo`

**Port-forward fails** - Ensure:
- `kubectl port-forward` command still running on master
- PuTTY tunnel is configured and active
- Run `netstat -an | findstr 4343` on your PC — should show `LISTENING`
