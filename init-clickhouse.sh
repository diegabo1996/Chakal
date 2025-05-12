#!/bin/bash

# Set default values
CLICKHOUSE_HOST=${CLICKHOUSE_HOST:-localhost}
CLICKHOUSE_PORT=${CLICKHOUSE_PORT:-8123}
CLICKHOUSE_USER=${CLICKHOUSE_USER:-default}
CLICKHOUSE_PASSWORD=${CLICKHOUSE_PASSWORD:-Chakal123!}

echo "Initializing ClickHouse database on ${CLICKHOUSE_HOST}:${CLICKHOUSE_PORT}..."
echo "This might take a few seconds..."

# Wait for ClickHouse to be available
for i in {1..30}; do
    if curl --silent -X GET "http://${CLICKHOUSE_HOST}:${CLICKHOUSE_PORT}/ping" > /dev/null; then
        echo "ClickHouse is available!"
        break
    fi
    
    if [ $i -eq 30 ]; then
        echo "Timeout waiting for ClickHouse to be available. Exiting."
        exit 1
    fi
    
    echo "Waiting for ClickHouse to be available... (${i}/30)"
    sleep 1
done

# Execute the initialization script
AUTH=""
if [ -n "${CLICKHOUSE_USER}" ] && [ -n "${CLICKHOUSE_PASSWORD}" ]; then
    AUTH="--user ${CLICKHOUSE_USER}:${CLICKHOUSE_PASSWORD}"
fi

cat sql/chakal_clickhouse.sql | curl ${AUTH} -s -X POST "http://${CLICKHOUSE_HOST}:${CLICKHOUSE_PORT}/" --data-binary @-

if [ $? -eq 0 ]; then
    echo "ClickHouse database initialized successfully."
else
    echo "Error initializing ClickHouse database."
    exit 1
fi

exit 0 