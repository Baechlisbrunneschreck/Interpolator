#!/bin/bash

set -e

IMAGE_NAME="hanneszbinden/baechlisbrunneschreck-interpolator"
CURRENT_VERSION=0.0.1

docker build \
  -t $IMAGE_NAME:latest \
  -f ./Interpolator.Host/Dockerfile \
  .

docker tag \
  $IMAGE_NAME:latest \
  $IMAGE_NAME:$CURRENT_VERSION

docker push \
  $IMAGE_NAME:latest

docker push \
  $IMAGE_NAME:$CURRENT_VERSION