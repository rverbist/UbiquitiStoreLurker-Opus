<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { api } from '@/api';
import type { NotificationConfigDto } from '@/types';

const props = defineProps<{ config: NotificationConfigDto }>();
const emit = defineEmits<{ updated: [NotificationConfigDto] }>();

const isEnabled = ref(props.config.isEnabled);
const settingsJson = ref(props.config.settingsJson ?? '{}');
const saving = ref(false);
const error = ref('');
const expanded = ref(false);

watch(() => props.config, (c) => {
  isEnabled.value = c.isEnabled;
  settingsJson.value = c.settingsJson ?? '{}';
});

const isValidJson = computed(() => {
  try { JSON.parse(settingsJson.value); return true; }
  catch { return false; }
});

async function handleSave() {
  if (!isValidJson.value) { error.value = 'Settings must be valid JSON'; return; }
  saving.value = true;
  error.value = '';
  try {
    const updated = await api.notifications.updateConfig(props.config.id, {
      isEnabled: isEnabled.value,
      settingsJson: settingsJson.value,
    });
    emit('updated', updated);
  } catch (e) {
    error.value = String(e);
  } finally {
    saving.value = false;
  }
}

const providerIcons: Record<string, string> = {
  BrowserPush: '🔔',
  Email: '📧',
  SMS: '💬',
  TeamsWebhook: '👥',
  Discord: '🎮',
};

const icon = computed(() => providerIcons[props.config.providerType] ?? '📡');
</script>

<template>
  <div class="provider-card" :class="{ 'provider-card--active': isEnabled }">
    <div class="provider-card__header" @click="expanded = !expanded">
      <div class="provider-card__info">
        <span class="provider-card__icon">{{ icon }}</span>
        <div>
          <strong>{{ config.displayName }}</strong>
          <span class="provider-card__type">{{ config.providerType }}</span>
        </div>
      </div>
      <div class="provider-card__controls" @click.stop>
        <label class="toggle">
          <input type="checkbox" v-model="isEnabled" @change="handleSave" />
          <span class="toggle__track" />
        </label>
        <span class="provider-card__chevron" :class="{ rotated: expanded }">▾</span>
      </div>
    </div>

    <div v-if="expanded" class="provider-card__body">
      <label class="form-row">
        <span>Settings (JSON)</span>
        <textarea v-model="settingsJson" rows="6" />
        <span v-if="!isValidJson" class="error-text">Invalid JSON</span>
      </label>
      <div class="provider-card__actions">
        <button class="btn-primary" @click="handleSave" :disabled="saving || !isValidJson">
          {{ saving ? 'Saving…' : 'Save' }}
        </button>
        <span v-if="error" class="error-text">{{ error }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.provider-card {
  border: 1px solid var(--border);
  border-radius: var(--radius);
  overflow: hidden;
  background: var(--surface);
  margin-bottom: 8px;
  transition: border-color var(--transition);
}
.provider-card--active { border-color: var(--accent); }
.provider-card__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 16px;
  cursor: pointer;
}
.provider-card__header:hover { background: var(--surface-hover); }
.provider-card__info { display: flex; align-items: center; gap: 12px; }
.provider-card__icon { font-size: 20px; }
.provider-card__type { display: block; font-size: 11px; color: var(--text-muted); }
.provider-card__controls { display: flex; align-items: center; gap: 12px; }
.provider-card__chevron { color: var(--text-muted); transition: transform var(--transition); font-size: 16px; }
.provider-card__chevron.rotated { transform: rotate(180deg); }
.provider-card__body { padding: 16px; border-top: 1px solid var(--border); }
.provider-card__actions { display: flex; align-items: center; gap: 12px; margin-top: 12px; }
.form-row { display: flex; flex-direction: column; gap: 4px; font-size: 12px; color: var(--text-muted); }
.error-text { color: var(--danger); font-size: 12px; }
.toggle { position: relative; display: inline-flex; align-items: center; cursor: pointer; }
.toggle input { opacity: 0; width: 0; height: 0; position: absolute; }
.toggle__track {
  width: 36px; height: 20px;
  background: var(--border);
  border-radius: 10px;
  transition: background var(--transition);
}
.toggle input:checked ~ .toggle__track { background: var(--accent); }
</style>
