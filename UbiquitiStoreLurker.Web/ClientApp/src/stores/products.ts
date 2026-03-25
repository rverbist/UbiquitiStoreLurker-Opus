import { defineStore } from 'pinia';
import { ref } from 'vue';
import { api } from '@/api';
import type { Product, StockStatusChangedEvent, PollCycleCompletedEvent } from '@/types';

export const useProductStore = defineStore('products', () => {
  const products = ref<Product[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const pollingProductIds = ref<Set<number>>(new Set());

  async function fetchAll() {
    loading.value = true;
    error.value = null;
    try {
      products.value = await api.products.list();
    } catch (e) {
      error.value = String(e);
    } finally {
      loading.value = false;
    }
  }

  async function addProduct(url: string) {
    const product = await api.products.create(url);
    products.value.push(product);
    return product;
  }

  async function removeProduct(id: number) {
    await api.products.remove(id);
    const idx = products.value.findIndex(p => p.id === id);
    if (idx !== -1) products.value.splice(idx, 1);
  }

  async function toggleActive(id: number, isActive: boolean) {
    const updated = await api.products.update(id, { isActive });
    const idx = products.value.findIndex(p => p.id === id);
    if (idx !== -1) products.value[idx] = updated;
  }

  function applyStockStatusChanged(evt: StockStatusChangedEvent) {
    const product = products.value.find(p => p.id === evt.productId);
    if (product) {
      product.currentState = evt.toState;
      product.lastStateChangeAtUtc = evt.detectedAtUtc;
    }
    pollingProductIds.value.delete(evt.productId);
  }

  function applyPollCycleCompleted(evt: PollCycleCompletedEvent) {
    const product = products.value.find(p => p.id === evt.productId);
    if (product) {
      product.pollCount++;
      product.lastPollAtUtc = evt.completedAtUtc;
    }
    pollingProductIds.value.delete(evt.productId);
  }

  function markPolling(productId: number) {
    pollingProductIds.value.add(productId);
  }

  return { products, loading, error, pollingProductIds, fetchAll, addProduct, removeProduct, toggleActive, applyStockStatusChanged, applyPollCycleCompleted, markPolling };
});
