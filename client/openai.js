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
 * Modified with AI: OpenAI Codex; explicit provider status, tests and desktop-only Codex transport; 2026-10-16
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

/** Reject unknown transport identifiers rather than silently sending through another provider. */
function requireAiProvider(provider) {
  if (provider !== "api-key" && provider !== "codex-cli") {
    throw new WorkspaceError("ai-provider", "Choose an available AI connection in Settings.");
  }
  return provider;
}

/** Read local configuration only; choosing Codex never inspects the saved API credential. */
export async function aiConnectionStatus(provider, signal) {
  checkCancelled(signal);
  requireAiProvider(provider);
  if (isDesktop()) {
    return nativeCall("ai.connection.status", { provider }, signal);
  }
  if (provider === "codex-cli") {
    return {
      provider,
      configured: false,
      available: false,
      message: "Codex CLI requires the Windows desktop app."
    };
  }

  const status = await openAiKeyStatus(signal);
  return {
    provider,
    configured: status.configured,
    available: status.storageAvailable,
    message: !status.storageAvailable
      ? "API key storage is unavailable."
      : status.configured ? "API key saved. Connection not tested." : "Add an API key in Settings."
  };
}

/** Test the explicit connection, saving an API candidate only after successful validation. */
export async function testAiConnection(args, signal) {
  checkCancelled(signal);
  const provider = requireAiProvider(args.provider);
  const candidate = provider === "api-key" && typeof args.apiKey === "string" ? args.apiKey.trim() : "";
  if (isDesktop()) {
    // A hidden API-key field must never cross the bridge for a Codex test.
    const payload = provider === "api-key" && candidate ? { provider, apiKey: candidate } : { provider };
    return nativeCall("ai.connection.test", payload, signal);
  }
  if (provider === "codex-cli") {
    return { passed: false, message: "Codex CLI requires the Windows desktop app." };
  }

  try {
    if (candidate) {
      await validateAndSaveOpenAiKey(candidate, signal);
    } else {
      // GET /models verifies the saved key without replacing it or generating content.
      await openAiModels(signal);
    }
    return { passed: true, message: candidate ? "API key validated and saved." : "Saved API key accepted." };
  } catch (error) {
    checkCancelled(signal);
    return {
      passed: false,
      message: error instanceof WorkspaceError ? error.message : "The API connection test could not be completed."
    };
  }
}

/** Reject a desktop-only provider before browser code can read an API credential. */
function requireBrowserApi(provider) {
  if (provider !== "api-key") {
    throw new WorkspaceError("codex-desktop-required", "Codex CLI requires the Windows desktop app.");
  }
}

/** Load the selected provider's catalog through one current bridge contract. */
export async function aiModels(args, signal) {
  checkCancelled(signal);
  const provider = requireAiProvider(args.provider);
  if (isDesktop()) {
    return nativeCall("ai.models", { provider }, signal);
  }

  requireBrowserApi(provider);
  return openAiModels(signal);
}

/** Prepare the captured request without selecting a different provider on failure. */
export async function prepareMarkdown(args, signal) {
  checkCancelled(signal);
  const provider = requireAiProvider(args.provider);
  if (isDesktop()) {
    return nativeCall("ai.prepare", { provider, request: args.request }, signal);
  }

  requireBrowserApi(provider);
  return prepareMarkdownWithOpenAi(args, signal);
}

/** Send one bounded edit through the explicit provider while keeping native credentials out of the browser. */
export async function editMarkdown(args, signal) {
  checkCancelled(signal);
  const provider = requireAiProvider(args.provider);
  if (isDesktop()) {
    return nativeCall("ai.edit", {
      provider,
      request: args.request,
      prefixTokens: args.prefixTokens
    }, signal);
  }

  requireBrowserApi(provider);
  return editMarkdownWithOpenAi(args, signal);
}

