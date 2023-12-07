all: dafny z3 plugin

dafny:
	make -C ../dafny/

z3:
	cd ../z3
	mkdir build
	cd build
	cmake -G "Unix Makefiles" ../
	make -j4
	cd ../Plugin

plugin:
	rm -f Plugin.dll
	dotnet build
	ln -s bin/Debug/net6.0/Plugin.dll .
