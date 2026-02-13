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

deploy-namespaces:
	ansible-playbook k8s-namespaces.yaml

deploy-storage:
	ansible-playbook k8s-storage.yaml

clean-cicd:
	@echo "Cleaning up GitHub Actions Runners..."
	@for runner in ~/actions-runner-*; do \
		if [ -d "$$runner" ]; then \
			echo "Cleaning up $$runner..."; \
			cd "$$runner" && sudo ./svc.sh stop || true; \
			cd "$$runner" && sudo ./svc.sh uninstall || true; \
			cd "$$runner" && ./config.sh remove || true; \
		fi; \
	done
	@rm -rf ~/actions-runner-* || true
	@echo "Cleanup complete. Run 'make init-cicd' to reconfigure."

init-cicd: scripts/github-runner.sh
	@echo "You need TWO registration tokens from GitHub:"
	@echo "  1. Go to: https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge/settings/actions/runners/new"
	@echo "  2. Copy the token for Runner 1"
	@echo "  3. Refresh the page and get a NEW token for Runner 2"
	@echo ""
	@read -p "Enter token for Runner 1: " TOKEN1; \
	read -p "Enter token for Runner 2: " TOKEN2; \
	bash scripts/github-runner.sh "$$TOKEN1" "$$TOKEN2"

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

update-ubuntu:
	@echo "Updating Ubuntu..."
	@if [ -n "$$SUDO_PASSWORD" ]; then \
		echo "$$SUDO_PASSWORD" > /tmp/.ansible_become_pass; \
		chmod 600 /tmp/.ansible_become_pass; \
		ansible-playbook update-ubuntu.yaml -vv --become-password-file /tmp/.ansible_become_pass; \
		rm -f /tmp/.ansible_become_pass; \
	else \
		ansible-playbook update-ubuntu.yaml -K; \
	fi

update-kubernetes:
	@echo "Updating Kubernetes..."
	@if [ -n "$$SUDO_PASSWORD" ]; then \
		echo "$$SUDO_PASSWORD" > /tmp/.ansible_become_pass; \
		chmod 600 /tmp/.ansible_become_pass; \
		if [ -n "$$KUBERNETES_VERSION" ]; then \
			ansible-playbook update-kubernetes.yaml -e "kubernetes_version=$$KUBERNETES_VERSION" -vv --become-password-file /tmp/.ansible_become_pass; \
		else \
			ansible-playbook k8s-kubernetes.yaml -vv --become-password-file /tmp/.ansible_become_pass; \
		fi; \
		rm -f /tmp/.ansible_become_pass; \
	else \
		if [ -n "$$KUBERNETES_VERSION" ]; then \
			ansible-playbook update-kubernetes.yaml -e "kubernetes_version=$$KUBERNETES_VERSION" -vv -K; \
		else \
			ansible-playbook k8s-kubernetes.yaml -vv -K; \
		fi; \
	fi

test-system:
	@echo "System Information:"
	@echo "--------------------"
	@echo "Hostname: $$(hostname)"
	@echo "OS Version: $$(lsb_release -d | cut -f2)"
	@echo "Kernel Version: $$(uname -r)"
	@echo "Architecture: $$(uname -m)"
	@echo "Uptime: $$(uptime -p)"