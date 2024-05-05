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
        git gpg make wget python3 python3-pip z3 \
        default-jre dotnet-sdk-8.0 dotnet-runtime-6.0

# Install Z3
# RUN cd /plugin/Z3 && \
#     python3 scripts/mk_make.py && \
#     make -C build && \
#     make install -C build

# ----------------------------------------------------------------------------
# Project
# ----------------------------------------------------------------------------

# Compilation
RUN dotnet build ./Source/DafnyRepair.csproj --no-incremental && \
    ln -s bin/Debug/net6.0/DafnyRepair.dll . && \
    make exe -C Dafny


# ----------------------------------------------------------------------------
# Run
# ----------------------------------------------------------------------------

# Just keep running
CMD tail -f /dev/null