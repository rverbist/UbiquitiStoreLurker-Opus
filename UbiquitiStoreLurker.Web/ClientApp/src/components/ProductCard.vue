<script setup lang="ts">
import { computed, ref, watch, onMounted } from 'vue';
import gsap from 'gsap';
import type { Product } from '@/types';
import { StockState } from '@/types';
import { useProductStore } from '@/stores/products';

const props = defineProps<{ product: Product }>();
const emit = defineEmits<{ select: [product: Product] }>();

const store = useProductStore();
const isPolling = computed(() => store.pollingProductIds.has(props.product.id));

const cardEl = ref<HTMLElement | null>(null);
const badgeEl = ref<HTMLElement | null>(null);

onMounted(() => {
  if (cardEl.value) {
    gsap.from(cardEl.value, {
      y: 20,
      opacity: 0,
      duration: 0.35,
      ease: 'power2.out',
    });
  }
});

watch(() => props.product.currentState, () => {
  if (badgeEl.value) {
    gsap.fromTo(badgeEl.value,
      { scale: 1.3 },
      { scale: 1, duration: 0.4, ease: 'elastic.out(1, 0.5)' }
    );
  }
});

const statusLabel = computed(() => {
  switch (props.product.currentState) {
    case StockState.InStock: return 'In Stock';
    case StockState.OutOfStock: return 'Out of Stock';
    case StockState.Indeterminate: return 'Unknown';
    default: return 'Checking...';
  }
});

const statusClass = computed(() => {
  switch (props.product.currentState) {
    case StockState.InStock: return 'badge--success';
    case StockState.OutOfStock: return 'badge--danger';
    default: return 'badge--muted';
  }
});

const hostname = computed(() => {
  try { return new URL(props.product.url).hostname; }
  catch { return props.product.url; }
});

const lastChecked = computed(() => {
  if (!props.product.lastPollAtUtc) return 'Never';
  const d = new Date(props.product.lastPollAtUtc);
  return d.toLocaleTimeString();
});

async function handleDelete(e: Event) {
  e.stopPropagation();
  if (!confirm(`Remove ${props.product.name ?? hostname.value}?`)) return;
  if (cardEl.value) {
    await gsap.to(cardEl.value, {
      rotate: 5,
      opacity: 0,
      x: 40,
      duration: 0.3,
      ease: 'power2.in',
    });
  }
  await store.removeProduct(props.product.id);
}

async function handleToggle(e: Event) {
  e.stopPropagation();
  await store.toggleActive(props.product.id, !props.product.isActive);
}
</script>

<template>
  <article
    ref="cardEl"
    class="product-card"
    :class="{ 'product-card--inactive': !product.isActive, 'product-card--polling': isPolling }"
    @click="emit('select', product)"
  >
    <div class="product-card__image">
      <img v-if="product.imageUrl" :src="product.imageUrl" :alt="product.name ?? 'Product'" />
      <div v-else class="product-card__image-placeholder">
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <rect x="3" y="3" width="18" height="18" rx="2"/>
          <path d="M9 13l3 3 6-6"/>
        </svg>
      </div>
    </div>

    <div class="product-card__body">
      <h3 class="product-card__name">{{ product.name ?? hostname }}</h3>
      <p class="product-card__url">{{ hostname }}</p>

      <div class="product-card__meta">
        <span ref="badgeEl" class="badge" :class="statusClass">
          <span v-if="isPolling" class="spinner" />
          {{ isPolling ? 'Polling...' : statusLabel }}
        </span>
        <span class="product-card__checked">{{ lastChecked }}</span>
      </div>
    </div>

    <div class="product-card__actions" @click.stop>
      <button class="btn-ghost" @click="handleToggle">
        {{ product.isActive ? 'Pause' : 'Resume' }}
      </button>
      <button class="btn-danger" @click="handleDelete">×</button>
    </div>
  </article>
</template>

<style scoped>
.product-card {
  display: flex;
  gap: 16px;
  align-items: center;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 16px;
  cursor: pointer;
  transition: border-color var(--transition), background var(--transition);
}

.product-card:hover {
  border-color: var(--accent);
  background: var(--surface-hover);
}

.product-card--inactive { opacity: 0.55; }

.product-card--polling { border-color: var(--warning); }

.product-card__image {
  width: 60px;
  height: 60px;
  flex-shrink: 0;
  border-radius: 6px;
  overflow: hidden;
  background: var(--surface-hover);
  display: flex;
  align-items: center;
  justify-content: center;
}

.product-card__image img { width: 100%; height: 100%; object-fit: cover; }
.product-card__image-placeholder { color: var(--text-muted); }

.product-card__body { flex: 1; min-width: 0; }

.product-card__name {
  margin: 0 0 2px;
  font-size: 14px;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.product-card__url {
  margin: 0 0 8px;
  font-size: 12px;
  color: var(--text-muted);
}

.product-card__meta {
  display: flex;
  align-items: center;
  gap: 10px;
}

.badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 2px 10px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 600;
}

.badge--success { background: rgba(47, 179, 67, 0.15); color: var(--success); }
.badge--danger  { background: rgba(244, 67, 54, 0.15);  color: var(--danger);  }
.badge--muted   { background: var(--surface-hover);      color: var(--text-muted); }

.product-card__checked { font-size: 12px; color: var(--text-muted); }

.product-card__actions {
  display: flex;
  gap: 8px;
  flex-shrink: 0;
}

.spinner {
  width: 8px;
  height: 8px;
  border: 2px solid currentColor;
  border-top-color: transparent;
  border-radius: 50%;
  display: inline-block;
  animation: spin 0.7s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }
</style>
