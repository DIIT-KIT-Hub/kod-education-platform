/**
 * Applies the saved (or system) color theme before the first paint to avoid a flash of the wrong theme.
 * Must be loaded synchronously in <head>. The choice is stored per browser in localStorage.
 */
(function () {
    "use strict";

    /** @type {string|null} */
    var saved = null;
    try {
        saved = window.localStorage.getItem("kod-theme");
    } catch (e) {
        saved = null;
    }

    var theme = saved === "dark" || saved === "light"
        ? saved
        : (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");

    document.documentElement.setAttribute("data-theme", theme);
})();
