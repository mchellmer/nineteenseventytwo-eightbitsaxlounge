.PHONY: deploy-kubernetes deploy-cni console-init init-ansible-vault init-cicd

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
	ansible-playbook k8s-monitoring.yaml

deploy-namespaces:
	ansible-playbook k8s-namespaces.yaml

clean-cicd:
	@echo "Cleaning up GitHub Actions Runner..."
	cd ~/actions-runner && sudo ./svc.sh stop || true
	cd ~/actions-runner && sudo ./svc.sh uninstall || true
	cd ~/actions-runner && rm -f .credentials .credentials_rsaparams .runner .runner_migrated
	cd ~/actions-runner && rm -rf _diag
	@echo "Cleanup complete. Run 'make init-cicd' to reconfigure."

init-cicd: scripts/github-runner.sh
	@read -p "Enter the GitHub Actions Runner token: " TOKEN; \
	bash scripts/github-runner.sh $$TOKEN

init-console: scripts/init.sh
	bash scripts/init.sh

init-console-ansible-vault: scripts/ansible-vault-init.sh
	bash scripts/ansible-vault-init.sh

shutdown-cluster:
	@echo "⚠️  Initiating cluster shutdown sequence..."
	@if [ -n "$$SUDO_PASSWORD" ]; then \
		echo "$$SUDO_PASSWORD" > /tmp/.ansible_become_pass; \
		chmod 600 /tmp/.ansible_become_pass; \
		ansible-playbook shutdown.yaml --become-password-file /tmp/.ansible_become_pass; \
		rm -f /tmp/.ansible_become_pass; \
	else \
		ansible-playbook shutdown.yaml -K; \
	fi

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