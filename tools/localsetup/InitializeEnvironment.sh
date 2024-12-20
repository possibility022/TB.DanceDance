#!/bin/bash

# Log message for Docker
echo "================================="
echo "🚀 Hello from the Docker container!"
echo "📅 Current Date & Time: $(date)"
echo "🖥️ Hostname: $(hostname)"
echo "================================="

# Environment variables for PostgreSQL connection
DB_HOST=${DB_HOST:-"host.docker.internal"}
DB_PORT=${DB_PORT:-"5432"}
DB_NAME=${DB_NAME:-"dancedance"}
IDENT_DBNAME=${IDENT_DBNAME:-"identitystore"}
DB_USER=${DB_USER:-"postgres"}
DB_PASSWORD=${DB_PASSWORD:-"rgFraWIuyxONqWCQ71wh"}
RETRY_COUNT=15 
RETRY_DELAY=5

echo "DB_HOST is: $DB_HOST"

# Export password to PGPASSWORD for non-interactive login
export PGPASSWORD=$DB_PASSWORD

# Function to check database connection
check_db() {
    psql -h $DB_HOST -p $DB_PORT -U $DB_USER -c "\q"
}

# Retry loop
for i in $(seq 1 $RETRY_COUNT); do
    if check_db; then
        echo "Database is up and running."
        break
    else
        echo "Attempt $i failed. Retrying in $RETRY_DELAY seconds..."
        sleep $RETRY_DELAY
    fi
done

if ! check_db; then
    echo "Failed to connect to the database after $RETRY_COUNT attempts."
    exit 1
fi

echo "Running further database operations..."

# Check if the database exists
DB_EXISTS=$(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -lqt | cut -d \| -f 1 | grep -w $DB_NAME | wc -l)

# Create the database if it doesn't exist
if [ $DB_EXISTS -eq 0 ]; then
  echo "Database $DB_NAME does not exist. Creating..."
  createdb -h $DB_HOST -p $DB_PORT -U $DB_USER $DB_NAME
else
  echo "Database $DB_NAME already exists. Skipping creation."
fi

# Check if the IDENT DB exists
DB_EXISTS=$(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -lqt | cut -d \| -f 1 | grep -w $IDENT_DBNAME | wc -l)

# Create the database if it doesn't exist
if [ $DB_EXISTS -eq 0 ]; then
  echo "Database $IDENT_DBNAME does not exist. Creating..."
  createdb -h $DB_HOST -p $DB_PORT -U $DB_USER $IDENT_DBNAME
else
  echo "Database $IDENT_DBNAME already exists. Skipping creation."
fi

echo "Executing migration scripts."
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$IDENT_DBNAME" -f "persistedGrant-migrations.sql"
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$IDENT_DBNAME" -f "configuration-migrations.sql"
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$IDENT_DBNAME" -f "identityStore-migrations.sql"
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "danceDb-migrations.sql"

echo "Executing seed scripts."
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$IDENT_DBNAME" -f "identity-data-seed.sql"
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$IDENT_DBNAME" -f "oauth-data-seed.sql"
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "dance-data-seed.sql"

dotnet BlobSeedProgram.dll

echo "✅ All SQL scripts executed successfully."
