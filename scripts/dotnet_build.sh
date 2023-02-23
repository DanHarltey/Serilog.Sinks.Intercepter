#!/bin/bash
set -e

dotnet restore ../
dotnet build ../ --configuration Release --no-restore
dotnet test ../ --configuration Release --no-build --verbosity normal
dotnet pack ../ --configuration Release --no-build
