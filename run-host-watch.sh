#!/bin/bash

projectName="Interpolator.Host"

dotnet watch \
  --no-hot-reload \
  --project ./$projectName/$projectName.csproj \
  -- \
  run \
  --configuration "Debug"
