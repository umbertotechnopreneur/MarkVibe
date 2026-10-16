/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Created with AI: OpenAI Codex; OpenAI credential and Markdown request transport; 2026-10-11
 * Human guidance: Umberto Giacobbi; disclose insecure browser storage and validate before saving
 * Modified with AI: OpenAI Codex; stateless generation, context preflight and cancellation; 2026-10-11
 * Human guidance: Umberto Giacobbi; stateless scoped edits and a one-minute deadline
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */

import { isDesktop, nativeCall } from "./native.js";
import { WorkspaceError, checkCancelled } from "./path.js";

const keyStorageName = "markvibe.openai.apiKey";

/** Report credential availability without returning the stored key to the settings dialog. */
export async function openAiKeyStatus(signal) {
  checkCancelled(signal);
  if (isDesktop()) {
    return nativeCall("openai.status", {}, signal);
  }
  try {
    return {
      configured: !!localStorage.getItem(keyStorageName)?.trim(),
      storageAvailable: true,
      storage: "browser"
    };
  } catch {
    return { configured: false, storageAvailable: false, storage: "browser" };
  }
}

/** Trim pasted whitespace and reject values that cannot safely form an authorization header. */
function normalizeKey(apiKey) {
  const candidateKey = typeof apiKey === "string" ? apiKey.trim() : "";
  if (!candidateKey) {
    throw new WorkspaceError("openai-key-required", "Enter an OpenAI API key first.");
  }
  if (candidateKey.length > 4096 || /[^\x21-\x7e]/u.test(candidateKey)) {
    throw new WorkspaceError("openai-key-format", "The API key contains unsupported characters or is too long.");
  }
  return candidateKey;
}

/** Describe API failures using fixed text instead of reflecting remote bodies or credentials. */
function requestFailure(statusCode) {
  switch (statusCode) {
    case 401:
      return new WorkspaceError("openai-key-rejected", "OpenAI rejected this API key. Check it and try again.");
    case 403:
      return new WorkspaceError("openai-access-denied", "OpenAI denied access. Check this key's permissions and your account restrictions.");
    case 429:
      return new WorkspaceError("openai-rate-limit", "OpenAI limited the request. Check your account limits and try again later.");
    default:
      return new WorkspaceError("openai-request-failed", "OpenAI could not complete the validation request. Try again later.");
  }
}

/** Validate the candidate through OpenAI before replacing any previously saved credential. */
export async function validateAndSaveOpenAiKey(apiKey, signal) {
  checkCancelled(signal);
  const candidateKey = normalizeKey(apiKey);
  if (isDesktop()) {
    // The Windows host owns both HTTPS validation and environment persistence.
    return nativeCall("openai.validate", { apiKey: candidateKey }, signal);
  }

  const controller = new AbortController();
  /** Stop the network request when the settings dialog closes. */
  const cancelRequest = () => controller.abort();
  signal?.addEventListener("abort", cancelRequest, { once: true });
  const timeout = setTimeout(cancelRequest, 20000);
  try {
    const response = await fetch("https://api.openai.com/v1/models", {
      method: "GET",
      headers: { Authorization: "Bearer " + candidateKey },
      credentials: "omit",
      cache: "no-store",
      redirect: "error",
      referrerPolicy: "no-referrer",
      signal: controller.signal
    });
    if (!response.ok) {
      throw requestFailure(response.status);
    }
    const result = await response.json();
    if (result?.object !== "list" || !Array.isArray(result.data)) {
      throw new WorkspaceError("openai-response-invalid", "OpenAI returned an unexpected validation response. The key was not saved.");
    }
    checkCancelled(signal);
    if (controller.signal.aborted) {
      throw new WorkspaceError("openai-timeout", "OpenAI validation timed out. Check your connection and try again.");
    }
    // This origin-isolated storage is deliberately disclosed as unencrypted in Settings.
    try {
      localStorage.setItem(keyStorageName, candidateKey);
    } catch {
      throw new WorkspaceError("openai-storage", "OpenAI accepted the key, but browser storage is unavailable. The key was not saved.");
    }
    return { configured: true, storageAvailable: true, storage: "browser" };
  } catch (error) {
    checkCancelled(signal);
    if (error instanceof WorkspaceError) {
      throw error;
    }
    if (controller.signal.aborted) {
      throw new WorkspaceError("openai-timeout", "OpenAI validation timed out. Check your connection and try again.");
    }
    if (error instanceof SyntaxError) {
      throw new WorkspaceError("openai-response-invalid", "OpenAI returned an unexpected validation response. The key was not saved.");
    }
    throw new WorkspaceError("openai-network", "OpenAI could not be reached. Check your connection and try again.");
  } finally {
    clearTimeout(timeout);
    signal?.removeEventListener("abort", cancelRequest);
  }
}

