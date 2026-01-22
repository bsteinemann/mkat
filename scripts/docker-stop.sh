#!/usr/bin/env bash
set -euo pipefail

CONTAINER_NAME="mkat-local"

if docker ps -q -f name="$CONTAINER_NAME" | grep -q .; then
  echo "Stopping $CONTAINER_NAME..."
  docker stop "$CONTAINER_NAME" && docker rm "$CONTAINER_NAME"
  echo "Stopped."
else
  echo "No running container named $CONTAINER_NAME."
fi
