#!/bin/sh
set -eu
case "${DB_ENGINE}" in
  derby) exec java -cp /opt/db/derby.jar:/opt/db/derbynet.jar:/opt/db/derbyshared.jar:/opt/db/derbytools.jar org.apache.derby.drda.NetworkServerControl start -h 0.0.0.0 -p 1527 ;;
  hsqldb) exec java -cp /opt/db/hsqldb.jar org.hsqldb.server.Server --database.0 file:/data/hogwarts --dbname.0 hogwarts --port 9001 ;;
  h2) exec java -cp /opt/db/h2.jar org.h2.tools.Server -tcp -tcpAllowOthers -tcpPort 9092 -ifNotExists ;;
  *) echo "DB_ENGINE desconocido: ${DB_ENGINE}" >&2; exit 2 ;;
esac
