import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection, type IRetryPolicy } from "@microsoft/signalr";
import { useEffect, useEffectEvent } from "react";

/**
 * The address of the server hub (LiveHub): under the API, signed-in users only.
 */
const hubUrl = "/api/v1/live";

/**
 * The pauses between reconnect attempts (the last one repeats): a chat must come back after any outage.
 */
const retryDelays = [0, 1000, 3000, 5000, 10_000, 20_000, 30_000];

/**
 * A connection that drops within this time after coming up counts as a failed attempt.
 */
const shortLife = 10_000;

/**
 * When the connection last came up.
 */
let upSince = 0;

/**
 * How many times in a row the connection dropped soon after coming up (the pauses grow with it, so a server that
 * closes every new connection is not hammered).
 */
let shortLived = 0;

/**
 * Notes that the connection dropped.
 */
function noteDrop(): void {
    shortLived = Date.now() - upSince < shortLife ? shortLived + 1 : 0;
}

/**
 * The pause before the next attempt.
 * @param attempts The failed attempts so far.
 * @returns The pause in milliseconds.
 */
function pause(attempts: number): number {
    return retryDelays[Math.min(attempts + shortLived, retryDelays.length - 1)] ?? 30_000;
}

/**
 * Reconnects forever with growing pauses.
 */
const retryPolicy: IRetryPolicy = {
    nextRetryDelayInMilliseconds: ({ previousRetryCount }) => pause(previousRetryCount),
};

/**
 * The one SignalR connection of the application; created on first use and shared by every page that listens.
 */
let connection: HubConnection | null = null;

/**
 * The number of components that listen; the connection stays up while there are any.
 */
let listenerCount = 0;

/**
 * The pending restart after the server closed the connection.
 */
let restartTimer: ReturnType<typeof setTimeout> | undefined;

/**
 * The number of restarts in a row that failed (for the pause before the next one).
 */
let failedStarts = 0;

/**
 * The groups the pages listen to, with the number of listeners; joined again after a reconnect.
 */
const groups = new Map<string, { readonly join: (hub: HubConnection) => Promise<unknown>; count: number }>();

/**
 * Called after the connection came back: the pages load their data again (messages may have been missed meanwhile).
 */
const reconnectListeners = new Set<() => void>();

/**
 * Joins the groups again and tells the pages to catch up.
 * @param hub The connection that came back.
 */
function resume(hub: HubConnection): void {
    upSince = Date.now();
    groups.forEach((group) => void group.join(hub).catch(() => undefined));
    reconnectListeners.forEach((listener) => listener());
}

/**
 * Starts the connection again after the server closed it: it does so when the access token of the connection expires
 * (the next start authenticates with a fresh one) and when it shuts down.
 */
function scheduleRestart(): void {
    clearTimeout(restartTimer);
    const delay = pause(failedStarts);
    restartTimer = setTimeout(() => {
        const hub = connection;
        if (!hub || listenerCount === 0 || hub.state !== HubConnectionState.Disconnected) {
            return;
        }

        hub.start()
            .then(() => {
                failedStarts = 0;
                resume(hub);
            })
            .catch(() => {
                failedStarts++;
                scheduleRestart();
            });
    }, delay);
}

/**
 * Starts the connection once; reconnects on its own after network failures and restarts after the server closed it.
 * @returns The started connection.
 */
async function connect(): Promise<HubConnection> {
    if (!connection) {
        const hub = new HubConnectionBuilder()
            .withUrl(hubUrl, { withCredentials: true })
            .withAutomaticReconnect(retryPolicy)
            .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
            .build();
        hub.onreconnecting(noteDrop);
        hub.onreconnected(() => resume(hub));
        hub.onclose(() => {
            noteDrop();
            if (connection === hub && listenerCount > 0) {
                scheduleRestart();
            }
        });
        connection = hub;
    }

    if (connection.state === HubConnectionState.Disconnected) {
        await connection.start();
        upSince = Date.now();
    }

    return connection;
}

/**
 * Keeps the connection up while a component is shown and listens to a hub message.
 * @param message The hub message.
 * @param handler Called with the message arguments.
 * @param onReconnected Called after the connection came back (catch up on what was missed).
 */
export function useLiveEvent(message: string, handler: (...args: never[]) => void, onReconnected?: () => void): void {
    const onMessage = useEffectEvent((...args: unknown[]) => (handler as (...values: unknown[]) => void)(...args));
    const onBack = useEffectEvent(() => onReconnected?.());

    useEffect(() => {
        let active = true;
        const listener = (...args: unknown[]) => onMessage(...args);
        const reconnected = () => onBack();
        listenerCount++;
        reconnectListeners.add(reconnected);
        void connect()
            .then((hub) => {
                if (active) {
                    hub.on(message, listener);
                }
            })
            .catch(() => {
                // The page works without live updates; a restart is attempted while someone listens.
                scheduleRestart();
            });

        return () => {
            active = false;
            listenerCount--;
            reconnectListeners.delete(reconnected);
            connection?.off(message, listener);
        };
    }, [message]);
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
        listenerCount++;
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
                scheduleRestart();
            });

        return () => {
            active = false;
            listenerCount--;
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
 * Calls a hub method when the connection is up (signals that may be lost, like "typing").
 * @param method The hub method.
 * @param args The arguments.
 */
export function sendLive(method: string, ...args: unknown[]): void {
    if (connection?.state === HubConnectionState.Connected) {
        void connection.send(method, ...args).catch(() => undefined);
    }
}

/**
 * Ends the connection (sign-out): the server must not keep pushing to a signed-out page.
 * @returns Resolves when stopped.
 */
export async function disconnectLive(): Promise<void> {
    const hub = connection;
    connection = null;
    clearTimeout(restartTimer);
    groups.clear();
    await hub?.stop();
}
