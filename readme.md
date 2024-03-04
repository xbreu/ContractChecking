# Contract Repair for Dafny

## Setup

This repository contains a Docker container application. All the commands needed should be provided in the [Makefile](./Makefile).

After cloning the repository you can run `make setup`. This will do the following in the local folder:
- Clone the used Dafny and Python.NET repositories
- Download and extract the Daikon tool application

After this, you can start the container inside VSCode, for that, go to the `Remote Explorer` tab and click on `reopen the current folder in a container`. Wait a little bit and open the [test file](./test.dfy) to verify the plugin running.