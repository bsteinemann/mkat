#!/usr/bin/env bash
set -euo pipefail

IMAGE_NAME="mkat"
TAG="${1:-local}"
CONTAINER_NAME="mkat-local"

# Stop existing container if running
if docker ps -q -f name="$CONTAINER_NAME" | grep -q .; then
  echo "Stopping existing container..."
  docker stop "$CONTAINER_NAME" && docker rm "$CONTAINER_NAME"
fi

echo "Starting ${IMAGE_NAME}:${TAG}..."
docker run -d \
  --name "$CONTAINER_NAME" \
  -p 8080:8080 \
  -v mkat-local-data:/data \
  -e MKAT_USERNAME="${MKAT_USERNAME:-admin}" \
  -e MKAT_PASSWORD="${MKAT_PASSWORD:-changeme}" \
  -e MKAT_TELEGRAM_BOT_TOKEN="${MKAT_TELEGRAM_BOT_TOKEN:-}" \
  -e MKAT_TELEGRAM_CHAT_ID="${MKAT_TELEGRAM_CHAT_ID:-}" \
  -e MKAT_LOG_LEVEL="${MKAT_LOG_LEVEL:-Information}" \
  "${IMAGE_NAME}:${TAG}"

echo "Container started: http://localhost:8080"
echo "Logs: docker logs -f $CONTAINER_NAME"
