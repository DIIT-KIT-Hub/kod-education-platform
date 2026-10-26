/**
 * Full-size image viewer of news articles (ES module, mounted by app.js on .prose[data-article-body]).
 * Clicking an image of the text or a gallery opens a modal <dialog> with the main rendition (gallery tiles show
 * thumbnails and keep the full image in data-full); arrows, swipes and the buttons go through all images of the article.
 */

/**
 * Translates an interface text.
 * @param {string} text The Ukrainian source text.
 * @returns {string} The text in the interface language.
 */
function t(text) {
    return window.kodDs ? window.kodDs.t(text) : text;
}

/**
 * Creates an icon from the design system sprite.
 * @param {string} name The icon name.
 * @returns {SVGElement} The icon.
 */
function icon(name) {
    const ns = "http://www.w3.org/2000/svg";
    const svg = document.createElementNS(ns, "svg");
    svg.setAttribute("class", "ds-icon");
    svg.setAttribute("aria-hidden", "true");
    const use = document.createElementNS(ns, "use");
    use.setAttribute("href", `/icons/sprite.svg#${name}`);
    svg.append(use);
    return svg;
}

/**
 * Creates a round icon button of the viewer.
 * @param {string} name The icon name.
 * @param {string} label The accessible name.
 * @param {string} className The modifier class.
 * @returns {HTMLButtonElement} The button.
 */
function button(name, label, className) {
    const element = document.createElement("button");
    element.type = "button";
    element.className = `lightbox__button ${className}`;
    element.setAttribute("aria-label", label);
    element.title = label;
    element.append(icon(name));
    return element;
}

/**
 * Makes the images of an article open in the viewer.
 * @param {HTMLElement} article The article body.
 */
export function mountLightbox(article) {
    const images = () => Array.from(article.querySelectorAll("img"));
    /** @type {HTMLDialogElement|null} */
    let dialog = null;
    let index = 0;

    /**
     * Builds the dialog once.
     * @returns {HTMLDialogElement} The dialog.
     */
    function build() {
        const element = document.createElement("dialog");
        element.className = "lightbox";
        element.setAttribute("aria-label", t("Перегляд фото"));

        const picture = document.createElement("img");
        picture.className = "lightbox__image";
        picture.alt = "";
        const counter = document.createElement("span");
        counter.className = "lightbox__counter";
        const close = button("x", t("Закрити"), "lightbox__close");
        const previous = button("chevron-left", t("Попереднє фото"), "lightbox__previous");
        const next = button("chevron-right", t("Наступне фото"), "lightbox__next");

        close.addEventListener("click", () => element.close());
        previous.addEventListener("click", () => show(index - 1));
        next.addEventListener("click", () => show(index + 1));
        element.addEventListener("click", (event) => {
            if (event.target === element) {
                element.close();
            }
        });
        element.addEventListener("keydown", (event) => {
            if (event.key === "ArrowLeft") {
                show(index - 1);
            } else if (event.key === "ArrowRight") {
                show(index + 1);
            }
        });

        let startX = 0;
        picture.addEventListener("pointerdown", (event) => { startX = event.clientX; });
        picture.addEventListener("pointerup", (event) => {
            const distance = event.clientX - startX;
            if (Math.abs(distance) > 50) {
                show(index + (distance < 0 ? 1 : -1));
            }
        });

        element.append(picture, counter, close, previous, next);
        document.body.append(element);
        return element;
    }

    /**
     * Shows an image by its position (wraps around).
     * @param {number} position The position among the images of the article.
     */
    function show(position) {
        const all = images();
        if (!dialog || all.length === 0) {
            return;
        }

        index = (position + all.length) % all.length;
        const source = all[index];
        const picture = /** @type {HTMLImageElement} */ (dialog.querySelector(".lightbox__image"));
        picture.src = source.getAttribute("data-full") || source.currentSrc || source.src;
        picture.alt = source.alt;
        /** @type {HTMLElement} */ (dialog.querySelector(".lightbox__counter")).textContent = all.length > 1 ? `${index + 1} / ${all.length}` : "";
        dialog.toggleAttribute("data-single", all.length < 2);
    }

    article.addEventListener("click", (event) => {
        const image = event.target instanceof HTMLImageElement ? event.target : null;
        if (!image || image.closest("a")) {
            return;
        }

        if (!dialog || !dialog.isConnected) {
            dialog = build();
        }

        show(images().indexOf(image));
        dialog.showModal();
    });
}
