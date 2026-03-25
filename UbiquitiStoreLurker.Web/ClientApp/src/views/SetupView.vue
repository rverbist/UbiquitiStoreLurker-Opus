<script setup lang="ts">
import { ref, onMounted } from "vue";
import { api } from "@/api";
import { useSettings } from "@/composables/useSettings";
import { usePushNotifications } from "@/composables/usePushNotifications";
import GlobalSettingsForm from "@/components/setup/GlobalSettingsForm.vue";
import NotificationProviderCard from "@/components/setup/NotificationProviderCard.vue";
import type { NotificationConfigDto } from "@/types";

const { fetchSettings, settings } = useSettings();
const configs = ref<NotificationConfigDto[]>([]);

const {
  supported: pushSupported,
  subscribed: pushSubscribed,
  loading: pushLoading,
  error: pushError,
  subscribe,
  unsubscribe,
  sendTest,
  checkSubscribed,
} = usePushNotifications();

onMounted(async () => {
  await fetchSettings();
  configs.value = await api.notifications.getConfigs();
  await checkSubscribed();
});

function onConfigUpdated(updated: NotificationConfigDto) {
  const idx = configs.value.findIndex((c) => c.id === updated.id);
  if (idx !== -1) configs.value[idx] = updated;
}

async function handleSubscribe() {
  if (settings.value?.vapidPublicKey) {
    await subscribe(settings.value.vapidPublicKey);
  }
}
</script>

<template>
  <div class="setup">
    <h1 class="setup__title">Setup</h1>

    <GlobalSettingsForm />

    <section class="settings-section">
      <h2 class="settings-section__title">Notification Providers</h2>
      <NotificationProviderCard
        v-for="config in configs"
        :key="config.id"
        :config="config"
        @updated="onConfigUpdated"
      />
      <p v-if="configs.length === 0" style="color: var(--text-muted)">
        Loading providers…
      </p>
    </section>

    <section class="settings-section">
      <h2 class="settings-section__title">Browser Push Notifications</h2>
      <div class="push-section">
        <p v-if="!pushSupported" style="color: var(--text-muted)">
          Browser Push is not supported in this browser.
        </p>
        <template v-else>
          <p
            style="font-size: 13px; color: var(--text-muted); margin: 0 0 12px"
          >
            Status:
            <strong>{{
              pushSubscribed ? "Subscribed ✓" : "Not subscribed"
            }}</strong>
          </p>
          <div style="display: flex; gap: 8px">
            <button
              v-if="!pushSubscribed"
              class="btn-primary"
              :disabled="pushLoading || !settings?.vapidPublicKey"
              @click="handleSubscribe"
            >
              {{ pushLoading ? "Subscribing…" : "Enable Push Notifications" }}
            </button>
            <button
              v-else
              class="btn-ghost"
              :disabled="pushLoading"
              @click="unsubscribe"
            >
              Disable Push
            </button>
            <button
              v-if="pushSubscribed"
              class="btn-ghost"
              :disabled="pushLoading"
              @click="sendTest"
            >
              Send Test
            </button>
          </div>
          <p
            v-if="pushError"
            style="color: var(--danger); font-size: 12px; margin: 8px 0 0"
          >
            {{ pushError }}
          </p>
        </template>
      </div>
    </section>
  </div>
</template>

<style scoped>
.setup {
  max-width: 720px;
}
.setup__title {
  margin: 0 0 24px;
  font-size: 22px;
  font-weight: 700;
}
.settings-section {
  margin-bottom: 32px;
}
.settings-section__title {
  font-size: 16px;
  font-weight: 700;
  margin: 0 0 16px;
  border-bottom: 1px solid var(--border);
  padding-bottom: 8px;
}
</style>
