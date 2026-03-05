# PowerShell version of test_integration.sh
# Simple integration smoke script (requires NATS at $NATS_URL and overlay running at $OVERLAY_URL)

$natsUrl = if ($env:NATS_URL) { $env:NATS_URL } else { 'nats://127.0.0.1:4222' }
$natsUser = if ($env:NATS_USER) { $env:NATS_USER } else { '' }
$natsPass = if ($env:NATS_PASS) { $env:NATS_PASS } else { '' }
$overlayUrl = if ($env:OVERLAY_URL) { $env:OVERLAY_URL } else { 'http://localhost:3000/grid.html' }

Write-Host "Publishing sample messages to $natsUrl (user=$natsUser pass=$natsPass)"

# Publish initial sample
$env:NATS_URL = $natsUrl
$env:NATS_USER = $natsUser
$env:NATS_PASS = $natsPass

& node ./scripts/publish-sample.js
if ($LASTEXITCODE -ne 0) { exit 1 }

# Publish a few different subjects using the node publisher
$subjects = @(
    'overlay.engine|{"value":"room"}',
    'overlay.time|{"value":3}',
    'overlay.dial1|{"value":7}',
    'overlay.dial2|{"value":10}',
    'overlay.delay|{"value":1}'
)

foreach ($s in $subjects) {
    $parts = $s -split '\|'
    $subj = $parts[0]
    $payload = $parts[1]
    Write-Host " -> $subj $payload"
    
    $env:SUBJECT = $subj
    $env:PAYLOAD = $payload
    & node ./scripts/publish-sample.js
    
    if ($LASTEXITCODE -ne 0) { exit 1 }
    Start-Sleep -Milliseconds 200
}

Write-Host "Open $overlayUrl in your browser or OBS browser source to see updates."
