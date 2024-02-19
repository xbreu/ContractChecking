all: dafny plugin
setup: clones

dafny:
	make -C ../dafny/

plugin:
	rm -f Plugin.dll
	dotnet build
	ln -s bin/Debug/net6.0/Plugin.dll .

clones:
	git clone https://github.com/pythonnet/pythonnet ../pythonnet
	git clone https://github.com/xbreu/dafny ../dafny-custom
