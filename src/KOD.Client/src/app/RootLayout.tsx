import { Outlet, ScrollRestoration } from "react-router";
import global from "@/shared/styles/global.module.css";
import { ConfirmHost } from "@/shared/ui/ConfirmHost";
import { ToastRegion } from "@/shared/ui/Toast";

/**
 * The root of every page: the global styles, notifications, the confirmation dialog and scroll restoration.
 * @returns The layout.
 */
export function RootLayout() {
    return (
        <div className={global.root}>
            <Outlet />
            <ToastRegion />
            <ConfirmHost />
            <ScrollRestoration />
        </div>
    );
}
