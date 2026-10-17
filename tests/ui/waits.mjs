/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; shared observable UI waits; 2026-10-16
 * Human guidance: reliable checks without fixed animation delays
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import { setTimeout as delay } from "node:timers/promises";

/** Retry an assertion until its observable state changes, retaining the useful final failure. */
export async function eventually(assertion, timeout = 15000) {
  const deadline = Date.now() + timeout;
  let lastError;
  while (Date.now() < deadline) {
    try {
      return await assertion();
    } catch (error) {
      lastError = error;
      await delay(100);
    }
  }
  throw lastError || new Error("The expected application state was not observed.");
}

/** Close the current settings modal and wait until its backdrop releases input. */
export async function closeSettings(page) {
  const dialog = page.locator(".mv-dialog").last();
  await dialog.getByText("Close", { exact: true }).last().click();
  await dialog.waitFor({ state: "hidden" });
}
