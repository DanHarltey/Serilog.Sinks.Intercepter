#!/bin/bash
set -e

mkdir ../release

dotnet --info > ../release/dotnet_info.txt

find ../tests/Serilog.Sinks.Intercepter.Tests/TestResults -name coverage.info -exec cp "{}" ../release  \;

find ../src/Serilog.Sinks.Intercepter/bin/Release -name "Serilog.Sinks.Intercepter.*.nupkg" -exec cp "{}" ../release/Serilog.Sinks.Intercepter.nupkg \;

gh release create $1 \
  "../release/Serilog.Sinks.Intercepter.nupkg" \
  "../release/coverage.info#Code coverage report" \
  "../release/dotnet_info.txt#Built with" \
  --draft \
  --generate-notes