<script setup lang="ts">
import { reactive, watch } from 'vue';
import { useSettings } from '@/composables/useSettings';

const { settings, saving, saveSettings } = useSettings();

const form = reactive({
  nickname: '',
  pollIntervalMinSeconds: 30,
  pollIntervalMaxSeconds: 90,
  maxRetryAttempts: 3,
  retryBaseDelaySeconds: 5,
  minDelayBetweenRequestsSeconds: 5,
});

watch(settings, (s) => {
  if (s) Object.assign(form, s);
}, { immediate: true });

async function handleSave() {
  await saveSettings({ ...form });
}
</script>

<template>
  <section class="settings-section">
    <h2 class="settings-section__title">Global Settings</h2>
    <form @submit.prevent="handleSave" class="settings-form">
      <div class="form-row">
        <label>Nickname</label>
        <input v-model="form.nickname" type="text" placeholder="My Stock Monitor" />
      </div>
      <div class="form-row">
        <label>Min Poll Interval (s)</label>
        <input v-model.number="form.pollIntervalMinSeconds" type="number" min="10" max="3600" />
      </div>
      <div class="form-row">
        <label>Max Poll Interval (s)</label>
        <input v-model.number="form.pollIntervalMaxSeconds" type="number" min="10" max="3600" />
      </div>
      <div class="form-row">
        <label>Max Retry Attempts</label>
        <input v-model.number="form.maxRetryAttempts" type="number" min="0" max="10" />
      </div>
      <div class="form-row">
        <label>Retry Base Delay (s)</label>
        <input v-model.number="form.retryBaseDelaySeconds" type="number" min="1" max="300" />
      </div>
      <div class="form-row">
        <label>Min Delay Between Requests (s)</label>
        <input v-model.number="form.minDelayBetweenRequestsSeconds" type="number" min="1" max="60" />
      </div>
      <div class="form-actions">
        <button type="submit" class="btn-primary" :disabled="saving">
          {{ saving ? 'Saving…' : 'Save Settings' }}
        </button>
      </div>
    </form>
  </section>
</template>

<style scoped>
.settings-section { margin-bottom: 32px; }
.settings-section__title { font-size: 16px; font-weight: 700; margin: 0 0 16px; border-bottom: 1px solid var(--border); padding-bottom: 8px; }
.settings-form { display: flex; flex-direction: column; gap: 12px; max-width: 460px; }
.form-row { display: flex; flex-direction: column; gap: 4px; }
.form-row label { font-size: 12px; color: var(--text-muted); font-weight: 500; }
.form-actions { margin-top: 8px; }
</style>
