export enum StockState {
  Unknown = 0,
  InStock = 1,
  OutOfStock = 2,
  Indeterminate = 3,
}

export enum SubscriptionType {
  None = 0,
  InStock = 1,
  OutOfStock = 2,
  Both = 3,
}

export interface Product {
  id: number;
  url: string;
  productCode: string | null;
  name: string | null;
  description: string | null;
  imageUrl: string | null;
  currentState: StockState;
  isActive: boolean;
  subscribedEvents: SubscriptionType;
  nextPollDueAtUtc: string;
  lastPollAtUtc: string | null;
  lastStateChangeAtUtc: string | null;
  pollCount: number;
  errorCount: number;
}

export interface StockCheck {
  id: number;
  requestUrl: string;
  httpStatusCode: number | null;
  detectedState: StockState;
  parserStrategy: string | null;
  parserConfidence: number | null;
  durationMs: number;
  errorMessage: string | null;
  createdAtUtc: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AppSettingsDto {
  nickname: string;
  email: string | null;
  phone: string | null;
  pollIntervalMinSeconds: number;
  pollIntervalMaxSeconds: number;
  maxRetryAttempts: number;
  retryBaseDelaySeconds: number;
  minDelayBetweenRequestsSeconds: number;
  vapidPublicKey: string | null;
}

export interface NotificationConfigDto {
  id: number;
  providerType: string;
  displayName: string;
  isEnabled: boolean;
  settingsJson: string | null;
}

// SignalR event payloads
export interface StockStatusChangedEvent {
  productId: number;
  url: string;
  productName: string | null;
  fromState: StockState;
  toState: StockState;
  detectedAtUtc: string;
}

export interface PollCycleCompletedEvent {
  productId: number;
  url: string;
  success: boolean;
  httpStatusCode: number;
  durationMs: number;
  completedAtUtc: string;
}
