/**
 * Square crop preview of the profile photo form (ES module, mounted by app.js on [data-photo-cropper]).
 * The chosen photo is shown in a round frame; it can be dragged (pointer or arrow keys) and zoomed (slider or wheel).
 * The visible square is written to the hidden crop fields as fractions of the upright image, so the server cuts the
 * same part from the original file. Nothing is uploaded until the form is submitted.
 */

import { shrinkImage } from "./image-shrink.js";

/**
 * Mounts the cropper on a photo form.
 * @param {HTMLFormElement} form The form with [data-photo-input], [data-photo-stage], [data-photo-frame],
 *   [data-photo-image], [data-photo-zoom] and the [data-crop] hidden fields.
 */
export function mountPhotoCropper(form) {
    const input = /** @type {HTMLInputElement} */ (form.querySelector("[data-photo-input]"));
    const stage = /** @type {HTMLElement} */ (form.querySelector("[data-photo-stage]"));
    const frame = /** @type {HTMLElement} */ (form.querySelector("[data-photo-frame]"));
    const image = /** @type {HTMLImageElement} */ (form.querySelector("[data-photo-image]"));
    const zoom = /** @type {HTMLInputElement} */ (form.querySelector("[data-photo-zoom]"));
    const fields = Object.fromEntries(Array.from(form.querySelectorAll("[data-crop]"))
        .map((field) => [field.getAttribute("data-crop"), /** @type {HTMLInputElement} */ (field)]));

    /** The image position: offset of its top-left corner in the frame and its scale. */
    const state = { x: 0, y: 0, scale: 1, base: 1, width: 0, height: 0, size: 0 };
    /** @type {string|null} */
    let objectUrl = null;

    frame.tabIndex = 0;
    frame.setAttribute("role", "img");

    /**
     * Keeps the frame covered by the image and draws it.
     */
    function render() {
        const width = state.width * state.scale;
        const height = state.height * state.scale;
        state.x = Math.min(0, Math.max(state.size - width, state.x));
        state.y = Math.min(0, Math.max(state.size - height, state.y));
        image.style.width = `${state.width}px`;
        image.style.height = `${state.height}px`;
        image.style.transform = `translate(${state.x}px, ${state.y}px) scale(${state.scale})`;

        fields.left.value = String(-state.x / width);
        fields.top.value = String(-state.y / height);
        fields.width.value = String(state.size / width);
        fields.height.value = String(state.size / height);
    }

    /**
     * Zooms around a point of the frame.
     * @param {number} factor The new zoom (1–4) relative to the size that just covers the frame.
     * @param {number} [cx] The x of the fixed point in the frame.
     * @param {number} [cy] The y of the fixed point in the frame.
     */
    function zoomTo(factor, cx = state.size / 2, cy = state.size / 2) {
        const scale = state.base * Math.min(4, Math.max(1, factor));
        state.x = cx - ((cx - state.x) * scale) / state.scale;
        state.y = cy - ((cy - state.y) * scale) / state.scale;
        state.scale = scale;
        zoom.value = String(scale / state.base);
        render();
    }

    /**
     * Clears the preview and the crop (the server then uses the centre).
     */
    function reset() {
        stage.hidden = true;
        Object.values(fields).forEach((field) => { field.value = ""; });
        if (objectUrl) {
            URL.revokeObjectURL(objectUrl);
            objectUrl = null;
        }
    }

    input.addEventListener("change", async () => {
        reset();
        const chosen = input.files && input.files[0];
        if (!chosen || !chosen.type.startsWith("image/")) {
            return;
        }

        // A smaller copy uploads faster; the crop is relative, so it still applies (setting files fires no change event).
        const file = await shrinkImage(chosen, 1600);
        if (file !== chosen && typeof DataTransfer === "function") {
            const transfer = new DataTransfer();
            transfer.items.add(file);
            input.files = transfer.files;
        }

        objectUrl = URL.createObjectURL(file);
        image.onload = () => {
            stage.hidden = false;
            state.size = frame.clientWidth;
            state.width = image.naturalWidth;
            state.height = image.naturalHeight;
            state.base = Math.max(state.size / state.width, state.size / state.height);
            state.scale = state.base;
            state.x = (state.size - state.width * state.scale) / 2;
            state.y = (state.size - state.height * state.scale) / 2;
            zoom.value = "1";
            render();
        };
        // A format the browser cannot preview (the server still checks the file): no crop, the centre is used.
        image.onerror = reset;
        image.src = objectUrl;
    });

    zoom.addEventListener("input", () => zoomTo(Number(zoom.value)));

    frame.addEventListener("wheel", (event) => {
        event.preventDefault();
        const box = frame.getBoundingClientRect();
        zoomTo(state.scale / state.base * (event.deltaY < 0 ? 1.1 : 1 / 1.1), event.clientX - box.left, event.clientY - box.top);
    }, { passive: false });

    frame.addEventListener("pointerdown", (event) => {
        frame.setPointerCapture(event.pointerId);
        const start = { x: event.clientX - state.x, y: event.clientY - state.y };
        const move = (/** @type {PointerEvent} */ next) => {
            state.x = next.clientX - start.x;
            state.y = next.clientY - start.y;
            render();
        };
        const stop = () => {
            frame.removeEventListener("pointermove", move);
            frame.removeEventListener("pointerup", stop);
            frame.removeEventListener("pointercancel", stop);
        };
        frame.addEventListener("pointermove", move);
        frame.addEventListener("pointerup", stop);
        frame.addEventListener("pointercancel", stop);
    });

    frame.addEventListener("keydown", (event) => {
        const steps = { ArrowLeft: [8, 0], ArrowRight: [-8, 0], ArrowUp: [0, 8], ArrowDown: [0, -8] };
        const step = steps[/** @type {keyof typeof steps} */ (event.key)];
        if (step) {
            event.preventDefault();
            state.x += step[0];
            state.y += step[1];
            render();
        } else if (event.key === "+" || event.key === "-") {
            event.preventDefault();
            zoomTo(state.scale / state.base + (event.key === "+" ? 0.1 : -0.1));
        }
    });
}
