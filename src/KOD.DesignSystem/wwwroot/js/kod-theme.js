/**
 * Applies the saved interface preferences before the first paint to avoid a flash of the wrong state:
 * the color theme (saved or system) and the collapsed desktop sidebar.
 * Must be loaded synchronously in <head>. The choices are stored per browser in localStorage.
 */
(function () {
    "use strict";

    /**
     * Reads a stored preference.
     * @param {string} key The storage key.
     * @returns {string|null} The value.
     */
    function read(key) {
        try {
            return window.localStorage.getItem(key);
        } catch (e) {
            return null;
        }
    }

    var saved = read("kod-theme");
    var theme = saved === "dark" || saved === "light"
        ? saved
        : (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");

    document.documentElement.setAttribute("data-theme", theme);
    document.documentElement.setAttribute("data-sidebar", read("kod-sidebar") === "collapsed" ? "collapsed" : "expanded");
})();
