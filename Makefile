docker-build:
	docker build \
		-t fdns-ms-dotnet-object \
		--rm \
		--force-rm=true \
		--build-arg OBJECT_PORT=9090 \
		--build-arg OBJECT_MONGO_CONNECTION_STRING=mongodb://mongo:27017 \
		--build-arg OBJECT_MONGO_USE_SSL=false \
		--build-arg OBJECT_FLUENTD_HOST=fluentd \
		--build-arg OBJECT_FLUENTD_PORT=24224 \
		--build-arg OBJECT_PROXY_HOSTNAME= \
		--build-arg OBJECT_IMMUTABLE= \
		--build-arg OAUTH2_ACCESS_TOKEN_URI= \
		--build-arg OAUTH2_PROTECTED_URIS= \
		--build-arg OAUTH2_CLIENT_ID= \
		--build-arg OAUTH2_CLIENT_SECRET= \
		--build-arg SSL_VERIFYING_DISABLE=false \
		.

docker-test:
	docker build \
		-t fdns-ms-dotnet-object-tests \
		-f tests/Dockerfile.test \
		--rm \
		--force-rm=true \
		.
	docker rmi fdns-ms-dotnet-object-tests

docker-run: docker-start
docker-start:
	docker-compose up -d
	docker run -d \
		-p 9090:9090 \
		--network=fdns-ms-dotnet-object_default  \
		--name=fdns-ms-dotnet-object_main \
		fdns-ms-dotnet-object

docker-stop:
	docker stop fdns-ms-dotnet-object_main || true
	docker rm fdns-ms-dotnet-object_main || true
	docker-compose down

docker-restart:
	make docker-stop 2>/dev/null || true
	make docker-start