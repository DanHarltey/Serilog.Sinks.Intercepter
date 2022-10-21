#!/bin/bash
set -e

dotnet restore ../
dotnet build ../ --configuration Release --nologo --no-restore
dotnet test ../ --configuration Release --nologo --no-build --verbosity normal
