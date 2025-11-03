.PHONY: deploy-kubernetes deploy-cni console-init init-ansible-vault

ansible-docker:
	ansible-playbook k8s-docker.yaml

ansible-kubernetes:
	ansible-playbook k8s-kubernetes.yaml -vv

ansible-kubernetes-join:
	ansible-playbook k8s-kubernetes.yaml -vv --skip-tags "install_kubernetes,reset_kubeadm,init_kubeadm"

ansible-netplan:
	ansible-playbook k8s-netplan.yaml

deploy-kubernetes:
	make ansible-docker
	make ansible-kubernetes

deploy-cni:
	ansible-playbook k8s-flannel.yaml

deploy-ingress:
	ansible-playbook k8s-ingress-nginx.yaml

deploy-loadbalancer:
	ansible-playbook k8s-metallb.yaml

deploy-monitoring:
	if ! grep -q "grafana_password" $$HOME/nineteenseventytwo-eightbitsaxlounge/group_vars/all/vault.yml; then \
		read -p "Enter the grafana token: " grafana_password; \
		echo; \
		ansible-vault encrypt_string \
		--vault-password-file=$$HOME/vault_password.txt \
		--encrypt-vault-id default \
		"$$grafana_password" \
		--name "grafana_password" \
		--output temp_vault.yml; \
		echo >> $$HOME/nineteenseventytwo-eightbitsaxlounge/group_vars/all/vault.yml; \
		cat temp_vault.yml >> $$HOME/nineteenseventytwo-eightbitsaxlounge/group_vars/all/vault.yml; \
		rm temp_vault.yml; \
	fi;
	ansible-playbook k8s-monitoring.yaml

init-cicd: scripts/gitlabs-runner.sh
	bash scripts/github-runner.sh

init-console: scripts/init.sh
	bash scripts/init.sh

init-console-ansible-vault: scripts/ansible-vault-init.sh
	bash scripts/ansible-vault-init.sh

init-console-config:
	ansible-playbook k8s-console.yaml

init-nodes:
	export ANSIBLE_HOST_KEY_CHECKING=False && ansible-playbook k8s-nodes.yaml --ask-pass

scripts/ansible-vault-init.sh:
	chmod +x scripts/ansible-vault-init.sh

scripts/init.sh:
	chmod +x scripts/init.sh

scripts/github-runner.sh:
	chmod +x scripts/github-runner.sh

test-system:
	@echo "System Information:"
	@echo "--------------------"
	@echo "Hostname: $$(hostname)"
	@echo "OS Version: $$(lsb_release -d | cut -f2)"
	@echo "Kernel Version: $$(uname -r)"
	@echo "Architecture: $$(uname -m)"
	@echo "Uptime: $$(uptime -p)"