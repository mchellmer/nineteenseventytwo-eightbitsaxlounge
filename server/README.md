# 1972-Server
iaas and kubernetes cluster config for 1972
- assumes 1 console host and 3 node hosts
- install ansible on console
- using ansible to setup kubernetes cluster on nodes with 1 master and 2 workers via kubeadm/kubectl

# Setup servers
1. Boot up - Boot of latest ubuntu (tested on 24.4) on rpi (tested on rpi4 - console/rpi5 - nodes)
   - set config via imager:
     - hostname - to match /group_vars/all/vars.yaml
     - wifi name and pass
     - region settings
     - user/pass
   - BEFORE BOOTING NODES ONLY (don't apply to console) - setup cgroup in config and cmdline files on sd card
     - add cgroup_memory=1 cgroup_enable=memory cgroup_enable=hugetlb to /cmdline.txt
     - add dtoverlay=vc4-kms-v3d,cma-256 to /config.txt
   - AFTER BOOTING
     - enable dhcp on eth0 in netplan, add the following to /etc/netplan/50-cloud-init.yaml (don't use tabs!):
         ```yaml
           network: 
            version: 2
            ethernets:
              eth0:
                dhcp4: true
         ```
   - After boot - ssh to retrieve details (unless you know them already) for /group_vars/all/vars.yaml
     - eth0 mac address - `ip a`
     - wifi ip - set this to static values in your router, otherwise retrieve with `ip a`
     - eth0 ip - same

2. On console host - get code via `git clone https://github.com/mchellmer/1972-Server.git`
    - adjust /group_vars/all.yaml to match your network settings
        - boot into each pi or e.g. my router gui shows all pis with ip addresses and mac addresses for each
        - consider setting static ips via router or dhcp server

3. Setup CI/CD
    - Install and configure a GitHub Actions Runner for CI/CD pipelines.
    - Run the following command to set up the runner:
      ```bash
      make init-cicd
      ```
    - You will be prompted to provide the GitHub action Runner token. Obtain this token from your GitLab project under **Settings > Runners**.
    - Note that server pipelines requiring ansible vault secrets should load the vault file from server to runner environment

4. Init console
    - Updates/upgrades and install ansible/ansible vault on console host, generate secrets on server
    - installs ansible and adds secrets to vault
        - you will be prompted for the following so have them ready:
            - a vault password - save this in order to access the vault
              ```bash
              # The following command will generate a random 32 character password
              openssl rand -base64 32
              ```
            - the wifi hash from /etc/netplan/50-cloud-init.yaml.network.wifis.wlan0.access-points.<wifi name>.auth.password
            - an ansible become password - this is the password some user ansible will run as, in these scripts it's for 'mchellmer'
            - an ansible default ip address to setup egress to some ip
    - Setup console via ansible
        - this sets the ansible host as a dhcp server serving ip addresses to nodes
        - configures ip tables for kubernetes traffic allowing bridge traffic between console and nodes

    - ```bash
      sudo apt update
      sudo apt install make
      make init-console
      ```
    - after reboot - populate the ansible vault with the secrets
      ```bash
      make init-console-ansible-vault
      ```
    - after reboot - configure the console
      ```bash
      make init-console-config
      ```

5. Deploy Namespaces
  - Use the following command to deploy the Kubernetes namespaces for the app and environments:
    ```bash
    make deploy-namespaces
    ```

6. Deploy Monitoring
  - kubernetes monitoring via grafana
  - setup grafana account and activate kubernetes monitoring
  - set server name and options in ansible playbook - use Kubernetes > Configuration in Grafana to generate scripts
  - init: run make command on console server and provide token
  ```bash
  make deploy-monitoring
  ```
  - releases: increment versions/monitoring.txt and push change

7. Deploy Message Broker
  - Deploy an in-cluster NATS server with JetStream enabled (durable event storage).
  - Apply the manifest to your target namespace (dev/prod):
    ```bash
    kubectl -n eightbitsaxlounge-dev apply -f state/k8s/
    ```
  - In-cluster connection string: `nats://nats:4222` (use `NATS_URL` or `NATS_SERVERS` in pods)
  - Quick test (uses the `nats-box` image):
    ```bash
    kubectl run --rm -i --tty nats-test --image=natsio/nats-box --restart=Never -- nats sub test & nats pub test "hello"
    ```
  - Persistence: the StatefulSet creates a PVC per pod (1Gi default). To change the storage class or size, edit `state/k8s/nats.yaml` `volumeClaimTemplates`.
  - Scaling: JetStream requires careful configuration for HA — run 3+ replicas and configure clustering/routes before increasing replicas.

# Test
- ingress - apply the files/manifests/nginxtest.yaml and try to curl from nodes/another machine on the same subnet
- ci/cd - adjust the .github/workflows/test.yaml file to match your repo and branch and push, it should output os info

# Troubleshoot
Kubectl connection refused
- ensure config exists
- ensure swapoff
- ensure kubelet is running

Apt update fails to find kubernetes sources
- rm /etc/apt/keyrings/kubernetes-apt-keyring.gpg and retry