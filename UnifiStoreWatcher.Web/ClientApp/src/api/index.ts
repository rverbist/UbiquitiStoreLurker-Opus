import type {
  Product,
  PagedResult,
  StockCheck,
  AppSettingsDto,
  NotificationConfigDto,
} from "@/types";

const BASE = "/api";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...init,
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`HTTP ${res.status}: ${text}`);
  }
  if (res.status === 204 || res.status === 201) return undefined as unknown as T;
  return res.json() as Promise<T>;
}

function post<T>(path: string, body: unknown): Promise<T> {
  return request<T>(path, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

function del<T>(path: string, body?: unknown): Promise<T> {
  return request<T>(path, {
    method: "DELETE",
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
}

export const api = {
  post,
  del,
  products: {
    list: () => request<Product[]>("/products"),
    get: (id: number) => request<Product>(`/products/${id}`),
    create: (url: string, subscribedEvents = 1) =>
      request<Product>("/products", {
        method: "POST",
        body: JSON.stringify({ url, subscribedEvents }),
      }),
    update: (
      id: number,
      patch: { isActive?: boolean; subscribedEvents?: number },
    ) =>
      request<Product>(`/products/${id}`, {
        method: "PUT",
        body: JSON.stringify(patch),
      }),
    remove: (id: number) =>
      request<void>(`/products/${id}`, { method: "DELETE" }),
    history: (id: number, page = 1, pageSize = 20) =>
      request<PagedResult<StockCheck>>(
        `/products/${id}/history?page=${page}&pageSize=${pageSize}`,
      ),
  },
  settings: {
    get: () => request<AppSettingsDto>("/settings"),
    update: (patch: Partial<AppSettingsDto>) =>
      request<AppSettingsDto>("/settings", {
        method: "PUT",
        body: JSON.stringify(patch),
      }),
  },
  notifications: {
    getConfigs: () =>
      request<NotificationConfigDto[]>("/notifications/configs"),
    updateConfig: (
      id: number,
      patch: { isEnabled?: boolean; settingsJson?: string },
    ) =>
      request<NotificationConfigDto>(`/notifications/configs/${id}`, {
        method: "PUT",
        body: JSON.stringify(patch),
      }),
  },
};
