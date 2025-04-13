#!/bin/bash

projectName="Interpolator.Host"

dotnet watch \
  --no-hot-reload \
  --project ./$projectName/$projectName.csproj \
  --configuration "Debug" \
  -- \
  run
