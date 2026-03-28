import { ref, readonly } from "vue";
import { api } from "@/api";

const supported = "serviceWorker" in navigator && "PushManager" in window;
const subscribed = ref(false);
const loading = ref(false);
const error = ref<string | null>(null);

function urlBase64ToUint8Array(base64String: string): Uint8Array<ArrayBuffer> {
  const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
  const rawData = atob(base64);
  return Uint8Array.from(
    [...rawData].map((c) => c.charCodeAt(0)),
  ) as Uint8Array<ArrayBuffer>;
}

async function subscribe(vapidPublicKey: string) {
  if (!supported) {
    error.value = "Push notifications not supported";
    console.warn("[push] not supported");
    return;
  }
  loading.value = true;
  error.value = null;
  try {
    console.log("[push] waiting for service worker...");
    const reg = await navigator.serviceWorker.ready;
    console.log("[push] SW ready, subscribing...", reg);
    const sub = await reg.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(vapidPublicKey),
    });
    console.log("[push] browser subscription obtained", sub.endpoint);
    const json = sub.toJSON();
    console.log("[push] posting to /api/push/subscribe...");
    await api.post("/push/subscribe", {
      endpoint: sub.endpoint,
      p256dh: json.keys?.p256dh ?? "",
      auth: json.keys?.auth ?? "",
    });
    console.log("[push] subscribed successfully");
    subscribed.value = true;
  } catch (e) {
    console.error("[push] subscribe failed:", e);
    error.value = String(e);
  } finally {
    loading.value = false;
  }
}

async function unsubscribe() {
  if (!supported) return;
  loading.value = true;
  try {
    const reg = await navigator.serviceWorker.ready;
    const sub = await reg.pushManager.getSubscription();
    if (sub) {
      const json = sub.toJSON();
      await api.del("/push/unsubscribe", {
        endpoint: sub.endpoint,
        p256dh: json.keys?.p256dh ?? "",
        auth: json.keys?.auth ?? "",
      });
      await sub.unsubscribe();
    }
    subscribed.value = false;
  } catch (e) {
    error.value = String(e);
  } finally {
    loading.value = false;
  }
}

async function checkSubscribed() {
  if (!supported) return;
  try {
    const reg = await navigator.serviceWorker.ready;
    const sub = await reg.pushManager.getSubscription();
    subscribed.value = !!sub;
  } catch {
    subscribed.value = false;
  }
}

async function sendTest() {
  loading.value = true;
  error.value = null;
  try {
    await api.post<void>("/push/test", {});
    console.log("[push] test notification sent");
  } catch (e) {
    console.error("[push] test failed:", e);
    error.value = String(e);
  } finally {
    loading.value = false;
  }
}

export function usePushNotifications() {
  return {
    supported,
    subscribed: readonly(subscribed),
    loading: readonly(loading),
    error: readonly(error),
    subscribe,
    unsubscribe,
    sendTest,
    checkSubscribed,
  };
}
