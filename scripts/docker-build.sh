#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

IMAGE_NAME="mkat"
TAG="${1:-local}"

echo "Building ${IMAGE_NAME}:${TAG}..."
docker build -t "${IMAGE_NAME}:${TAG}" "$PROJECT_ROOT"
echo "Done. Image: ${IMAGE_NAME}:${TAG}"
