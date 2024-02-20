docker: build run

build:
	docker build --tag 'plugin' .

run:
	docker run -d plugin tail -f /dev/null

# Only run `make clean` if you have no other docker containers running
clean:
	docker ps -aq | xargs docker stop | xargs docker rm
	docker system prune -a -f