/** Read the browser credential only in the transport layer, never into component state or documents. */
function savedBrowserKey() {
  try {
    return normalizeKey(localStorage.getItem(keyStorageName));
  } catch (error) {
    if (error instanceof WorkspaceError) {
      throw error;
    }
    throw new WorkspaceError("openai-storage", "The OpenAI key could not be read. Open Settings and validate it again.");
  }
}

/** Bound remote JSON while streaming so large replies cannot grow an unlimited text buffer. */
async function readJson(response, signal) {
  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  const chunks = [];
  let bytes = 0;
  try {
    while (true) {
      checkCancelled(signal);
      const chunk = await reader.read();
      if (chunk.done) {
        break;
      }
      bytes += chunk.value.byteLength;
      if (bytes > 8 * 1024 * 1024) {
        throw new WorkspaceError("openai-response-invalid", "OpenAI returned an oversized response. Try a shorter selection.");
      }
      chunks.push(decoder.decode(chunk.value, { stream: true }));
    }
    chunks.push(decoder.decode());
    return JSON.parse(chunks.join(""));
  } finally {
    await reader.cancel().catch(() => {});
    reader.releaseLock();
  }
}

/** Call a fixed OpenAI route with cancellation, no redirects/referrers and credential-free error messages. */
async function sendOpenAi(endpoint, payload, apiKey, signal, timeoutMs = 60000) {
  checkCancelled(signal);
  const controller = new AbortController();
  /** Cancel both the request and response-body stream when the caller stops the operation. */
  const cancel = () => controller.abort();
  signal?.addEventListener("abort", cancel, { once: true });
  const timeout = setTimeout(cancel, timeoutMs);
  try {
    const response = await fetch("https://api.openai.com/v1/" + endpoint, {
      method: payload ? "POST" : "GET",
      headers: { Authorization: "Bearer " + apiKey, ...(payload ? { "Content-Type": "application/json" } : {}) },
      body: payload ? JSON.stringify(payload) : undefined,
      credentials: "omit",
      cache: "no-store",
      redirect: "error",
      referrerPolicy: "no-referrer",
      signal: controller.signal
    });
    if (!response.ok) {
      if (response.status === 400 || response.status === 404) {
        throw new WorkspaceError("openai-model-unavailable", "This model could not process the request. Refresh models or try a shorter selection.");
      }
      if ([401, 403, 429].includes(response.status)) {
        throw requestFailure(response.status);
      }
      throw new WorkspaceError("openai-request-failed", "OpenAI could not complete the Markdown edit. Try again later.");
    }
    return await readJson(response, controller.signal);
  } catch (error) {
    checkCancelled(signal);
    if (error instanceof WorkspaceError) {
      throw error;
    }
    if (controller.signal.aborted) {
      throw new WorkspaceError("openai-timeout", "OpenAI took too long. Try a lower reasoning level or a shorter selection.");
    }
    if (error instanceof SyntaxError) {
      throw new WorkspaceError("openai-response-invalid", "OpenAI returned an unexpected response. Try again.");
    }
    throw new WorkspaceError("openai-network", "OpenAI could not be reached. Check your connection and try again.");
  } finally {
    clearTimeout(timeout);
    signal?.removeEventListener("abort", cancel);
  }
}

/** Return account model IDs; managed code intersects them with its documented text-model catalog. */
export async function openAiModels(signal) {
  if (isDesktop()) {
    return nativeCall("openai.models", {}, signal);
  }
  const result = await sendOpenAi("models", null, savedBrowserKey(), signal, 20000);
  if (result?.object !== "list" || !Array.isArray(result.data)) {
    throw new WorkspaceError("openai-response-invalid", "OpenAI returned an unexpected model list.");
  }
  return result.data.filter(model => typeof model?.id === "string").slice(0, 4096).map(model => model.id);
}

/** Count this turn's fixed prefixes before generation; unavailable counters remain explicit estimates. */
export async function prepareMarkdownWithOpenAi(args, signal) {
  if (isDesktop()) {
    return nativeCall("openai.prepare", { request: args.request }, signal);
  }
  const apiKey = savedBrowserKey();
  const counts = await Promise.all(args.counts.map(async payload => {
    try {
      const result = await sendOpenAi("responses/input_tokens", payload, apiKey, signal, 15000);
      return Number.isSafeInteger(result?.input_tokens) && result.input_tokens >= 0 && result.input_tokens <= 10000000
        ? result.input_tokens : -1;
    } catch {
      return -1;
    }
  }));
  checkCancelled(signal);
  return counts;
}

/** Submit exactly this turn's managed payload after context preflight; history and tools are never added. */
export async function editMarkdownWithOpenAi(args, signal) {
  if (isDesktop()) {
    // The native process rebuilds the fixed payload and keeps the credential outside the WebView.
    return nativeCall("openai.edit", { request: args.request, prefixTokens: args.prefixTokens }, signal);
  }
  const response = await sendOpenAi("responses", args.payload, savedBrowserKey(), signal);
  checkCancelled(signal);
  return { response, prefixTokens: args.prefixTokens };
}
