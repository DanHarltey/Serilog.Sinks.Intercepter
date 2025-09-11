#!/bin/bash
set -e

rm -rf ../release
mkdir ../release

dotnet --info > ../release/dotnet_info.txt

dotnet restore ../
dotnet build ../ --configuration Release --no-restore -p:ContinuousIntegrationBuild=true

if [ $1 = "test_coverage" ] 
then
  dotnet test ../ --configuration Release --no-build --verbosity normal --collect:"Code Coverage;Format=cobertura" --settings:"../.runsettings"
  find ../tests/Serilog.Sinks.Intercepter.Tests/TestResults/ -name "*.cobertura.xml" -type f -exec mv {} ../release/coverage.xml \;
else
  dotnet test ../ --configuration Release --no-build --verbosity normal
fi

dotnet pack ../ --configuration Release --no-build

cp ../src/Serilog.Sinks.Intercepter/bin/Release/Serilog.Sinks.Intercepter.*.nupkg ../release/Serilog.Sinks.Intercepter.nupkg
cp ../src/Serilog.Sinks.Intercepter/bin/Release/Serilog.Sinks.Intercepter.*.snupkg ../release/Serilog.Sinks.Intercepter.snupkg