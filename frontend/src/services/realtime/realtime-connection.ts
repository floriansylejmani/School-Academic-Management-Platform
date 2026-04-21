"use client";

import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel
} from "@microsoft/signalr";
import { HUBS_BASE_URL } from "@/services/apiConfig";
import type { RealtimeConnectionName } from "@/store/realtime.store";
import { useRealtimeStore } from "@/store/realtime.store";
import { useAuthStore } from "@/store/auth.store";

interface RealtimeConnectionOptions {
  name: RealtimeConnectionName;
  hubPath: string;
}

export class RealtimeConnectionService {
  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private readonly reconnectedListeners = new Set<() => void>();

  constructor(private readonly options: RealtimeConnectionOptions) {}

  subscribe<TPayload>(eventName: string, handler: (payload: TPayload) => void) {
    const connection = this.ensureConnection();
    connection.on(eventName, handler as (...args: unknown[]) => void);

    return () => {
      connection.off(eventName, handler as (...args: unknown[]) => void);
    };
  }

  onReconnected(handler: () => void) {
    this.reconnectedListeners.add(handler);

    return () => {
      this.reconnectedListeners.delete(handler);
    };
  }

  async start() {
    // Guard: Only start if user is authenticated
    const authStore = useAuthStore.getState();
    if (!authStore.isAuthenticated) {
      useRealtimeStore.getState().setConnectionStatus(this.options.name, "disconnected");
      throw new Error("Cannot start realtime connection: user is not authenticated");
    }

    const connection = this.ensureConnection();
    if (connection.state === HubConnectionState.Connected) {
      useRealtimeStore.getState().setConnectionStatus(this.options.name, "connected");
      return;
    }

    if (this.startPromise) {
      await this.startPromise;
      return;
    }

    useRealtimeStore.getState().setConnectionStatus(this.options.name, "connecting");

    this.startPromise = connection.start()
      .then(() => {
        useRealtimeStore.getState().setConnectionStatus(this.options.name, "connected");
      })
      .catch((error) => {
        useRealtimeStore.getState().setConnectionStatus(this.options.name, "disconnected");
        throw error;
      })
      .finally(() => {
        this.startPromise = null;
      });

    await this.startPromise;
  }

  async stop() {
    if (!this.connection) {
      useRealtimeStore.getState().setConnectionStatus(this.options.name, "disconnected");
      return;
    }

    if (this.startPromise) {
      try {
        await this.startPromise;
      } catch {
        // Ignore connection-start errors when stopping.
      }
    }

    if (this.connection.state !== HubConnectionState.Disconnected) {
      await this.connection.stop();
    }

    // Clear reconnected listeners to prevent accumulation on reuse
    this.reconnectedListeners.clear();

    // Clear connection reference to allow fresh connection on next start()
    this.connection = null;

    useRealtimeStore.getState().setConnectionStatus(this.options.name, "disconnected");
  }

  private ensureConnection() {
    if (this.connection) {
      return this.connection;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${HUBS_BASE_URL}${this.options.hubPath}`, {
        withCredentials: true,
        transport:
          HttpTransportType.WebSockets |
          HttpTransportType.ServerSentEvents |
          HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build();

    connection.onreconnecting(() => {
      useRealtimeStore.getState().setConnectionStatus(this.options.name, "reconnecting");
    });

    connection.onreconnected(() => {
      useRealtimeStore.getState().setConnectionStatus(this.options.name, "connected");
      for (const listener of this.reconnectedListeners) {
        listener();
      }
    });

    connection.onclose(() => {
      useRealtimeStore.getState().setConnectionStatus(this.options.name, "disconnected");
    });

    this.connection = connection;
    return connection;
  }
}
