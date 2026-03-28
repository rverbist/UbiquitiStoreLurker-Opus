import { onMounted, onUnmounted } from "vue";
import * as signalR from "@microsoft/signalr";
import { useProductStore } from "@/stores/products";
import type { StockStatusChangedEvent, PollCycleCompletedEvent } from "@/types";

export function useSignalR(
  onStockStatusChanged?: (evt: StockStatusChangedEvent) => void,
) {
  const store = useProductStore();
  let connection: signalR.HubConnection | null = null;

  async function start() {
    connection = new signalR.HubConnectionBuilder()
      .withUrl("/UniFiStoreWatcher-hub")
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on("StockStatusChanged", (evt: StockStatusChangedEvent) => {
      store.applyStockStatusChanged(evt);
      onStockStatusChanged?.(evt);
    });

    connection.on("PollCycleCompleted", (evt: PollCycleCompletedEvent) => {
      store.applyPollCycleCompleted(evt);
    });

    connection.on("PollStarted", (evt: { productId: number }) => {
      store.markPolling(evt.productId);
    });

    try {
      await connection.start();
    } catch {
      // Silent — app works without real-time updates
    }
  }

  async function stop() {
    if (connection) {
      await connection.stop();
      connection = null;
    }
  }

  onMounted(start);
  onUnmounted(stop);

  return { connection };
}
