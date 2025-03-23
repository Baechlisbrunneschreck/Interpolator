#!/bin/bash

dotnet watch \
  --no-hot-reload \
  --project ./Interpolator.Host/Interpolator.Host.csproj \
  -- \
  run \
  --configuration "Debug"
