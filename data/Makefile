deploy-data-api:
	ansible-playbook data-go-couchdb.yaml

docker-build:
	docker build -t eightbitsaxlounge-data .

docker-run:
	docker run -d --name eightbitsaxlounge-data -p 8080:8080 eightbitsaxlounge-data