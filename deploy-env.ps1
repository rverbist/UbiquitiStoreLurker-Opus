$target = "rverbist@proxmox:/opt/docker/apps/UnifiAssistant"

scp ".env.Production" "$target/.env.Production"
scp ".env" "$target/.env"
