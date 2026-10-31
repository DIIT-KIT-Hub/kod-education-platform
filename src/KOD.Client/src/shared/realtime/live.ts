import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from "@microsoft/signalr";
import { useEffect, useEffectEvent } from "react";

/**
 * The address of the server hub (LiveHub): under the API, signed-in users only.
 */
const hubUrl = "/api/v1/live";

/**
 * The one SignalR connection of the application; created on first use and shared by every page that listens.
 */
let connection: HubConnection | null = null;

/**
 * The groups the pages listen to, with the number of listeners; joined again after a reconnect.
 */
const groups = new Map<string, { readonly join: (hub: HubConnection) => Promise<unknown>; count: number }>();

/**
 * Called after the connection came back: the pages load their data again (signals may have been missed meanwhile).
 */
const reconnectListeners = new Set<() => void>();

/**
 * Starts the connection once; automatic reconnects with the default back-off.
 * @returns The started connection.
 */
async function connect(): Promise<HubConnection> {
    if (!connection) {
        connection = new HubConnectionBuilder()
            .withUrl(hubUrl, { withCredentials: true })
            .withAutomaticReconnect()
            .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
            .build();
        connection.onreconnected(() => {
            const hub = connection;
            if (hub) {
                groups.forEach((group) => void group.join(hub).catch(() => undefined));
            }

            reconnectListeners.forEach((listener) => listener());
        });
    }

    if (connection.state === HubConnectionState.Disconnected) {
        await connection.start();
    }

    return connection;
}

/**
 * Listens to a hub message while a component is shown, inside a group the component joins and leaves.
 * @param group A unique key of the group (e.g. `news:<id>`).
 * @param join Joins the group on the server (a hub method).
 * @param leave Leaves the group on the server.
 * @param message The hub message to listen to.
 * @param handler Called with the message arguments; also called (without arguments) after a reconnect.
 */
export function useLiveGroup(
    group: string,
    join: (hub: HubConnection) => Promise<unknown>,
    leave: (hub: HubConnection) => Promise<unknown>,
    message: string,
    handler: (...args: unknown[]) => void,
): void {
    const onMessage = useEffectEvent((...args: unknown[]) => handler(...args));
    const onJoin = useEffectEvent((hub: HubConnection) => join(hub));
    const onLeave = useEffectEvent((hub: HubConnection) => leave(hub));

    useEffect(() => {
        let active = true;
        const listener = (...args: unknown[]) => onMessage(...args);
        const reconnected = () => onMessage();
        const entry = groups.get(group) ?? { join: (hub: HubConnection) => onJoin(hub), count: 0 };
        entry.count++;
        groups.set(group, entry);
        reconnectListeners.add(reconnected);

        void connect()
            .then(async (hub) => {
                if (!active) {
                    return;
                }

                hub.on(message, listener);
                if (entry.count === 1) {
                    await onJoin(hub);
                }
            })
            .catch(() => {
                // The page still works without live updates; automatic reconnects take over when the server is back.
            });

        return () => {
            active = false;
            reconnectListeners.delete(reconnected);
            connection?.off(message, listener);
            entry.count--;
            if (entry.count === 0) {
                groups.delete(group);
                if (connection?.state === HubConnectionState.Connected) {
                    void onLeave(connection).catch(() => undefined);
                }
            }
        };
    }, [group, message]);
}

/**
 * Ends the connection (sign-out): the server must not keep pushing to a signed-out page.
 * @returns Resolves when stopped.
 */
export async function disconnectLive(): Promise<void> {
    const hub = connection;
    connection = null;
    groups.clear();
    await hub?.stop();
}
