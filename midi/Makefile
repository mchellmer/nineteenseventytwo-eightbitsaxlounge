.PHONY: init-midi-host deploy-midi-pc deploy-midi-api docker-build

init-midi-host:
	ansible-playbook midi-init.yaml --ask-pass

deploy-midi-pc:
	ansible-playbook midi-pc-deploy.yaml

deploy-midi-api:
	ansible-playbook midi-api-deploy.yaml

docker-build:
	docker build -t eightbitsaxlounge-midi .