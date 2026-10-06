#!/usr/bin/env bash

set -u

# Keep the Unix socket inside the activation's writable temporary data
# directory. Some Flox PostgreSQL builds default to /run/postgresql, which may
# not exist or be writable for a user-local service.
postgres -D "$ORDREFLOW_PGDATA" -p "$PGPORT" -k "$ORDREFLOW_PGDATA" &
postgres_pid=$!

stop_postgres() {
  kill "$postgres_pid" 2>/dev/null || true
  wait "$postgres_pid" 2>/dev/null || true
  exit 0
}

trap stop_postgres TERM INT

until pg_isready -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" >/dev/null 2>&1; do
  if ! kill -0 "$postgres_pid" 2>/dev/null; then
    wait "$postgres_pid"
    exit 1
  fi

  sleep 0.1
done

# initdb creates the default postgres database. Create the application database
# once the server is accepting connections; it is removed with the temp data
# directory when the Flox activation ends.
PGDATABASE=postgres createdb -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" "$PGDATABASE" 2>/dev/null || true

wait "$postgres_pid"
