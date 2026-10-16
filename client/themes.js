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

import { catalog, previewStyles, codeStyles } from "./theme-assets.js";
import themeNotices from "../THIRD-PARTY-NOTICES.txt";

export const readerThemeCatalog = catalog;
const compiled = new Map();
// The repeated class outranks the host's Technical/Editorial/Compact rules
// without changing its viewport width, responsive padding or source anchors.
const documentScope = ".markdown.mv-document-theme.mv-document-theme.mv-document-theme";
const codeScope = ".markdown.mv-code-theme.mv-code-theme.mv-code-theme";
const forbiddenSelector = /(?:#app\b|\.app-nav\b|\.sidebar\b|\.sidebar-toggle\b|\.github-corner\b|\.content\b|\.cover\b|\.progress\b|\.search\b|\.md-sidebar-toc\b)/u;

/** Resolve a persisted theme ID within its mode, falling back for stale IDs. */
function findTheme(category, themeId, mode, fallbackId) {
  const availableThemes = catalog[category];
  return availableThemes.find(theme => theme.id === themeId && theme.modes.includes(mode))
    || availableThemes.find(theme => theme.id === fallbackId);
}

/** Remove remote resources before trusted bundled CSS reaches the CSS parser. */
function offlineCss(css) {
  return css.replace(/@import\s+[^;]+;/giu, "")
    .replace(/url\(\s*(?:"([^"]*)"|'([^']*)'|([^)]*))\s*\)/giu, (match, doubleQuotedUrl, singleQuotedUrl, unquotedUrl) => {
      const resourceUrl = (doubleQuotedUrl ?? singleQuotedUrl ?? unquotedUrl ?? "").trim();
      const embeddedImage = /^data:image\/(?:png|jpeg|gif|webp);base64,[a-z0-9+/=]+$/iu.test(resourceUrl);
      return embeddedImage ? match : "none";
    });
}

/** Scope an upstream selector to the article and reject website chrome rules. */
function scopeSelector(selector, scope) {
  if (forbiddenSelector.test(selector) || /:host|::slotted|::part|:root/iu.test(selector)) {
    return null;
  }

  let scopedSelector = selector.trim().replace(/^html\s+body\b/u, scope)
    .replace(/^(?:html|body)\b/u, scope)
    .replace(/\.markdown-preview\b/gu, scope);
  if (!scopedSelector.startsWith(scope)) {
    scopedSelector = scope + " " + scopedSelector;
  }
  return scopedSelector;
}

/** Preserve theme declarations and conditional rules while retaining reader layout. */
function serializeRules(rules, scope) {
  const output = [];
  for (const rule of rules) {
    if (rule.type === CSSRule.STYLE_RULE) {
      const selectors = rule.selectorText.split(",").flatMap(selector => {
        // All imported selectors are simple selector lists (no comma functions).
        const scopedSelector = scopeSelector(selector, scope);
        if (!scopedSelector) {
          return [];
        }

        // Markdig wraps blocks in sections for source and ink anchoring.
        return scopedSelector.startsWith(scope + " > ")
          ? [scopedSelector, scopedSelector.replace(scope + " > ", scope + " > section > ")]
          : [scopedSelector];
      });
      if (!selectors.length) {
        continue;
      }

      const declarations = [];
      const isRoot = selectors.includes(scope);
      for (const property of rule.style) {
        // Preview themes may include a complete website. The reader owns its
        // viewport, responsive padding and source anchors; themes own typography.
        const changesViewport = /^(?:height|min-height|max-height|width|min-width|max-width|overflow(?:-x|-y)?|position|inset|top|right|bottom|left|margin(?:-.+)?|padding(?:-.+)?|z-index)$/u.test(property);
        if (isRoot && changesViewport) {
          continue;
        }
        if (/^(?:animation|transition|behavior|-moz-binding)/u.test(property)) {
          continue;
        }

        let value = rule.style.getPropertyValue(property);
        if (isRoot && property === "font-size") {
          value = `calc(${value} * var(--reading-scale, 1))`;
        }
        const priority = rule.style.getPropertyPriority(property) ? "!important" : "";
        declarations.push(property + ":" + value + priority);
      }
      output.push(selectors.join(",") + "{" + declarations.join(";") + "}");
    } else if (rule.type === CSSRule.MEDIA_RULE || rule.type === CSSRule.SUPPORTS_RULE) {
      const nestedRules = serializeRules(rule.cssRules, scope);
      if (nestedRules) {
        const conditionPrefix = rule.type === CSSRule.MEDIA_RULE ? "@media " : "@supports ";
        output.push(conditionPrefix + rule.conditionText + "{" + nestedRules + "}");
      }
    }
    // Font faces, keyframes, page rules and other global at-rules are omitted.
  }
  return output.join("\n");
}

