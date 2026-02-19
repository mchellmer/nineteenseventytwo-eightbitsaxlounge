docker-build:
	docker build -t eightbitsaxlounge-overlay:local .

docker-run:
	docker run --rm -p 3000:3000 eightbitsaxlounge-overlay:local

deploy:
	kubectl apply -f k8s/deployment.yaml
