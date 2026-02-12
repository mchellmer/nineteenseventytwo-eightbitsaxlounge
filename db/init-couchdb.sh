#!/bin/bash
# CouchDB initialization entrypoint
# This script starts CouchDB and initializes system databases

set -e

# Start CouchDB in the background
echo "Starting CouchDB..."
/docker-entrypoint.sh /opt/couchdb/bin/couchdb &
COUCHDB_PID=$!

# Wait for CouchDB to be ready
echo "Waiting for CouchDB to be ready..."
until curl -s http://localhost:5984/_up > /dev/null 2>&1; do
  sleep 1
done

echo "CouchDB is ready. Initializing system databases..."

# Create system databases
curl -s -X PUT "http://localhost:5984/_users" \
  -u "${COUCHDB_USER}:${COUCHDB_PASSWORD}" \
  > /dev/null 2>&1 || echo "_users already exists"

curl -s -X PUT "http://localhost:5984/_replicator" \
  -u "${COUCHDB_USER}:${COUCHDB_PASSWORD}" \
  > /dev/null 2>&1 || echo "_replicator already exists"

curl -s -X PUT "http://localhost:5984/_global_changes" \
  -u "${COUCHDB_USER}:${COUCHDB_PASSWORD}" \
  > /dev/null 2>&1 || echo "_global_changes already exists"

echo "CouchDB initialization complete."

# Bring CouchDB to foreground
wait $COUCHDB_PID
