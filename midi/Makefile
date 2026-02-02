.PHONY: init-midi-host deploy-midi-pc deploy-midi-api docker-build data-init data-upload

init-midi-host:
	ansible-playbook midi-init.yaml --ask-pass

deploy-midi-pc:
	ansible-playbook midi-pc-deploy.yaml -vv

deploy-midi-api:
	ansible-playbook midi-api-deploy.yaml -vv

data-init:
	ansible-playbook midi-data-init.yaml -vv

data-upload:
	@if [ -z "$(UPLOAD_TYPE)" ]; then \
		echo "Error: UPLOAD_TYPE not set. Use: make data-upload UPLOAD_TYPE=effects|devices"; \
		exit 1; \
	fi
	UPLOAD_TYPE=$(UPLOAD_TYPE) ansible-playbook midi-data-upload.yaml -vv

docker-build:
	docker build -t eightbitsaxlounge-midi .
