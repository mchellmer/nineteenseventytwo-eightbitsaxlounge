#!/bin/bash

sudo apt update
sudo apt upgrade -y
sudo apt -y install software-properties-common sshpass python3-pip

sudo apt -y install ansible

git config --global user.email "mchellmer@gmail.com"
git config --global user.name "Mark Hellmer"

sudo sed -i 's/^FONTSIZE=.*/FONTSIZE="16x32"/' /etc/default/console-setup

sudo reboot now