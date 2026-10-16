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

import catalog from "../src/MarkVibe.Core/Localization/strings.json";

let language = "en";
let nativeLanguage = "";

/** Remember the native host language before browser locale negotiation. */
export function setSystemLanguage(value) {
  nativeLanguage = value || "";
}

/** Resolve supported locales, keeping Cantonese distinct from Mandarin. */
export function resolveLanguage(preference = "system") {
  const candidates = preference === "system"
    ? [nativeLanguage, ...(navigator.languages || []), navigator.language]
    : [preference];
  for (const candidate of candidates.filter(Boolean)) {
    const locale = candidate.toLowerCase();
    if (locale.startsWith("yue") || ["zh-hk", "zh-mo"].includes(locale)) {
      return "yue-HK";
    }
    if (locale.startsWith("zh")) {
      return "zh-CN";
    }

    const neutralLanguage = locale.split("-")[0];
    if (["en", ...catalog.languages].includes(neutralLanguage)) {
      return neutralLanguage;
    }
  }
  return "en";
}

/** Update document language and notify DOM hosts only when it changes. */
export function setLanguage(preference) {
  const previousLanguage = language;
  language = resolveLanguage(preference);
  document.documentElement.lang = language;
  if (previousLanguage !== language) {
    window.dispatchEvent(new CustomEvent("markvibe-language-changed"));
  }
  return language;
}

/** Translate catalog aliases and numbered placeholders, retaining missing keys. */
export function t(key, ...values) {
  const originalKey = key;
  key = catalog.aliases?.[key] || key;
  const languageIndex = catalog.languages.indexOf(language);
  const text = languageIndex >= 0
    ? catalog.strings[key]?.[languageIndex] ?? originalKey
    : originalKey;
  return text.replace(/\{(\d+)\}/gu, (match, position) => position < values.length ? String(values[position]) : match);
}

/** Store translation inputs so an element can be refreshed after language changes. */
export function localize(element, key, attribute = "textContent", ...values) {
  element._mvTranslations ??= {};
  element._mvTranslations[attribute] = { key, values };
  if (attribute === "textContent") {
    element.textContent = t(key, ...values);
  } else {
    element.setAttribute(attribute, t(key, ...values));
  }
  return element;
}

/** Refresh registered text, accessible attributes and icon captions under a host. */
export function refreshTranslations(root) {
  for (const element of [root, ...root.querySelectorAll("*")]) {
    for (const [attribute, translation] of Object.entries(element._mvTranslations || {})) {
      localize(element, translation.key, attribute, ...translation.values);
    }
    if (element._mvCaptionKey !== undefined) {
      element._mvRefreshCaption?.();
    }
  }
}
