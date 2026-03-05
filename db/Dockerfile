# Use the official CouchDB image as the base image
FROM couchdb:latest

# Expose the default CouchDB port
EXPOSE 5984

# Set environment variables for CouchDB configuration
ENV COUCHDB_USER=admin
ENV COUCHDB_PASSWORD=${DB_COUCHDB_PASSWORD}

# Copy initialization script
COPY init-couchdb.sh /usr/local/bin/init-couchdb.sh
RUN chmod +x /usr/local/bin/init-couchdb.sh

# Use our initialization script as entrypoint
ENTRYPOINT ["/usr/local/bin/init-couchdb.sh"]