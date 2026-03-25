import { ref, readonly } from 'vue';
import { api } from '@/api';
import type { AppSettingsDto } from '@/types';

const settings = ref<AppSettingsDto | null>(null);
const loading = ref(false);
const saving = ref(false);
const error = ref<string | null>(null);

async function fetchSettings() {
  loading.value = true;
  error.value = null;
  try {
    settings.value = await api.settings.get();
  } catch (e) {
    error.value = String(e);
  } finally {
    loading.value = false;
  }
}

async function saveSettings(patch: Partial<AppSettingsDto>) {
  saving.value = true;
  error.value = null;
  try {
    settings.value = await api.settings.update(patch);
    return true;
  } catch (e) {
    error.value = String(e);
    return false;
  } finally {
    saving.value = false;
  }
}

export function useSettings() {
  return {
    settings: readonly(settings),
    loading: readonly(loading),
    saving: readonly(saving),
    error: readonly(error),
    fetchSettings,
    saveSettings,
  };
}
