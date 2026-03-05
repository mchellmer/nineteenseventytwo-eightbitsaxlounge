FROM ubuntu:22.04
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates curl git jq gnupg apt-transport-https python3 python3-pip \
    golang-go build-essential ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# Install Trivy
RUN curl -sSL https://raw.githubusercontent.com/aquasecurity/trivy/main/contrib/install.sh | sh -s -- -b /usr/local/bin v0.69.1

# Install GitHub CLI
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg && \
    chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null && \
    apt-get update && apt-get install -y gh && rm -rf /var/lib/apt/lists/*

# Python linters
RUN pip3 install --no-cache-dir flake8

RUN useradd -m scanner
WORKDIR /workspace
COPY --chown=scanner:scanner ./scripts/run-scan.sh /scripts/run-scan.sh
RUN chmod +x /scripts/run-scan.sh || true
USER scanner
ENTRYPOINT ["/bin/bash","-c","/scripts/run-scan.sh"]
