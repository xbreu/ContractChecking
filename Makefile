init: setup image container

# ----------------------------------------------------------------------------
# Docker commands
# ----------------------------------------------------------------------------
image:
	docker build --tag 'plugin' .

container:
	docker run -d plugin tail -f /dev/null

# Only run `make clean` if you have no other docker containers running
clean:
	docker ps -aq | xargs docker stop | xargs docker rm
	docker system prune -a -f
	rm -Rf .github .idea .mono bin obj

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