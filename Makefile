# ----------------------------------------------------------------------------
# Necessary dependencies
# ----------------------------------------------------------------------------
setup: dafny pythonnet daikon

dafny:
ifeq ($(wildcard ./Dafny/.*),)
	git clone --depth 1 https://github.com/xbreu/dafny Dafny
endif

pythonnet:
ifeq ($(wildcard ./PythonNet/.*),)
	git clone --depth 1 https://github.com/pythonnet/pythonnet PythonNet
endif

daikon:
ifeq ($(wildcard ./Daikon/.*),)
	wget https://plse.cs.washington.edu/daikon/download/daikon-5.8.18.tar.gz
	tar -xvzf daikon-5.8.18.tar.gz
	rm daikon-5.8.18.tar.gz
	mv daikon-5.8.18 Daikon
endif

z3:
ifeq ($(wildcard ./Z3/.*),)
	git clone --depth 1 https://github.com/Z3Prover/z3 Z3
endif