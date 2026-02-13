# 1972-Server
iaas and kubernetes cluster config for 1972
- assumes 1 console host and 3 node hosts
- install ansible on console
- using ansible to setup kubernetes cluster on nodes with 1 master and 2 workers via kubeadm/kubectl

# Setup servers
1. Boot up - Boot of latest ubuntu (tested on 25.10) on rpi (tested on rpi4 - console/rpi5 - nodes)
   - set config via imager:
     - hostname - to match /group_vars/all/vars.yaml
     - wifi name and pass
     - region settings
     - user/pass
   - BEFORE BOOTING NODES ONLY (don't apply to console) - setup cgroup in config and cmdline files on sd card
     - add cgroup_memory=1 cgroup_enable=memory cgroup_enable=hugetlb to /cmdline.txt
     - add dtoverlay=vc4-kms-v3d,cma-256 to /config.txt
   - AFTER BOOTING
     - check enabled dhcp on eth0 in netplan, add the following to /etc/netplan/50-cloud-init.yaml (don't use tabs!):
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

2. On console host - get code via `git clone https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge.git`
    - adjust /group_vars/all.yaml to match your network settings
        - boot into each pi or e.g. my router gui shows all pis with ip addresses and mac addresses for each
        - consider setting static ips via router or dhcp server

3. Init raspberry pis
    - Updates/upgrades and install ansible/ansible vault on console host, generate secrets on server
    - ```bash
      sudo apt update
      sudo apt install make
      make init-console
      ```
    - console will reboot
    - Setup console via ansible
      - setup ansible host file and base config
      - sets the ansible host as a dhcp server serving ip addresses to nodes
      ```bash
      make init-console-config
      ```
    - Setup nodes via ansible
      - setups up ssh access between console and nodes
      - configures ip tables for kubernetes traffic allowing bridge traffic between console and nodes
      ```bash
      make init-nodes
      ```
      - nodes will reboot

4. Deploy Kubernetes
  - install container runtime (docker), kubeadm, tools, and join nodes
  ```bash
  make deploy-kubernetes
  ```

5. Setup CI/CD
    - Install and configure GitHub Actions Runners for CI/CD pipelines, follow instructions to provide join tokens
    - follow prompts to configure runners
    - Run the following command to set up the runner:
      ```bash
      cd server
      make init-cicd
      ```

5. Deploy Namespaces
  - Use the following command to deploy the Kubernetes namespaces for the app and environments:
    ```bash
    make deploy-namespaces
    ```

6. Deploy CNI
   - use flannel as CNI
   ```bash
   make deploy-cni
   ```

7. Deploy loadbalancer
   - use metallb
   ```bash
   make deploy-loadbalancer
   ```

8. Deploy ingress
   - use nginx
   ```bash
   make deploy-ingress
   ```

9. Deploy Storage
  - Install Longhorn distributed storage system for persistent volumes
  - Provides replicated storage across worker nodes with web UI management
    ```bash
    make deploy-storage
    ```
  - Access Longhorn UI:
    ```bash
    kubectl port-forward -n longhorn-system svc/longhorn-frontend 8080:80
    # Open http://localhost:8080
    ```

## Cluster Infrastructure Choices

### Storage: Longhorn

**What it does**: Provides distributed block storage with automatic replication across worker nodes.

**Why Longhorn:**
- Cloud-native storage designed for bare-metal and edge deployments (Raspberry Pi friendly)
- Automatic data replication (2 replicas across worker nodes for high availability)
- Dynamic provisioning - automatically creates PersistentVolumes when applications request storage via PersistentVolumeClaims
- Web UI for monitoring volumes, replicas, and node health
- Snapshots and backup support for disaster recovery

**How it works:**
1. Applications create a PersistentVolumeClaim (PVC) requesting storage with `storageClassName: longhorn`
2. Longhorn automatically provisions a PersistentVolume (PV) from the storage pool (`/var/lib/longhorn` on each worker)
3. Data is replicated in real-time across multiple nodes (default: 2 replicas)
4. If a node fails, data remains accessible from replicas on other nodes

**Management:**
```bash
# Access Longhorn UI
kubectl port-forward -n longhorn-system svc/longhorn-frontend 8080:80
# Open http://localhost:8080

# Check storage class
kubectl get storageclass

# View persistent volumes
kubectl get pv
kubectl get pvc -A
```

### CNI: Flannel

**What it does**: Container Network Interface - handles pod-to-pod networking across nodes.

**Why Flannel:**
- Simple, lightweight CNI plugin ideal for small clusters
- Proven reliability and minimal resource overhead
- Easy to troubleshoot and maintain
- Supports ARM architecture (Raspberry Pi)

**How it works:**
- Creates an overlay network using VXLAN tunneling
- Assigns pod IP ranges to each node from the cluster CIDR
- Routes traffic between pods across nodes transparently
- No additional configuration needed for most workloads

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