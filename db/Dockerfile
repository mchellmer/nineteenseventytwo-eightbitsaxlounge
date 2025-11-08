# Use the official CouchDB image as the base image
FROM couchdb:latest

# Expose the default CouchDB port
EXPOSE 5984

# Set environment variables for CouchDB configuration
ENV COUCHDB_USER=admin
ENV COUCHDB_PASSWORD=${DB_COUCHDB_PASSWORD}

# Copy any custom configuration files if needed
# COPY local.ini /opt/couchdb/etc/local.ini

# Start CouchDB
CMD ["couchdb"]