/** Report credential availability without returning the stored key to the settings dialog. */
async function openAiKeyStatus(signal) {
  checkCancelled(signal);
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
async function validateAndSaveOpenAiKey(apiKey, signal) {
  checkCancelled(signal);
  const candidateKey = normalizeKey(apiKey);
  const result = await sendOpenAi("models", null, candidateKey, signal, {
    timeoutMs: 20000,
    validatingKey: true
  });
  if (result?.object !== "list" || !Array.isArray(result.data)) {
    throw new WorkspaceError("openai-response-invalid", "OpenAI returned an unexpected validation response. The key was not saved.");
  }

  // A valid remote reply must not save a key after Settings has been closed.
  checkCancelled(signal);
  try {
    // This origin-isolated storage is deliberately disclosed as unencrypted in Settings.
    localStorage.setItem(keyStorageName, candidateKey);
  } catch {
    throw new WorkspaceError("openai-storage", "OpenAI accepted the key, but browser storage is unavailable. The key was not saved.");
  }

  return { configured: true, storageAvailable: true, storage: "browser" };
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
async function sendOpenAi(endpoint, payload, apiKey, signal, options = {}) {
  checkCancelled(signal);
  const timeoutMs = options.timeoutMs ?? 60000;
  const validatingKey = options.validatingKey === true;
  const timeoutMessage = validatingKey
    ? "OpenAI validation timed out. Check your connection and try again."
    : "OpenAI took too long. Try a lower reasoning level or a shorter selection.";
  const invalidResponseMessage = validatingKey
    ? "OpenAI returned an unexpected validation response. The key was not saved."
    : "OpenAI returned an unexpected response. Try again.";
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
      if (validatingKey) {
        throw requestFailure(response.status);
      }
      if (response.status === 400 || response.status === 404) {
        throw new WorkspaceError("openai-model-unavailable", "This model could not process the request. Refresh models or try a shorter selection.");
      }
      if ([401, 403, 429].includes(response.status)) {
        throw requestFailure(response.status);
      }
      throw new WorkspaceError("openai-request-failed", "OpenAI could not complete the Markdown edit. Try again later.");
    }
    const result = await readJson(response, controller.signal);
    checkCancelled(signal);
    if (controller.signal.aborted) {
      throw new WorkspaceError("openai-timeout", timeoutMessage);
    }

    return result;
  } catch (error) {
    checkCancelled(signal);
    if (error instanceof WorkspaceError) {
      if (validatingKey && error.code === "openai-response-invalid") {
        throw new WorkspaceError(error.code, invalidResponseMessage);
      }
      throw error;
    }
    if (controller.signal.aborted) {
      throw new WorkspaceError("openai-timeout", timeoutMessage);
    }
    if (error instanceof SyntaxError) {
      throw new WorkspaceError("openai-response-invalid", invalidResponseMessage);
    }
    throw new WorkspaceError("openai-network", "OpenAI could not be reached. Check your connection and try again.");
  } finally {
    clearTimeout(timeout);
    signal?.removeEventListener("abort", cancel);
  }
}

/** Return account model IDs; managed code intersects them with its documented text-model catalog. */
async function openAiModels(signal) {
  const result = await sendOpenAi("models", null, savedBrowserKey(), signal, { timeoutMs: 20000 });
  if (result?.object !== "list" || !Array.isArray(result.data)) {
    throw new WorkspaceError("openai-response-invalid", "OpenAI returned an unexpected model list.");
  }
  return result.data.filter(model => typeof model?.id === "string").slice(0, 4096).map(model => model.id);
}

/** Count this turn's fixed prefixes before generation; unavailable counters remain explicit estimates. */
async function prepareMarkdownWithOpenAi(args, signal) {
  const apiKey = savedBrowserKey();
  const counts = await Promise.all(args.counts.map(async payload => {
    try {
      const result = await sendOpenAi("responses/input_tokens", payload, apiKey, signal, { timeoutMs: 15000 });
      const tokenCount = result?.input_tokens;
      const validTokenCount = Number.isSafeInteger(tokenCount)
        && tokenCount >= 0
        && tokenCount <= 10000000;

      return validTokenCount ? tokenCount : -1;
    } catch {
      return -1;
    }
  }));
  checkCancelled(signal);
  return counts;
}

/** Submit exactly this turn's managed payload after context preflight; history and tools are never added. */
async function editMarkdownWithOpenAi(args, signal) {
  const response = await sendOpenAi("responses", args.payload, savedBrowserKey(), signal);
  checkCancelled(signal);
  return { response, prefixTokens: args.prefixTokens };
}
