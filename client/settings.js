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
 * Modified with AI: OpenAI Codex; persist a separate validated application UI font choice; 2026-10-16
 * Human guidance: Umberto Giacobbi; choose installed monospace fonts for the UI
 * Modified with AI: OpenAI Codex; persist validated map inspector splitter width; 2026-10-16
 * Modified with AI: OpenAI Codex; retain the explicitly selected AI connection provider; 2026-10-16
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import { readerThemeCatalog } from "./themes.js";
import uiFontFamilies from "../src/MarkVibe.Core/Presentation/ui-fonts.json";
import localizationCatalog from "../src/MarkVibe.Core/Localization/strings.json";

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
  darkCodeTheme: "auto",
  uiFont: "system",
  aiProvider: "api-key"
});

/** Validate a stored theme identifier against the requested category and appearance mode. */
function themeId(category, value, mode, fallback) {
  return readerThemeCatalog[category].some(item => item.id === value && item.modes.includes(mode)) ? value : fallback;
}

/** Canonicalize supported persisted UI families; actual installation is checked by the desktop host. */
function uiFontId(value) {
  if (typeof value !== "string") {
    return defaults.uiFont;
  }
  const requestedFamily = value.toLowerCase();
  return uiFontFamilies.find(family => family.toLowerCase() === requestedFamily) || defaults.uiFont;
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
    language: ["system", "en", ...localizationCatalog.languages].includes(value.language) ? value.language : defaults.language,
    acrylic: value.acrylic !== false,
    lightDocumentTheme: themeId("document", value.lightDocumentTheme, "light", "markvibe"),
    darkDocumentTheme: themeId("document", value.darkDocumentTheme, "dark", "markvibe"),
    lightCodeTheme: themeId("code", value.lightCodeTheme, "light", "auto"),
    darkCodeTheme: themeId("code", value.darkCodeTheme, "dark", "auto"),
    uiFont: uiFontId(value.uiFont),
    aiProvider: value.aiProvider === "codex-cli" ? "codex-cli" : defaults.aiProvider
  };
}

/** Read only the current bounded envelope; malformed data and other versions are not migrated. */
function readStoredValues(storageKey, maximumCharacters) {
  try {
    const serialized = localStorage.getItem(storageKey);
    if (!serialized || serialized.length > maximumCharacters) {
      return null;
    }

    const saved = JSON.parse(serialized);
    return saved?.version === 1 ? saved.values : null;
  } catch {
    return null;
  }
}

/** Write the current envelope and report storage failure without leaking a partial application state. */
function saveStoredValues(storageKey, values) {
  try {
    const serialized = JSON.stringify({ version: 1, values });
    localStorage.setItem(storageKey, serialized);
    return true;
  } catch {
    return false;
  }
}

/** Restore preferences through the current envelope and the supported-value validator. */
export function readPreferences() {
  const savedValues = readStoredValues("markvibe.preferences", 4096);
  return normalizePreferences(savedValues);
}

/** Persist managed validated preferences, reporting storage failure to the settings UI. */
export function savePreferences(value) {
  return saveStoredValues("markvibe.preferences", value);
}

/** Normalize shell geometry without retaining prompts, document paths or arbitrary stored fields. */
function normalizeLayout(value) {
  const layout = value && typeof value === "object" ? value : {};
  return {
    aiOpen: layout.aiOpen === true,
    aiWidth: boundedLayoutNumber(layout.aiWidth, 300, 600, 360),
    sidebarOpen: layout.sidebarOpen !== false,
    sidebarWidth: boundedLayoutNumber(layout.sidebarWidth, 180, 520, 220),
    sourceRatio: boundedLayoutNumber(layout.sourceRatio, 0.2, 0.8, 0.5),
    mapInspectorWidth: boundedLayoutNumber(layout.mapInspectorWidth, 220, 600, 290)
  };
}

/** Keep persisted geometry finite and inside the managed shell's supported bounds. */
function boundedLayoutNumber(value, minimum, maximum, fallback) {
  return Number.isFinite(value) ? Math.max(minimum, Math.min(maximum, value)) : fallback;
}

/** Restore the independent shell layout, defaulting safely when storage is absent or malformed. */
export function readLayout() {
  const savedValues = readStoredValues("markvibe.layout", 1024);
  return normalizeLayout(savedValues);
}

/** Persist layout alone so resizing panes does not refresh themes or document renderers. */
export function saveLayout(value) {
  return saveStoredValues("markvibe.layout", normalizeLayout(value));
}
