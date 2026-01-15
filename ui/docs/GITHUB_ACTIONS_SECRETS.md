# GitHub Actions Secrets Setup

Quick reference for managing Twitch tokens in GitHub Actions.

## Required Secrets

Add these in your GitHub repository: **Settings → Secrets and variables → Actions**

| Secret Name | Description | Example |
|------------|-------------|---------|
| `TWITCH_CLIENT_ID` | Your app's Client ID from dev.twitch.tv | `abc123xyz789` |
| `TWITCH_BOT_TOKEN` | Access token with `oauth:` prefix | `oauth:your_token_here` |
| `TWITCH_CHANNEL` | Your streaming channel name | `yourchannelname` |
| `MIDI_CLIENT_ID` | MIDI API client ID | `localhost` |
| `MIDI_CLIENT_SECRET` | MIDI API client secret | `your_secret` |
| `KUBECONFIG` | Kubernetes config for deployment | `<base64 kubeconfig>` |

## How to Get Tokens

See [TWITCH_SETUP_SIMPLE.md](TWITCH_SETUP_SIMPLE.md) for detailed instructions.

**Quick method using TwitchTokenGenerator:**
1. Go to [twitchtokengenerator.com](https://twitchtokengenerator.com)
2. Log in with your **BOT account**
3. Click "Bot Chat Token" → Authorize
4. Copy the access token
5. Format as: `oauth:your_access_token`

## Token Expiry Monitoring

The bot logs warnings when tokens are expiring:

```
⚠️  WARNING: Twitch token expires in 14.2 days.
⚠️  URGENT: Twitch token expires in 3.5 days! Update TWITCH_TOKEN in GitHub secrets immediately.
```

**Set up log monitoring** to alert you when these messages appear.

## Renewing Tokens

When you need to renew (every ~50-60 days):

1. Generate a new token (see above)
2. Update `TWITCH_BOT_TOKEN` secret in GitHub
3. Redeploy or wait for next automatic deployment

## Deployment Workflow Example

```yaml
name: Deploy UI to Kubernetes

on:
  push:
    branches: [main]
    paths: ['ui/**']
  workflow_dispatch:  # Allow manual triggers

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v2
      
      - name: Log in to GitHub Container Registry
        uses: docker/login-action@v2
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      
      - name: Build and push Docker image
        uses: docker/build-push-action@v4
        with:
          context: ./ui
          push: true
          tags: |
            ghcr.io/${{ github.repository_owner }}/eightbitsaxlounge-ui:latest
            ghcr.io/${{ github.repository_owner }}/eightbitsaxlounge-ui:${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
      
      - name: Set up kubectl
        uses: azure/setup-kubectl@v3
      
      - name: Configure kubectl
        run: |
          mkdir -p $HOME/.kube
          echo "${{ secrets.KUBECONFIG }}" | base64 -d > $HOME/.kube/config
      
      - name: Update Kubernetes secrets
        run: |
          kubectl create secret generic twitch-secrets \
            --namespace=eightbitsaxlounge \
            --from-literal=client-id="${{ secrets.TWITCH_CLIENT_ID }}" \
            --from-literal=token="${{ secrets.TWITCH_BOT_TOKEN }}" \
            --from-literal=midi-client-id="${{ secrets.MIDI_CLIENT_ID }}" \
            --from-literal=midi-client-secret="${{ secrets.MIDI_CLIENT_SECRET }}" \
            --dry-run=client -o yaml | kubectl apply -f -
      
      - name: Deploy to Kubernetes
        run: |
          # Update image in deployment
          kubectl set image deployment/ui-deployment \
            twitch-bot=ghcr.io/${{ github.repository_owner }}/eightbitsaxlounge-ui:${{ github.sha }} \
            --namespace=eightbitsaxlounge
          
          # Wait for rollout to complete
          kubectl rollout status deployment/ui-deployment \
            --namespace=eightbitsaxlounge \
            --timeout=5m
      
      - name: Verify deployment
        run: |
          kubectl get pods -n eightbitsaxlounge -l app=ui
          kubectl logs -n eightbitsaxlounge -l app=ui --tail=50
```

## Automated Token Expiry Alerts

Add a scheduled workflow to check token expiry:

```yaml
name: Check Twitch Token Expiry

on:
  schedule:
    - cron: '0 12 * * *'  # Daily at noon UTC
  workflow_dispatch:

jobs:
  check-token:
    runs-on: ubuntu-latest
    steps:
      - name: Check token status
        run: |
          # Validate token and get expiry info
          RESPONSE=$(curl -s -H "Authorization: OAuth ${{ secrets.TWITCH_BOT_TOKEN }}" \
            https://id.twitch.tv/oauth2/validate)
          
          EXPIRES_IN=$(echo $RESPONSE | jq -r '.expires_in')
          DAYS_LEFT=$((EXPIRES_IN / 86400))
          
          echo "Token expires in $DAYS_LEFT days"
          
          if [ $DAYS_LEFT -lt 7 ]; then
            echo "::error::URGENT: Twitch token expires in $DAYS_LEFT days!"
            exit 1
          elif [ $DAYS_LEFT -lt 14 ]; then
            echo "::warning::Twitch token expires in $DAYS_LEFT days"
          fi
```

This will create GitHub Actions warnings/errors when tokens are expiring soon.

## Best Practices

1. **Set calendar reminders** - Renew tokens every 50 days
2. **Monitor CI/CD logs** - Watch for expiry warnings
3. **Use workflow_dispatch** - Enable manual deployments for emergency token updates
4. **Test token updates** - Verify new tokens work before old ones expire
5. **Document the process** - Keep a runbook for token renewal

## Troubleshooting

### Secret not updating in pods
Kubernetes secrets don't auto-update in running pods. After updating secrets:
```bash
kubectl rollout restart deployment/ui-deployment -n eightbitsaxlounge
```

### Token validation fails in workflow
Check that:
- Token includes `oauth:` prefix
- No extra whitespace in secret value
- You're using the bot account's token, not your main account

### Deployment succeeds but bot doesn't connect
- Check pod logs: `kubectl logs -n eightbitsaxlounge -l app=ui`
- Verify secrets are correctly mounted
- Ensure token hasn't expired
