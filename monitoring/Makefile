# Variables
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= monitoring
CLUSTER_NAME ?= nineteenseventytwo

# Deploy monitoring stack
deploy:
	@echo "Deploying monitoring stack version $(VERSION)..."
	ansible-playbook k8s-monitoring.yaml; \

# Check Alloy agents
check-alloy:
	@echo "Checking Alloy agent status..."
	kubectl get pods -n $(NAMESPACE) -l app.kubernetes.io/name=alloy

# Get Alloy logs
logs-alloy:
	@echo "Getting Alloy agent logs..."
	kubectl logs -n $(NAMESPACE) -l app.kubernetes.io/name=alloy --tail=100

# Uninstall monitoring stack
uninstall:
	@echo "Uninstalling monitoring stack..."
	kubectl delete namespace $(NAMESPACE)
	helm uninstall grafana-k8s-monitoring -n $(NAMESPACE) || true

# Show monitoring configuration
show-config:
	@echo "Current monitoring configuration:"
	@echo "  Cluster: $(CLUSTER_NAME)"
	@echo "  Namespace: $(NAMESPACE)"
	@echo "  Version: $(VERSION)"
	helm get values grafana-k8s-monitoring -n $(NAMESPACE) || echo "Helm release not found"

# Help
help:
	@echo "Monitoring Makefile targets:"
	@echo "  deploy        - Deploy monitoring stack (auto-detects CI/CD or interactive mode)"
	@echo "  deploy-ci     - Deploy monitoring stack using vault file (deprecated, use deploy)"
	@echo "  check-helm    - Check Helm version on cluster nodes"
	@echo "  verify        - Check status of monitoring pods"
	@echo "  check-alloy   - Check Alloy agent pod status"
	@echo "  logs-alloy    - View recent Alloy agent logs"
	@echo "  uninstall     - Remove monitoring stack"
	@echo "  show-config   - Display current monitoring configuration"
	@echo "  help          - Show this help message"

.PHONY: deploy deploy-ci check-helm verify check-alloy logs-alloy uninstall show-config help
