$target = "rverbist@proxmox:/opt/docker/apps/UnifiStoreWatcher"

scp ".env.Production" "$target/.env.Production"
scp ".env" "$target/.env"
scp "compose.yaml" "$target/compose.yaml"
scp "Dockerfile" "$target/Dockerfile"
