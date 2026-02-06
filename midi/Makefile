# CI/CD Variables (set by GitHub Actions)
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= eightbitsaxlounge-dev
GITHUB_REPOSITORY_OWNER ?= mchellmer
IMAGE_BASE = ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-midi

.PHONY: help test build-container deploy-k8s build-pc deploy-pc
.PHONY: init-midi-host deploy-midi-pc deploy-midi-api docker-build data-init data-upload request-seteffect

help:
	@echo "MIDI API - Build and Deployment Targets"
	@echo ""
	@echo "Standard CI/CD targets:"
	@echo "  test             - Run .NET unit tests"
	@echo "  build-container  - Build and push Docker container for K8s"
	@echo "  deploy-k8s       - Deploy MIDI API to Kubernetes"
	@echo "  build-pc         - Build Windows self-contained binary"
	@echo "  deploy-pc        - Deploy MIDI to Windows PC"
	@echo ""
	@echo "Legacy targets (kept for compatibility):"
	@echo "  init-midi-host   - Initialize MIDI host"
	@echo "  deploy-midi-pc   - Alias for deploy-pc"
	@echo "  deploy-midi-api  - Alias for deploy-k8s"
	@echo "  docker-build     - Local Docker build (no push)"
	@echo "  data-init        - Initialize MIDI data"
	@echo "  data-upload      - Upload MIDI data (requires UPLOAD_TYPE)"
	@echo "  request-seteffect - Send MIDI effect change request"
	@echo ""
	@echo "Environment Variables:"
	@echo "  VERSION=<version>                 - Override version (default: from version.txt)"
	@echo "  NAMESPACE=<namespace>             - K8s namespace (default: eightbitsaxlounge-dev)"
	@echo "  GITHUB_REPOSITORY_OWNER=<owner>   - Container registry owner"

# Standard CI/CD targets
test:
	@echo "Setting up .NET and running tests..."
	@if ! command -v dotnet >/dev/null 2>&1 || [ ! -d "$$HOME/.dotnet" ]; then \
		echo "Installing .NET SDK to $$HOME/.dotnet..."; \
		curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --install-dir $$HOME/.dotnet; \
	fi
	@echo "Running .NET tests in NineteenSeventyTwo.EightBitSaxLounge.Midi..."
	cd NineteenSeventyTwo.EightBitSaxLounge.Midi && \
	$${HOME}/.dotnet/dotnet restore NineteenSeventyTwo.EightBitSaxLounge.Midi.sln && \
	$${HOME}/.dotnet/dotnet build NineteenSeventyTwo.EightBitSaxLounge.Midi.sln --no-restore --configuration Release && \
	$${HOME}/.dotnet/dotnet test NineteenSeventyTwo.EightBitSaxLounge.Midi.sln --no-build --configuration Release --verbosity normal

build-container:
	@echo "Building and pushing MIDI container $(IMAGE_BASE):$(VERSION)..."
	docker build -t "$(IMAGE_BASE):$(VERSION)" -t "$(IMAGE_BASE):latest" .
	docker push "$(IMAGE_BASE):$(VERSION)"
	docker push "$(IMAGE_BASE):latest"

deploy-k8s:
	@echo "Deploying MIDI API to Kubernetes namespace: $(NAMESPACE)..."
	ansible-playbook midi-api-deploy.yaml -vv

build-pc:
	@echo "Building Windows self-contained binary version $(VERSION)..."
	@mkdir -p publish
	cd NineteenSeventyTwo.EightBitSaxLounge.Midi && \
	dotnet publish NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.csproj \
		--configuration Release \
		--runtime win-x64 \
		--self-contained true \
		-p:PublishSingleFile=false \
		-p:Version=$(VERSION) \
		--output ../publish/bin
	cd publish/bin && tar -czf ../midi-api-$(VERSION).tar.gz *
	@echo "Package created: publish/midi-api-$(VERSION).tar.gz"

deploy-pc:
	@echo "Deploying MIDI to Windows PC..."
	ansible-playbook midi-pc-deploy.yaml -vv

# Legacy targets (kept for backward compatibility)
init-midi-host:
	ansible-playbook midi-init.yaml --ask-pass

deploy-midi-pc: deploy-pc

deploy-midi-api: deploy-k8s

data-init:
	ansible-playbook midi-data-init.yaml -vv

data-upload:
	@if [ -z "$(UPLOAD_TYPE)" ]; then \
		echo "Error: UPLOAD_TYPE not set. Use: make data-upload UPLOAD_TYPE=effects|devices"; \
		exit 1; \
	fi
	UPLOAD_TYPE=$(UPLOAD_TYPE) ansible-playbook midi-data-upload.yaml -vv

request-seteffect:
	@if [ -z "$(DEVICE)" ] || [ -z "$(DEVICE_EFFECT)" ] || [ -z "$(DEVICE_EFFECT_SETTING)" ]; then \
		echo "Error: Required parameters not set."; \
		echo "Usage: make request-seteffect DEVICE=VentrisDualReverb DEVICE_EFFECT=ReverbEngineA DEVICE_EFFECT_SETTING=ReverbEngine [VALUE=<int>] [SELECTION=<string>]"; \
		exit 1; \
	fi
	DEVICE=$(DEVICE) \
	DEVICE_EFFECT=$(DEVICE_EFFECT) \
	DEVICE_EFFECT_SETTING=$(DEVICE_EFFECT_SETTING) \
	VALUE=$(VALUE) \
	SELECTION=$(SELECTION) \
	ansible-playbook midi-request-seteffect.yaml -vv

docker-build:
	docker build -t eightbitsaxlounge-midi .
