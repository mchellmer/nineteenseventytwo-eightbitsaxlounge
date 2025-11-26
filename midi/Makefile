.PHONY: init-midi-host deploy-midi-api

init-midi-host:
	ansible-playbook midi-init.yaml --ask-pass

deploy-midi-api:
	ansible-playbook hosts midi-deploy.yaml