#!/bin/bash
set -euo pipefail

vault_pass_file_path="$HOME/vault_password.txt"
vault_file_path="$HOME/nineteenseventytwo-eightbitsaxlounge/group_vars/all/vault.yml"
ansible_config_path="$HOME/ansible.cfg"
ansible_log_path="$HOME/ansible.log"

mkdir -p "$(dirname "$vault_file_path")"

# Create/overwrite ansible.cfg idempotently
cat > "$ansible_config_path" <<EOL
[defaults]
vault_password_file = $vault_pass_file_path
log_path = $ansible_log_path
stdout_callback = yaml
EOL

sudo mkdir -p /etc/ansible
sudo cp -f "$ansible_config_path" /etc/ansible/ansible.cfg

# Prompt secrets
read -s -p "Enter a password for the vault: " vault_password; echo
read -s -p "Enter Wi-Fi password: " wifi_password; echo
read -s -p "Enter become pass: " become_pass; echo
read -p  "Enter ansible_default_ipv4_address: " ansible_default_ipv4_address; echo

# Write vault password with strict perms
umask 077
printf "%s\n" "$vault_password" > "$vault_pass_file_path"

# Rebuild vault.yml from scratch (no duplicates)
tmp_vault="$(mktemp)"
trap 'rm -f "$tmp_vault"' EXIT

ansible-vault encrypt_string \
  --vault-password-file="$vault_pass_file_path" \
  --encrypt-vault-id default \
  "$wifi_password" \
  --name "bearden_wifi_pass" >> "$tmp_vault"

echo >> "$tmp_vault"

ansible-vault encrypt_string \
  --vault-password-file="$vault_pass_file_path" \
  --encrypt-vault-id default \
  "$become_pass" \
  --name "ansible_become_password" >> "$tmp_vault"

echo >> "$tmp_vault"

ansible-vault encrypt_string \
  --vault-password-file="$vault_pass_file_path" \
  --encrypt-vault-id default \
  "$ansible_default_ipv4_address" \
  --name "ansible_default_ipv4_address" >> "$tmp_vault"

# Atomically replace the vault file
mv -f "$tmp_vault" "$vault_file_path"

echo "Vault and config updated idempotently."