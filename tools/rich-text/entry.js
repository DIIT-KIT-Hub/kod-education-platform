/**
 * The parts of Tiptap (https://tiptap.dev, MIT) used by the news editor, bundled into one ES module.
 * Rebuild after changing this file or the versions in package.json: `npm ci && npm run build`.
 */
export { Editor, Node, Extension, mergeAttributes } from "@tiptap/core";
export { StarterKit } from "@tiptap/starter-kit";
export { Image } from "@tiptap/extension-image";
export { TextAlign } from "@tiptap/extension-text-align";
export { TableKit } from "@tiptap/extension-table";
export { Placeholder, CharacterCount } from "@tiptap/extensions";
