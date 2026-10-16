/* VBWR B
 *
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 *
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 *
 * Created with AI: OpenAI Codex; official model/pricing catalog download; 2026-10-11
 * Human guidance: Umberto Giacobbi; retain model context and qualitative cost categories in JSON
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 *
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 *
 * VBWR E */
import { writeFile } from "node:fs/promises";

const catalogPath = new URL("../src/MarkVibe.Core/AI/openai-models.json", import.meta.url);
const pricingSource = "https://developers.openai.com/api/docs/pricing";
const specifications = [
  ["gpt-6.1-sol", "GPT-6.1 Sol", "Quality and cost in balance"],
  ["gpt-6-astra", "GPT-6 Astra", "For demanding revisions"],
  ["gpt-6-sol", "GPT-6 Sol", "General-purpose reasoning"],
  ["gpt-6-luna", "GPT-6 Luna", "Lightweight everyday editing"],
  ["gpt-5.6-sol", "GPT-5.6 Sol", "Complex document work"],
  ["gpt-5.6-terra", "GPT-5.6 Terra", "A balanced smaller model"],
  ["gpt-5.6-luna", "GPT-5.6 Luna", "Efficient focused revisions"]
];

/** Download only official public documentation, with bounded size, timeout and no API credentials. */
async function officialMarkdown(source) {
  const response = await fetch(source + ".md", { signal: AbortSignal.timeout(25000), redirect: "error" });
  if (!response.ok) {
    throw new Error("Official model documentation could not be downloaded: " + source);
  }
  const text = await response.text();
  if (Buffer.byteLength(text) > 1024 * 1024) {
    throw new Error("Official documentation exceeded the catalog download limit.");
  }
  return text;
}

/** Extract a required documented integer; changed source formats fail before replacing the existing JSON. */
function documentedInteger(markdown, expression, field) {
  const value = markdown.match(expression)?.[1];
  const tokens = Number(value?.replaceAll(",", ""));
  if (!Number.isSafeInteger(tokens) || tokens <= 0) {
    throw new Error("The official document no longer exposes " + field + ". Review the catalog downloader.");
  }
  return tokens;
}

/** Read a Standard USD-per-million price from the model's first text-pricing table. */
function documentedPrice(markdown, metric) {
  const pricing = markdown.split("## Pricing")[1]?.split("## Endpoints")[0] || "";
  const match = pricing.match(new RegExp("\\|\\s*" + metric + "\\s*\\|\\s*\\$([0-9.]+)\\s*\\|", "i"));
  const value = Number(match?.[1]);
  if (!Number.isFinite(value) || value < 0) {
    throw new Error("A required official text price is missing: " + metric);
  }
  return value;
}

/** Classify relative output-token cost for the application; these labels are not official billing estimates. */
function costTier(outputPrice) {
  if (outputPrice <= 5) {
    return "cheap";
  }
  return outputPrice <= 20 ? "affordable" : "prohibitive";
}

/** Parse limits and pricing facts without retaining copyrighted prose or changing the supported model choices. */
async function modelEntry(specification) {
  const [id, label, description] = specification;
  const source = "https://developers.openai.com/api/docs/models/" + id;
  const markdown = await officialMarkdown(source);
  if (!markdown.includes("Model ID: `" + id + "`")) {
    throw new Error("The official model identity did not match " + id);
  }
  const contextWindow = documentedInteger(markdown, /([\d,]+) context window/i, "context window");
  const maxOutputTokens = documentedInteger(markdown, /([\d,]+) max output tokens/i, "maximum output");
  const explicitInput = markdown.match(/Maximum input tokens:\s*([\d,]+)/i);
  const maxInputTokens = explicitInput
    ? documentedInteger(markdown, /Maximum input tokens:\s*([\d,]+)/i, "maximum input")
    : contextWindow - maxOutputTokens;
  if (maxInputTokens <= 0 || maxInputTokens + maxOutputTokens > contextWindow) {
    throw new Error("The model input/output limits cannot fit its documented context window.");
  }
  const input = documentedPrice(markdown, "Input");
  const cachedInput = documentedPrice(markdown, "Cached input");
  const output = documentedPrice(markdown, "Output");
  const thresholdMatch = markdown.match(/Prompts with\s*(?:more than\s*|>\s*)([\d,]+K?)\s+input tokens/i);
  const longContextThreshold = thresholdMatch
    ? Number(thresholdMatch[1].replaceAll(",", "").replace(/K/i, "000")) : null;
  const inputMultiplier = longContextThreshold
    ? Number(markdown.match(/(?:priced at|rates and)\s*([\d.]+)x input/i)?.[1]) : 1;
  const outputMultiplier = longContextThreshold
    ? Number(markdown.match(/([\d.]+)x output/i)?.[1]) : 1;
  if (!Number.isFinite(inputMultiplier) || !Number.isFinite(outputMultiplier)) {
    throw new Error("The official long-context multipliers are missing for " + id);
  }
  const cacheWriteRow = markdown.match(/\|\s*Cache writes\s*\|\s*\$([\d.]+)\s*\|/i);
  const cacheWriteMultiplier = markdown.match(/Cache writes are billed at\s*([\d.]+)x/i);
  const cacheWrite = cacheWriteRow ? Number(cacheWriteRow[1])
    : cacheWriteMultiplier ? input * Number(cacheWriteMultiplier[1]) : null;
  const promotion = markdown.match(/promotional pricing[^\n]*?(?:through|until)\s+([^\n.]+)[.]/i)?.[1] || null;
  return {
    id, label, description, contextWindow, maxInputTokens, maxOutputTokens,
    reasoningEfforts: ["low", "medium", "high", "xhigh", "max"],
    standardUsdPerMillionTokens: { input, cachedInput, cacheWrite, output },
    longContext: { thresholdInputTokens: longContextThreshold, inputMultiplier, outputMultiplier },
    costTier: costTier(output),
    longContextCostTier: costTier(output * outputMultiplier),
    promotionalPricingThrough: promotion,
    source
  };
}

/** Replace the public catalog only after every model and the official pricing page has downloaded successfully. */
async function updateCatalog() {
  const downloads = await Promise.allSettled([officialMarkdown(pricingSource), ...specifications.map(modelEntry)]);
  const failure = downloads.find(result => result.status === "rejected");
  if (failure) {
    throw failure.reason;
  }
  const models = downloads.slice(1).map(result => result.value);
  const catalog = {
    schemaVersion: 1,
    retrievedAt: new Date().toISOString(),
    source: pricingSource,
    pricingMode: "standard",
    currency: "USD",
    costClassification: {
      basis: "Standard output-token price; app-defined qualitative categories, not a quote",
      cheapMaximumOutputUsdPerMillion: 5,
      affordableMaximumOutputUsdPerMillion: 20
    },
    models
  };
  await writeFile(catalogPath, JSON.stringify(catalog, null, 2) + "\n", "utf8");
  console.log("Saved the official OpenAI limits/pricing snapshot for " + models.length + " models.");
}

await updateCatalog();
