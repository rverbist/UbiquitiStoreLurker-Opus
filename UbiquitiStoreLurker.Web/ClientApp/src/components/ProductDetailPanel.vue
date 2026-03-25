<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import gsap from 'gsap';
import type { Product, StockCheck } from '@/types';
import { StockState } from '@/types';
import { api } from '@/api';

const props = defineProps<{ product: Product | null }>();
const emit = defineEmits<{ close: [] }>();

const history = ref<StockCheck[]>([]);
const loading = ref(false);
const panelEl = ref<HTMLElement | null>(null);

watch(() => props.product, async (p) => {
  if (!p) { history.value = []; return; }
  loading.value = true;
  try {
    const result = await api.products.history(p.id);
    history.value = result.items;
  } finally {
    loading.value = false;
  }
  await nextTick();
  if (panelEl.value) {
    gsap.from(panelEl.value, {
      x: '100%',
      duration: 0.3,
      ease: 'power3.out',
    });
  }
}, { immediate: true });

function stateLabel(s: StockState) {
  return ['Unknown', 'In Stock', 'Out of Stock', 'Indeterminate'][s] ?? '?';
}

function formatDate(d: string) {
  return new Date(d).toLocaleString();
}
</script>

<template>
  <Teleport to="body">
    <div v-if="product" class="panel-backdrop" @click="emit('close')">
      <aside ref="panelEl" class="panel" @click.stop>
        <header class="panel__header">
          <h2 class="panel__title">{{ product.name ?? product.url }}</h2>
          <button class="btn-ghost" @click="emit('close')">✕</button>
        </header>

        <section class="panel__body">
          <div class="panel__meta">
            <a :href="product.url" target="_blank" rel="noopener" class="panel__link">
              Open in store ↗
            </a>
            <span>Polls: {{ product.pollCount }}</span>
            <span>Errors: {{ product.errorCount }}</span>
          </div>

          <h3>Poll History</h3>
          <div v-if="loading" class="panel__loading">Loading…</div>
          <table v-else class="panel__table">
            <thead>
              <tr>
                <th>Time</th>
                <th>State</th>
                <th>Parser</th>
                <th>Status</th>
                <th>ms</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="check in history" :key="check.id">
                <td>{{ formatDate(check.createdAtUtc) }}</td>
                <td>{{ stateLabel(check.detectedState) }}</td>
                <td>{{ check.parserStrategy ?? '—' }}</td>
                <td>{{ check.httpStatusCode ?? '—' }}</td>
                <td>{{ check.durationMs }}</td>
              </tr>
              <tr v-if="history.length === 0">
                <td colspan="5" style="text-align:center;color:var(--text-muted)">No history yet</td>
              </tr>
            </tbody>
          </table>
        </section>
      </aside>
    </div>
  </Teleport>
</template>

<style scoped>
.panel-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.5);
  z-index: 200;
  display: flex;
  justify-content: flex-end;
}

.panel {
  width: min(600px, 90vw);
  height: 100vh;
  background: var(--surface);
  border-left: 1px solid var(--border);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.panel__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid var(--border);
}

.panel__title {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.panel__body {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.panel__meta {
  display: flex;
  gap: 16px;
  align-items: center;
  margin-bottom: 20px;
  font-size: 13px;
  color: var(--text-muted);
}

.panel__link { color: var(--accent); }

.panel__loading { color: var(--text-muted); text-align: center; padding: 20px; }

.panel__table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
}

.panel__table th,
.panel__table td {
  text-align: left;
  padding: 8px;
  border-bottom: 1px solid var(--border);
}

.panel__table th { color: var(--text-muted); font-weight: 600; }
</style>
