#!/bin/bash
set -e

gh release create $1 \
  "../release/Serilog.Sinks.Intercepter.nupkg" \
  "../release/Serilog.Sinks.Intercepter.snupkg" \
  "../release/coverage.xml#Code coverage report" \
  "../release/dotnet_info.txt#Built with" \
  --draft \
  --generate-notes