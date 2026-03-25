<script setup lang="ts">
import { ref } from 'vue';
import { useProductStore } from '@/stores/products';

const store = useProductStore();
const url = ref('');
const error = ref('');
const adding = ref(false);

async function handleSubmit() {
  if (!url.value.trim()) { error.value = 'URL is required'; return; }
  adding.value = true;
  error.value = '';
  try {
    await store.addProduct(url.value.trim());
    url.value = '';
  } catch (e) {
    error.value = String(e);
  } finally {
    adding.value = false;
  }
}
</script>

<template>
  <form class="add-form" @submit.prevent="handleSubmit">
    <input
      v-model="url"
      type="url"
      placeholder="https://eu.store.ui.com/eu/en/products/..."
      class="add-form__input"
      :disabled="adding"
    />
    <button type="submit" class="btn-primary" :disabled="adding">
      {{ adding ? 'Adding...' : '+ Add Product' }}
    </button>
    <p v-if="error" class="add-form__error">{{ error }}</p>
  </form>
</template>

<style scoped>
.add-form {
  display: flex;
  gap: 10px;
  align-items: flex-start;
  flex-wrap: wrap;
}
.add-form__input { max-width: 500px; }
.add-form__error { width: 100%; margin: 4px 0 0; color: var(--danger); font-size: 12px; }
</style>
