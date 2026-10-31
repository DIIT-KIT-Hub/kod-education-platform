/**
 * Applies the saved interface preferences before the first paint, so the page never flashes in the wrong theme:
 * the colour theme (saved or the system one) and the collapsed desktop sidebar. Loaded synchronously from <head>
 * (the Content-Security-Policy allows no inline scripts). The choices are stored per browser in localStorage.
 */
(function () {
    "use strict";

    /**
     * Reads a stored preference.
     * @param {string} key The storage key.
     * @returns {string | null} The value.
     */
    function read(key) {
        try {
            return window.localStorage.getItem(key);
        } catch {
            return null;
        }
    }

    var saved = read("kod-theme");
    var theme =
        saved === "dark" || saved === "light" ? saved : window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";

    document.documentElement.setAttribute("data-theme", theme);
    document.documentElement.setAttribute("data-sidebar", read("kod-sidebar") === "collapsed" ? "collapsed" : "expanded");
})();
