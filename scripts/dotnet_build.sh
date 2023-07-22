#!/bin/bash
set -e

dotnet restore ../
dotnet build ../ --configuration Release --no-restore
dotnet test ../ --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage;Format=lcov"
dotnet pack ../ --configuration Release --no-build
