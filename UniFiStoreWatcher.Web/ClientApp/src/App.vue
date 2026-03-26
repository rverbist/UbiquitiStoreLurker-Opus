<script setup lang="ts">
import { RouterView, RouterLink } from 'vue-router';
</script>

<template>
  <header class="app-header">
    <div class="app-header__brand">
      <svg width="20" height="20" viewBox="0 0 24 24" fill="var(--accent)">
        <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/>
      </svg>
      <span>Stock Monitor</span>
    </div>
    <nav class="app-header__nav">
      <RouterLink to="/" class="nav-link">Monitor</RouterLink>
      <RouterLink to="/setup" class="nav-link">Setup</RouterLink>
    </nav>
  </header>

  <main class="app-main">
    <RouterView />
  </main>
</template>

<style scoped>
.app-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  height: 52px;
  background: var(--surface);
  border-bottom: 1px solid var(--border);
  position: sticky;
  top: 0;
  z-index: 100;
}

.app-header__brand {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 15px;
  color: var(--text);
}

.app-header__nav {
  display: flex;
  gap: 4px;
}

.nav-link {
  padding: 6px 14px;
  border-radius: var(--radius);
  color: var(--text-muted);
  font-weight: 500;
  transition: color var(--transition), background var(--transition);
}

.nav-link:hover,
.nav-link.router-link-active {
  color: var(--text);
  background: var(--surface-hover);
}

.app-main {
  flex: 1;
  padding: 24px;
  max-width: 1400px;
  margin: 0 auto;
  width: 100%;
}
</style>
