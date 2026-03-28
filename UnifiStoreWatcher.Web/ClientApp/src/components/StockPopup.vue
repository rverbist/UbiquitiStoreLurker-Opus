<script setup lang="ts">
import { ref, onMounted } from 'vue';
import gsap from 'gsap';
import type { StockStatusChangedEvent } from '@/types';

const props = defineProps<{
  event: StockStatusChangedEvent | null;
}>();

const emit = defineEmits<{ dismiss: [] }>();

const popupEl = ref<HTMLElement | null>(null);

onMounted(() => {
  if (popupEl.value) {
    gsap.from(popupEl.value, {
      scale: 0.85,
      opacity: 0,
      y: -20,
      duration: 0.4,
      ease: 'back.out(1.7)',
    });
  }
});

function handleDismiss() {
  if (popupEl.value) {
    gsap.to(popupEl.value, {
      scale: 0.9,
      opacity: 0,
      duration: 0.25,
      onComplete: () => emit('dismiss'),
    });
  } else {
    emit('dismiss');
  }
}
</script>

<template>
  <Teleport to="body">
    <div v-if="event" class="popup-backdrop">
      <div ref="popupEl" class="popup">
        <div class="popup__icon">🎉</div>
        <h2 class="popup__title">In Stock!</h2>
        <p class="popup__product">{{ event.productName ?? event.url }}</p>
        <div class="popup__actions">
          <a :href="event.url" target="_blank" rel="noopener" class="btn-primary popup__buy">
            View Product ↗
          </a>
          <button class="btn-ghost" @click="handleDismiss">Dismiss</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.popup-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.65);
  z-index: 300;
  display: flex;
  align-items: center;
  justify-content: center;
}

.popup {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 16px;
  padding: 36px 40px;
  text-align: center;
  max-width: 380px;
  width: 90vw;
  box-shadow: 0 20px 60px rgba(0, 111, 255, 0.25);
}

.popup__icon { font-size: 48px; margin-bottom: 12px; }

.popup__title {
  margin: 0 0 8px;
  font-size: 28px;
  font-weight: 700;
  color: var(--success);
}

.popup__product {
  margin: 0 0 24px;
  color: var(--text-muted);
  font-size: 14px;
  overflow-wrap: break-word;
}

.popup__actions {
  display: flex;
  gap: 10px;
  justify-content: center;
  flex-wrap: wrap;
}

.popup__buy { padding: 10px 24px; font-size: 14px; }
</style>
