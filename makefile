all: dafny plugin

dafny:
	make -C ../dafny/

plugin:
	rm -f Plugin.dll
	dotnet build
	ln -s bin/Debug/net6.0/Plugin.dll .
