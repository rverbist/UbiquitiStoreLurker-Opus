self.addEventListener("push", (event) => {
  console.log("[sw] push received, has data:", !!event.data);
  let data = {};
  try {
    data = event.data?.json() ?? {};
  } catch (e) {
    console.error("[sw] push data parse failed:", e);
  }
  const title = data.title ?? "Stock Update";
  const options = {
    body: data.body ?? "",
    icon: data.icon ?? "/favicon.ico",
    badge: data.badge ?? "/favicon.ico",
    data: { url: data.url ?? "/" },
  };
  if (data.image)             options.image             = data.image;
  if (data.tag)               options.tag               = data.tag;
  if (data.renotify != null)  options.renotify          = data.renotify;
  if (data.timestamp != null) options.timestamp         = data.timestamp;
  if (data.requireInteraction != null) options.requireInteraction = data.requireInteraction;
  if (data.vibrate)           options.vibrate           = data.vibrate;
  event.waitUntil(
    self.registration
      .showNotification(title, options)
      .then(() => {
        console.log("[sw] notification shown:", title);
      })
      .catch((e) => {
        console.error("[sw] showNotification failed:", e);
        // Fallback: minimal notification without options that could trip up older Chrome
        return self.registration.showNotification(title, {
          body: options.body,
        });
      }),
  );
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  const url = event.notification.data?.url;
  if (url) {
    event.waitUntil(
      clients
        .matchAll({ type: "window", includeUncontrolled: true })
        .then((windowClients) => {
          for (const client of windowClients) {
            if (client.url === url && "focus" in client) return client.focus();
          }
          if (clients.openWindow) return clients.openWindow(url);
        }),
    );
  }
});
