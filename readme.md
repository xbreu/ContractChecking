# Contract Repair for Dafny

## Setup

This repository contains a Docker container application. All the commands needed should be provided in the [Makefile](./Makefile).

After cloning the repository you can just run `make init`. This will do the following in the local folder:
- Clone the used Dafny and Python.NET repositories
- Download and extract the Daikon tool application
- Build a Docker image and run it

After this, if you want to run it again, without building it again. You can just use the command `make container`.

## Development

To develop the project you may use the VSCode text editor, paired with its Dev Containers extension. If you do that, you can connect to the running container.

By using `Ctrl + Shift + P`, you can type `Dev Containers: Attach to Running Container...` and select the one named `plugin`.