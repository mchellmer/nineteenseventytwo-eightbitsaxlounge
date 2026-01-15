# Twitch Bot Setup - Simple Guide

Quick guide for getting a Twitch access token and storing it in GitHub secrets.

## Overview

Twitch tokens last ~60 days. This guide shows you how to get a token manually and store it in GitHub Actions secrets. The bot will log warnings when the token is approaching expiry.

## One-Time Setup Steps

### 1. Create Twitch Accounts

**Main Streaming Account**: Your existing Twitch account

**Bot Account** (recommended): Create a separate account at [twitch.tv](https://www.twitch.tv) with a bot-like username (e.g., "YourChannelBot")

### 2. Register Your Application

1. Log in to [dev.twitch.tv/console](https://dev.twitch.tv/console) with your **main account**
2. Click **"Register Your Application"**
3. Fill in:
   - **Name**: `EightBitSaxLounge Bot`
   - **OAuth Redirect URLs**: `http://localhost:3000`
   - **Category**: `Chat Bot`
4. Click **"Create"**
5. **Save** your **Client ID** (you'll need this)

### 3. Generate Access Token

**Option A: Using Twitch CLI (Recommended)**

Install Twitch CLI:
```bash
# On Windows (PowerShell)
scoop install twitch-cli

# On macOS
brew install twitchdev/twitch/twitch-cli

# On Linux
# Download from https://github.com/twitchdev/twitch-cli/releases
```

Get your token:
```bash
# Log in as your BOT account
twitch configure

# Generate a user access token with required scopes
twitch token -u -s "chat:read chat:edit"

# Copy the "User Access Token" shown
```

**Option B: Using TwitchTokenGenerator (Quick & Easy)**

1. Go to [twitchtokengenerator.com](https://twitchtokengenerator.com)
2. **Log in with your BOT account**
3. Click **"Bot Chat Token"**
4. Click **"Authorize"**
5. **Copy the "Access Token"** (don't worry about the CLIENT ID shown - that's theirs)

**Option C: Manual OAuth Flow**

1. Build this URL (replace `YOUR_CLIENT_ID` with your Client ID from step 2):
   ```
   https://id.twitch.tv/oauth2/authorize?client_id=YOUR_CLIENT_ID&redirect_uri=http://localhost:3000&response_type=token&scope=chat:read+chat:edit+channel:moderate
   ```

2. **Log in to Twitch with your BOT account**
3. Visit the URL in your browser
4. Click **"Authorize"**
5. You'll be redirected to a page that won't load - that's normal!
6. **Copy the URL** from your browser's address bar
   - Example: `http://localhost:3000/#access_token=abc123xyz...&scope=chat:read+chat:edit`
7. Extract the `access_token` value

### 4. Format Your Token

Your token needs the `oauth:` prefix for Twitch IRC:

```
oauth:abc123xyz789yourtoken
```

### 5. Store in GitHub Secrets

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **"New repository secret"**
4. Add these secrets:

| Secret Name | Value |
|-------------|-------|
| `TWITCH_CLIENT_ID` | Your Client ID from dev.twitch.tv |
| `TWITCH_BOT_TOKEN` | `oauth:your_access_token` (with prefix!) |
| `TWITCH_CHANNEL` | Your streaming channel name |
| `MIDI_CLIENT_ID` | Your MIDI client ID |
| `MIDI_CLIENT_SECRET` | Your MIDI client secret |

### 6. Update Your GitHub Actions Workflow

Your workflow should reference these secrets when deploying to Kubernetes:

```yaml
- name: Update Kubernetes secrets
  env:
    TWITCH_CLIENT_ID: ${{ secrets.TWITCH_CLIENT_ID }}
    TWITCH_BOT_TOKEN: ${{ secrets.TWITCH_BOT_TOKEN }}
    TWITCH_CHANNEL: ${{ secrets.TWITCH_CHANNEL }}
  run: |
    # Base64 encode and update k8s secret
    kubectl create secret generic twitch-secrets \
      --from-literal=client-id=$TWITCH_CLIENT_ID \
      --from-literal=token=$TWITCH_BOT_TOKEN \
      --dry-run=client -o yaml | kubectl apply -f -
```

### 7. Make Bot a Moderator (Optional)

Log in to your main account and type in your chat:
```
/mod yourbotusername
```

## Token Expiry Management

### Monitoring

The bot automatically validates the token on startup and logs warnings:

- **30+ days remaining**: Info message
- **14-30 days remaining**: Warning
- **< 7 days remaining**: Error (urgent)

**Monitor your logs** or set up alerts on these messages in your CI/CD pipeline.

### When to Renew

Renew your token when:
- You see warnings in the logs
- The bot fails to connect
- ~Every 50-55 days (to be safe)

### How to Renew

1. Repeat steps 3-5 above to get a new token
2. Update the `TWITCH_BOT_TOKEN` secret in GitHub
3. Redeploy your application (or let your next deployment pick up the new secret)

## GitHub Actions Example

Here's a complete example for your workflow:

```yaml
name: Deploy UI

on:
  push:
    branches: [ main ]
    paths:
      - 'ui/**'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Build and push Docker image
        run: |
          docker build -t ghcr.io/${{ github.repository }}/ui:${{ github.sha }} ./ui
          docker push ghcr.io/${{ github.repository }}/ui:${{ github.sha }}
      
      - name: Deploy to Kubernetes
        env:
          KUBECONFIG: ${{ secrets.KUBECONFIG }}
        run: |
          # Update secrets
          kubectl create secret generic twitch-secrets \
            --namespace=eightbitsaxlounge \
            --from-literal=client-id=${{ secrets.TWITCH_CLIENT_ID }} \
            --from-literal=token=${{ secrets.TWITCH_BOT_TOKEN }} \
            --from-literal=midi-client-id=${{ secrets.MIDI_CLIENT_ID }} \
            --from-literal=midi-client-secret=${{ secrets.MIDI_CLIENT_SECRET }} \
            --dry-run=client -o yaml | kubectl apply -f -
          
          # Update deployment
          kubectl set image deployment/ui-deployment \
            twitch-bot=ghcr.io/${{ github.repository }}/ui:${{ github.sha }} \
            --namespace=eightbitsaxlounge
          
          # Wait for rollout
          kubectl rollout status deployment/ui-deployment \
            --namespace=eightbitsaxlounge
```

## Configuration Summary

### Environment Variables Needed

**Local Development** (in `ui/.env`):
```bash
TWITCH_CLIENT_ID=your_client_id
TWITCH_BOT_TOKEN=oauth:your_token
TWITCH_CHANNEL=yourchannelname
TWITCH_PREFIX=!
BOT_NAME=YourBotName
LOG_LEVEL=INFO

# MIDI configuration
MIDI_DEVICE_URL=http://localhost:5000
MIDI_CLIENT_ID=your_midi_client_id
MIDI_CLIENT_SECRET=your_midi_secret
```

**GitHub Secrets** (for production):
- `TWITCH_CLIENT_ID`
- `TWITCH_BOT_TOKEN`
- `TWITCH_CHANNEL`
- `MIDI_CLIENT_ID`
- `MIDI_CLIENT_SECRET`
- `KUBECONFIG` (for kubectl access)

## Troubleshooting

### "Token is invalid or expired"
- Generate a new token following step 3
- Update GitHub secrets
- Redeploy

### "Authentication failed"
- Ensure token includes `oauth:` prefix
- Verify Client ID is correct
- Make sure you used your **bot account** (not main account) when authorizing

### Bot connects but doesn't respond
- Check command prefix (default is `!`)
- Verify bot has permission to send messages
- Check logs for errors

### Token validation fails on startup
- This is just a warning - the bot will still try to connect
- Generate a new token when you can

## Required OAuth Scopes

The bot needs these scopes (already included in the URLs above):
- `chat:read` - Read chat messages
- `chat:edit` - Send chat messages  
- `channel:moderate` - Moderate chat (optional)

## Security Best Practices

1. **Never commit tokens** - Always use secrets
2. **Use a separate bot account** - Don't use your main account
3. **Monitor expiry warnings** - Set up log alerts
4. **Rotate tokens regularly** - Every ~50 days
5. **Revoke compromised tokens** - At dev.twitch.tv/console

## Additional Resources

- [Twitch Authentication Guide](https://dev.twitch.tv/docs/authentication/getting-tokens-oauth)
- [Twitch CLI](https://github.com/twitchdev/twitch-cli)
- [GitHub Encrypted Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
