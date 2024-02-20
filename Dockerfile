# Docker configuration
FROM ubuntu:22.04
WORKDIR /plugin

# Basic Packages
RUN apt-get update
RUN apt-get install -y git gpg make wget python3-pip
RUN apt-get install -y dotnet-sdk-8.0 dotnet-runtime-6.0 default-jre

# Visual Studio Code
# RUN wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > packages.microsoft.gpg
# RUN install -D -o root -g root -m 644 packages.microsoft.gpg /etc/apt/keyrings/packages.microsoft.gpg
# RUN sh -c 'echo "deb [arch=amd64,arm64,armhf signed-by=/etc/apt/keyrings/packages.microsoft.gpg] https://packages.microsoft.com/repos/code stable main" > /etc/apt/sources.list.d/vscode.list'
# RUN rm -f packages.microsoft.gpg
# RUN apt-get install -y apt-transport-https
# RUN apt-get update
# RUN apt-get install -y code

# Dafny
RUN git clone --depth 1 https://github.com/xbreu/dafny /dafny-custom

# Python .NET
RUN git clone --depth 1 https://github.com/pythonnet/pythonnet /pythonnet
# RUN pip install -e /pythonnet

# Setup
COPY . .

# Compilation
RUN make -C /dafny-custom/
# RUN cd /plugin
RUN dotnet build Plugin.csproj
RUN ln -s bin/Debug/net6.0/Plugin.dll .