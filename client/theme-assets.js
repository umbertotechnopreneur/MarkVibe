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

import catalog from "../src/MarkVibe.Core/Presentation/themes.json";
import previewAtomLight from "./themes/preview/atom-light.css.txt";
import previewGithubLight from "./themes/preview/github-light.css.txt";
import previewGothic from "./themes/preview/gothic.css.txt";
import previewMedium from "./themes/preview/medium.css.txt";
import previewNewsprint from "./themes/preview/newsprint.css.txt";
import previewOneLight from "./themes/preview/one-light.css.txt";
import previewSolarizedLight from "./themes/preview/solarized-light.css.txt";
import previewVue from "./themes/preview/vue.css.txt";
import previewAtomDark from "./themes/preview/atom-dark.css.txt";
import previewAtomMaterial from "./themes/preview/atom-material.css.txt";
import previewGithubDark from "./themes/preview/github-dark.css.txt";
import previewMonokai from "./themes/preview/monokai.css.txt";
import previewNight from "./themes/preview/night.css.txt";
import previewOneDark from "./themes/preview/one-dark.css.txt";
import previewSolarizedDark from "./themes/preview/solarized-dark.css.txt";
import codeDefault from "./themes/code/default.css.txt";
import codeAtomLight from "./themes/code/atom-light.css.txt";
import codeCoy from "./themes/code/coy.css.txt";
import codeFunky from "./themes/code/funky.css.txt";
import codeGithub from "./themes/code/github.css.txt";
import codeOneLight from "./themes/code/one-light.css.txt";
import codePenPaperCoffee from "./themes/code/pen-paper-coffee.css.txt";
import codeSolarizedLight from "./themes/code/solarized-light.css.txt";
import codeVue from "./themes/code/vue.css.txt";
import codeVs from "./themes/code/vs.css.txt";
import codeAtomDark from "./themes/code/atom-dark.css.txt";
import codeAtomMaterial from "./themes/code/atom-material.css.txt";
import codeDark from "./themes/code/dark.css.txt";
import codeGithubDark from "./themes/code/github-dark.css.txt";
import codeHopscotch from "./themes/code/hopscotch.css.txt";
import codeMonokai from "./themes/code/monokai.css.txt";
import codeOkaidia from "./themes/code/okaidia.css.txt";
import codeOneDark from "./themes/code/one-dark.css.txt";
import codePojoaque from "./themes/code/pojoaque.css.txt";
import codeSolarizedDark from "./themes/code/solarized-dark.css.txt";
import codeTwilight from "./themes/code/twilight.css.txt";
import codeXonokai from "./themes/code/xonokai.css.txt";

/** Bundled preview CSS indexed by stable catalog IDs; never link globally. */
export const previewStyles = {
  "atom-light": previewAtomLight,
  "github-light": previewGithubLight,
  "gothic": previewGothic,
  "medium": previewMedium,
  "newsprint": previewNewsprint,
  "one-light": previewOneLight,
  "solarized-light": previewSolarizedLight,
  "vue": previewVue,
  "atom-dark": previewAtomDark,
  "atom-material": previewAtomMaterial,
  "github-dark": previewGithubDark,
  "monokai": previewMonokai,
  "night": previewNight,
  "one-dark": previewOneDark,
  "solarized-dark": previewSolarizedDark
};

/** Bundled Prism presentation CSS for the reader token adapter. */
export const codeStyles = {
  "default": codeDefault,
  "atom-light": codeAtomLight,
  "coy": codeCoy,
  "funky": codeFunky,
  "github": codeGithub,
  "one-light": codeOneLight,
  "pen-paper-coffee": codePenPaperCoffee,
  "solarized-light": codeSolarizedLight,
  "vue": codeVue,
  "vs": codeVs,
  "atom-dark": codeAtomDark,
  "atom-material": codeAtomMaterial,
  "dark": codeDark,
  "github-dark": codeGithubDark,
  "hopscotch": codeHopscotch,
  "monokai": codeMonokai,
  "okaidia": codeOkaidia,
  "one-dark": codeOneDark,
  "pojoaque": codePojoaque,
  "solarized-dark": codeSolarizedDark,
  "twilight": codeTwilight,
  "xonokai": codeXonokai
};

export { catalog };
