# ----------------------------------------------------------------------------
# Container
# ----------------------------------------------------------------------------

# Configuration
FROM ubuntu:22.04
WORKDIR /plugin

# Setup
COPY . .

# ----------------------------------------------------------------------------
# Necessary dependencies
# ----------------------------------------------------------------------------

# Install Packages
RUN apt-get update && \
    apt-get install -y \
        git gpg make wget python3-pip default-jre \
        dotnet-sdk-8.0 dotnet-runtime-6.0

# ----------------------------------------------------------------------------
# Project
# ----------------------------------------------------------------------------

# Compilation
RUN dotnet build ./Source/Plugin.csproj && \
    ln -s bin/Debug/net6.0/Plugin.dll . && \
    make exe -C Dafny