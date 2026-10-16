/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Modified with AI: OpenAI Codex; readability revision; 2026-10-11
 * Human guidance: Umberto Giacobbi; repository readability rules and MIT licensing
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import { readerThemeCatalog } from "./themes.js";

// C# owns Preferences. This adapter validates persisted browser data at the boundary.
const defaults = Object.freeze({
  appearance: "system",
  documentStyle: "technical",
  zoom: 100,
  simpleSource: false,
  rememberLastDocument: true,
  language: "system",
  acrylic: true,
  lightDocumentTheme: "markvibe",
  darkDocumentTheme: "markvibe",
  lightCodeTheme: "auto",
  darkCodeTheme: "auto"
});

/** Validate a stored theme identifier against the requested category and appearance mode. */
function themeId(category, value, mode, fallback) {
  return readerThemeCatalog[category].some(item => item.id === value && item.modes.includes(mode)) ? value : fallback;
}

/** Copy only supported persisted preferences and restore defaults for invalid values. */
export function normalizePreferences(value) {
  // value: an untrusted persisted object; unknown fields are intentionally ignored.
  if (!value || typeof value !== "object") {
    return { ...defaults };
  }
  return {
    appearance: ["system", "light", "dark"].includes(value.appearance) ? value.appearance : defaults.appearance,
    documentStyle: ["technical", "editorial", "compact"].includes(value.documentStyle) ? value.documentStyle : defaults.documentStyle,
    zoom: Number.isInteger(value.zoom) ? Math.max(80, Math.min(150, value.zoom)) : defaults.zoom,
    simpleSource: value.simpleSource === true,
    rememberLastDocument: value.rememberLastDocument !== false,
    language: ["system", "en", "it", "fr", "vi", "zh-CN", "yue-HK", "id", "ko", "ja"].includes(value.language) ? value.language : defaults.language,
    acrylic: value.acrylic !== false,
    lightDocumentTheme: themeId("document", value.lightDocumentTheme, "light", "markvibe"),
    darkDocumentTheme: themeId("document", value.darkDocumentTheme, "dark", "markvibe"),
    lightCodeTheme: themeId("code", value.lightCodeTheme, "light", "auto"),
    darkCodeTheme: themeId("code", value.darkCodeTheme, "dark", "auto")
  };
}

/** Read bounded persisted settings while tolerating malformed data, future versions and unavailable storage. */
export function readPreferences() {
  // Invalid JSON, future versions and unavailable storage fall back to session defaults.
  try {
    const raw = localStorage.getItem("markvibe.preferences");
    if (!raw || raw.length > 4096) {
      return { ...defaults };
    }
    const saved = JSON.parse(raw);
    return normalizePreferences(saved?.version === 1 ? saved.values : saved?.version === undefined ? saved : null);
  } catch {
    return { ...defaults };
  }
}

/** Persist the validated preference envelope and report whether browser storage accepted it. */
export function savePreferences(value) {
  // value: validated C# preferences. False reports browser storage failure to the UI.
  try {
    localStorage.setItem("markvibe.preferences", JSON.stringify({ version: 1, values: value }));
    return true;
  }
  catch {
    return false;
  }
}