/** Compile each trusted asset once in a detached, never-adopted stylesheet. */
function compileCss(kind, themeId) {
  const cacheKey = kind + ":" + themeId;
  if (compiled.has(cacheKey)) {
    return compiled.get(cacheKey);
  }

  const sourceStyles = (kind === "document" ? previewStyles : codeStyles)[themeId];
  if (!sourceStyles) {
    return "";
  }

  const stylesheet = new CSSStyleSheet();
  stylesheet.replaceSync(offlineCss(sourceStyles));
  const scope = kind === "document" ? documentScope : codeScope;
  const css = serializeRules(stylesheet.cssRules, scope);
  compiled.set(cacheKey, css);
  return css;
}

/** Resolve independent mode preferences into the same reader and export styles. */
export function resolveReaderTheme(preferences = {}, mode = "light") {
  mode = mode === "dark" ? "dark" : "light";
  const documentTheme = findTheme("document", preferences[mode + "DocumentTheme"], mode, "markvibe");
  const selectedCode = findTheme("code", preferences[mode + "CodeTheme"], mode, "auto");
  const codeId = selectedCode.id === "auto" ? documentTheme.codeTheme : selectedCode.id;
  const palette = documentTheme.palette || null;
  const documentCss = documentTheme.id === "markvibe" ? "" : compileCss("document", documentTheme.id);
  const codeCss = codeId === "auto" ? "" : compileCss("code", codeId);
  const notices = documentCss || codeCss
    ? "/*!\n" + themeNotices.replaceAll("*/", "* /") + "\n*/"
    : "";
  const resetTokens = codeCss
    ? `${codeScope} pre>code{color:inherit;background:transparent;font-family:inherit}`
    + `${codeScope} [class*="hljs-"]{color:inherit;font-style:normal;font-weight:inherit}`
    : "";
  const surface = palette
    ? `.reader{background:${palette.background};color:${palette.foreground};`
    + `--panel:${palette.background};--text:${palette.foreground};--muted:${palette.muted};`
    + `--line:${palette.border};--code:${palette.code};--accent:${palette.accent}}`
    : "";
  // Place catalog typography before imported rules so parsed root fonts win.
  // The same selector outranks built-in document styles if a root rule is absent.
  const fontFamilyFallback = palette?.fontFamily
    ? `${documentScope}{font-family:${palette.fontFamily}}`
    : "";
  return {
    mode,
    documentTheme: documentTheme.id,
    codeTheme: codeId,
    palette,
    css: [notices, surface, fontFamilyFallback, documentCss, resetTokens, codeCss]
      .filter(Boolean)
      .join("\n")
  };
}

const prismAliases = {
  comment: "comment",
  quote: "comment",
  keyword: "keyword",
  "selector-tag": "tag",
  built_in: "builtin",
  type: "class-name",
  string: "string",
  regexp: "regex",
  symbol: "symbol",
  number: "number",
  literal: "boolean",
  attr: "attr-name",
  attribute: "attr-name",
  title: "function",
  section: "keyword",
  name: "tag",
  variable: "variable",
  params: "parameter",
  property: "property",
  operator: "operator",
  punctuation: "punctuation",
  meta: "prolog",
  addition: "inserted",
  deletion: "deleted",
  emphasis: "italic",
  strong: "bold",
  "selector-class": "selector",
  "selector-id": "selector"
};

/** Add Prism presentation aliases while retaining highlight.js token parsing. */
export function decorateCodeTokens(article) {
  for (const codeElement of article.querySelectorAll("pre code:not(.language-mermaid)")) {
    const languageClass = [...codeElement.classList]
      .find(className => className.startsWith("language-")) || "language-none";
    codeElement.classList.add(languageClass);
    codeElement.parentElement.classList.add(languageClass);

    for (const tokenElement of codeElement.querySelectorAll("[class*='hljs-']")) {
      for (const className of [...tokenElement.classList]) {
        const prismAlias = className.startsWith("hljs-") && prismAliases[className.slice(5)];
        if (prismAlias) {
          tokenElement.classList.add("token", prismAlias);
        }
      }
    }
  }
}

/** Apply article classes shared by the live reader and standalone exports. */
export function decorateThemeArticle(article, theme) {
  article.classList.toggle("mv-document-theme", theme.documentTheme !== "markvibe");
  article.classList.toggle("mv-code-theme", theme.codeTheme !== "auto");
  decorateCodeTokens(article);
}

/** Replace the reader's scoped stylesheet without mutating application chrome. */
export function applyReaderTheme(shadowRoot, article, theme) {
  if (!(shadowRoot instanceof ShadowRoot)) {
    throw new TypeError("Reader themes require a ShadowRoot.");
  }

  let themeStyle = shadowRoot.querySelector("style[data-mv-reader-theme]");
  if (!themeStyle) {
    themeStyle = document.createElement("style");
    themeStyle.dataset.mvReaderTheme = "";
    shadowRoot.append(themeStyle);
  }
  themeStyle.textContent = theme.css;
  if (article) {
    decorateThemeArticle(article, theme);
  }
}
