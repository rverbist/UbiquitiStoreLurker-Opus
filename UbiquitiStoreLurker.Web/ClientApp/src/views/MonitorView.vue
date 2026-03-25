<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useProductStore } from '@/stores/products';
import { useSignalR } from '@/composables/useSignalR';
import ProductCard from '@/components/ProductCard.vue';
import ProductDetailPanel from '@/components/ProductDetailPanel.vue';
import AddProductForm from '@/components/AddProductForm.vue';
import StockPopup from '@/components/StockPopup.vue';
import ConfettiOverlay from '@/components/ConfettiOverlay.vue';
import type { Product, StockStatusChangedEvent } from '@/types';
import { StockState } from '@/types';

const store = useProductStore();
const selected = ref<Product | null>(null);
const stockPopupEvent = ref<StockStatusChangedEvent | null>(null);
const confettiRef = ref<InstanceType<typeof ConfettiOverlay> | null>(null);

useSignalR(onStockStatusChanged);

onMounted(() => store.fetchAll());

function onStockStatusChanged(evt: StockStatusChangedEvent) {
  if (evt.toState === StockState.InStock) {
    stockPopupEvent.value = evt;
    confettiRef.value?.fire();
  }
}
</script>

<template>
  <div class="monitor">
    <div class="monitor__toolbar">
      <div>
        <h1 class="monitor__title">Ubiquiti Stock Monitor</h1>
        <p class="monitor__subtitle">Watching {{ store.products.length }} product{{ store.products.length !== 1 ? 's' : '' }}</p>
      </div>
      <AddProductForm />
    </div>

    <div v-if="store.loading" class="monitor__empty">Loading products…</div>

    <div v-else-if="store.error" class="monitor__error">{{ store.error }}</div>

    <div v-else-if="store.products.length === 0" class="monitor__empty">
      <p>No products being monitored.</p>
      <p>Paste a Ubiquiti EU store URL above to get started.</p>
    </div>

    <div v-else class="monitor__grid">
      <ProductCard
        v-for="product in store.products"
        :key="product.id"
        :product="product"
        @select="selected = $event"
      />
    </div>

    <ProductDetailPanel :product="selected" @close="selected = null" />
    <StockPopup :event="stockPopupEvent" @dismiss="stockPopupEvent = null" />
    <ConfettiOverlay ref="confettiRef" />
  </div>
</template>

<style scoped>
.monitor__toolbar {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 16px;
  margin-bottom: 24px;
}

.monitor__title {
  margin: 0 0 4px;
  font-size: 22px;
  font-weight: 700;
}

.monitor__subtitle {
  margin: 0;
  color: var(--text-muted);
  font-size: 13px;
}

.monitor__grid {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.monitor__empty {
  text-align: center;
  padding: 60px 0;
  color: var(--text-muted);
}

.monitor__error {
  padding: 12px;
  background: rgba(244, 67, 54, 0.1);
  border: 1px solid var(--danger);
  border-radius: var(--radius);
  color: var(--danger);
}
</style>
