FROM mcr.microsoft.com/dotnet/sdk:6.0-jammy AS build
WORKDIR /build
ARG VERSION=1.0.0
COPY . .
RUN dotnet restore \
  && dotnet build -c Release --no-restore \
  && dotnet test -c Release --no-build
