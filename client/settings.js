// C# owns Preferences. This adapter validates persisted browser data at the boundary.
const defaults = Object.freeze({ appearance: "system", documentStyle: "technical", zoom: 100,
  simpleSource: false, rememberLastDocument: true });

export function normalizePreferences(value) {
  // value: an untrusted persisted object; unknown fields are intentionally ignored.
  if (!value || typeof value !== "object") return { ...defaults };
  return {
    appearance: ["system", "light", "dark"].includes(value.appearance) ? value.appearance : defaults.appearance,
    documentStyle: ["technical", "editorial", "compact"].includes(value.documentStyle) ? value.documentStyle : defaults.documentStyle,
    zoom: Number.isInteger(value.zoom) ? Math.max(80, Math.min(150, value.zoom)) : defaults.zoom,
    simpleSource: value.simpleSource === true,
    rememberLastDocument: value.rememberLastDocument !== false
  };
}

export function readPreferences() {
  // Invalid JSON, future versions and unavailable storage fall back to session defaults.
  try {
    const raw = localStorage.getItem("markvibe.preferences");
    if (!raw || raw.length > 4096) return { ...defaults };
    const saved = JSON.parse(raw);
    return normalizePreferences(saved?.version === 1 ? saved.values : saved?.version === undefined ? saved : null);
  } catch { return { ...defaults }; }
}

export function savePreferences(value) {
  // value: validated C# preferences. False reports browser storage failure to the UI.
  try { localStorage.setItem("markvibe.preferences", JSON.stringify({ version: 1, values: value })); return true; }
  catch { return false; }
}
