import type { PointerEvent } from "react";

/**
 * Moves the origin of a control's hover fill ("ink", the --_x / --_y custom properties of its ::before layer) to the
 * pointer, so the fill grows from where the pointer entered and shrinks towards where it left.
 * @param event The pointer event of the control.
 */
export function trackInk(event: PointerEvent<HTMLElement>): void {
    const element = event.currentTarget;
    const rect = element.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) {
        return;
    }

    element.style.setProperty("--_x", `${(((event.clientX - rect.left) / rect.width) * 100).toFixed(1)}%`);
    element.style.setProperty("--_y", `${(((event.clientY - rect.top) / rect.height) * 100).toFixed(1)}%`);
}

/**
 * The pointer handlers that drive the ink of a control.
 */
export const inkHandlers = { onPointerEnter: trackInk, onPointerLeave: trackInk } as const;
