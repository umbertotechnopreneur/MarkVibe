/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; isolated Debug browser graph profiling; 2026-10-23
 * Human guidance: Umberto Giacobbi; profile the real graph with browser Developer Tools and local Markdown
 * Modified with AI: OpenAI Codex; report compositor and raster costs in DevTools summaries; 2026-10-25
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import { chromium } from "playwright";
import { mkdir, writeFile } from "node:fs/promises";
import { resolve, join } from "node:path";
import { once } from "node:events";
import assert from "node:assert/strict";

/** Read an explicit command-line value without assuming a user document directory. */
function option(name, fallback = "") {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : fallback;
}

const folder = option("--folder");
const output = resolve(option("--output", "artifacts/graph-profile"));
const base = option("--url", "http://localhost:5080");
if (!folder || !["localhost", "127.0.0.1", "[::1]"].includes(new URL(base).hostname)) {
  throw new Error("Provide --folder and a local Debug server URL. This profiler never uploads documents.");
}
await mkdir(output, { recursive: true });
const browser = await chromium.launch({ channel: "msedge", headless: !process.argv.includes("--headed") });
const context = await browser.newContext({ locale: "en-US", viewport: { width: 1600, height: 1000 }, reducedMotion: "no-preference" });
const page = await context.newPage();
const errors = [];
page.on("pageerror", error => errors.push(error.message));
// Use the supported read-only folder import instead of opening a native directory dialog.
await page.addInitScript(() => {
  window.showDirectoryPicker = undefined;
  let bridge;
  // Blazor caches resolved functions, so observe the bridge before its first managed call.
  Object.defineProperty(window, "markvibe", {
    configurable: true,
    get: () => bridge,
    set(value) {
      bridge = value;
      const original = value.call;
      value.call = async function (operation, argumentsJson, request) {
        const result = await original.call(this, operation, argumentsJson, request);
        if (operation === "workspace.open") {
          window.graphProfileWorkspace = JSON.parse(result);
        }
        if (operation === "map.suspend" || operation === "map.resume") {
          window.graphProfileWorkspaceReady = operation === "map.resume";
        }
        if (operation === "map.render" || operation === "map.selection") {
          const { map } = JSON.parse(argumentsJson);
          window.graphProfileEdges = map.edges;
          window.graphProfileReady = {
            indexed: map.indexComplete,
            cards: map.nodes.length,
            edges: map.edges.length,
            totalDocuments: map.totalDocuments
          };
        }
        return result;
      };
    }
  });
});
await context.route("**/*", route => {
  const url = new URL(route.request().url());
  return ["http:", "https:"].includes(url.protocol) && url.origin !== new URL(base).origin
    ? route.abort()
    : route.continue();
});

try {
  const manifestResponse = await context.request.get(new URL("/js/input-fingerprint.json", base).href);
  assert.ok(manifestResponse.ok(), "The local shell must expose its bundle build evidence.");
  assert.equal((await manifestResponse.json()).configuration, "Debug", "Profile only an explicit Debug client bundle.");
  await page.goto(base, { waitUntil: "networkidle" });
  await page.locator(".reader h1").waitFor({ timeout: 90000 });
  await page.locator('[dataid="CancelButton"]').waitFor({ state: "hidden", timeout: 90000 });
  await page.waitForFunction(() => window.graphProfileWorkspaceReady, null, { timeout: 90000 });
  const openFolderButton = page.locator('[dataid="OpenFolderButton"]:not([disabled])');
  await openFolderButton.waitFor({ state: "visible", timeout: 90000 });
  console.log("Debug shell ready");
  const chooser = page.waitForEvent("filechooser", { timeout: 90000 });
  await openFolderButton.click();
  console.log("Folder picker requested");
  await (await chooser).setFiles(resolve(folder));
  console.log("Folder selected");
  await page.waitForFunction(() => document.querySelector('[dataid="WorkspaceName"]')?.textContent !== "Field notes");
  await page.waitForFunction(() => window.graphProfileWorkspaceReady, null, { timeout: 90000 });
  await page.locator('[dataid="MapButton"]').click();
  await page.locator("[data-map-node]").first().waitFor({ timeout: 90000 });
  try {
    await page.waitForFunction(() => window.graphProfileReady?.indexed, null, { timeout: 90000 });
  } catch (error) {
    console.log("Index readiness", await page.evaluate(() => window.graphProfileReady ?? null));
    throw error;
  }
  await page.waitForTimeout(2500);
  const viewport = page.locator(".map-viewport");
  const bounds = await viewport.boundingBox();
  if (!bounds) {
    throw new Error("The graph viewport has no visible bounds.");
  }
  const structure = await viewport.evaluate(element => ({
    ...window.graphProfileReady,
    elements: element.querySelectorAll("*").length,
    width: element.clientWidth,
    height: element.clientHeight
  }));
  console.log("Graph ready", JSON.stringify(structure));
  const session = await context.newCDPSession(page);
  await session.send("Performance.enable");
  await session.send("Profiler.enable");
  await session.send("Profiler.setSamplingInterval", { interval: 1000 });
  const results = [];

  /** Capture DevTools timeline, CPU samples, browser counters and animation intervals around one repeatable gesture. */
  async function measure(name, action) {
    const events = [];
    const collect = event => events.push(...event.value);
    session.on("Tracing.dataCollected", collect);
    const before = await session.send("Performance.getMetrics");
    await session.send("Tracing.start", {
      categories: "devtools.timeline,disabled-by-default-devtools.timeline,disabled-by-default-devtools.timeline.stack,blink.user_timing,v8.execute",
      options: "sampling-frequency=1000"
    });
    await session.send("Profiler.start");
    await page.evaluate(() => {
      const samples = { frames: [], tasks: [], running: true, previous: performance.now() };
      window.graphProfileSamples = samples;
      samples.observer = new PerformanceObserver(list => samples.tasks.push(...list.getEntries().map(entry => entry.duration)));
      samples.observer.observe({ type: "longtask" });
      const frame = now => {
        samples.frames.push(now - samples.previous);
        samples.previous = now;
        if (samples.running) {
          requestAnimationFrame(frame);
        }
      };
      requestAnimationFrame(frame);
    });
    const start = performance.now();
    await action();
    await page.waitForTimeout(550);
    const elapsed = performance.now() - start;
    const frames = await page.evaluate(() => {
      const samples = window.graphProfileSamples;
      samples.running = false;
      samples.observer.disconnect();
      return { frames: samples.frames.slice(1), longTasks: samples.tasks };
    });
    const { profile } = await session.send("Profiler.stop");
    const completion = once(session, "Tracing.tracingComplete");
    await session.send("Tracing.end");
    await completion;
    session.off("Tracing.dataCollected", collect);
    const after = await session.send("Performance.getMetrics");
    const initial = new Map(before.metrics.map(metric => [metric.name, metric.value]));
    const metrics = Object.fromEntries(after.metrics.filter(metric => /Duration$|Count$|JSHeapUsedSize|Nodes/.test(metric.name))
      .map(metric => [metric.name, metric.value - (initial.get(metric.name) ?? 0)]));
    const durations = frames.frames.slice().sort((left, right) => left - right);
    const summary = {
      name, elapsed, metrics,
      frameCount: durations.length,
      frameMedian: durations[Math.floor(durations.length / 2)],
      frame95: durations[Math.floor(durations.length * .95)],
      framesOver32: durations.filter(value => value > 32).length,
      longTasks: frames.longTasks,
      // Compositor/raster costs can dominate even when JavaScript is mostly idle.
      // These trace totals overlap across threads and are not additive wall time.
      timeline: Object.fromEntries(["Layout", "UpdateLayoutTree", "Paint", "PrePaint", "Layerize", "Commit", "RasterTask", "GPUTask"].map(eventName => {
        const matching = events.filter(event => event.name === eventName && event.ph === "X");
        return [eventName, { count: matching.length, milliseconds: matching.reduce((sum, event) => sum + (event.dur ?? 0), 0) / 1000 }];
      }))
    };
    await writeFile(join(output, name + ".trace.json"), JSON.stringify({ traceEvents: events }));
    await writeFile(join(output, name + ".cpuprofile"), JSON.stringify(profile));
    results.push(summary);
    console.log(JSON.stringify(summary));
    // Check settled endpoint alignment outside the measured interval, including the first pan frame's origin.
    const geometry = await viewport.evaluate(element => {
      const cards = new Map([...element.querySelectorAll(".map-card")].map(card => [card.dataset.mapNode, card]));
      const connections = [...element.querySelectorAll(".map-connection")];
      let maximumError = 0;
      let checked = 0;
      for (const [index, connection] of connections.entries()) {
        const edge = window.graphProfileEdges[index];
        if (!edge || getComputedStyle(connection.parentElement).display === "none") {
          continue;
        }
        for (const [path, length] of [[edge.source, 0], [edge.target, connection.getTotalLength()]]) {
          const card = cards.get(path);
          if (!card || card.hidden) {
            continue;
          }
          const bounds = card.getBoundingClientRect();
          const point = connection.getPointAtLength(length).matrixTransform(connection.getScreenCTM());
          const midX = bounds.x + bounds.width / 2;
          const midY = bounds.y + bounds.height / 2;
          const error = Math.min(...[[bounds.left, midY], [bounds.right, midY], [midX, bounds.top], [midX, bounds.bottom]]
            .map(([x, y]) => Math.hypot(x - point.x, y - point.y)));
          maximumError = Math.max(maximumError, error);
          checked++;
        }
      }
      return {
        checked, maximumError,
        moving: element.closest(".document-map").classList.contains("map-camera-moving"),
        arrowsRestored: connections.every(connection => getComputedStyle(connection).markerEnd !== "none"),
        hitTargetsRestored: [...element.querySelectorAll(".map-connection-hit")]
          .every(connection => getComputedStyle(connection).pointerEvents === "stroke")
      };
    });
    summary.geometry = geometry;
    assert.ok(geometry.checked > 0 && geometry.maximumError < 2, "Graph endpoints must stay aligned after " + name);
    assert.equal(geometry.moving, false, "Camera presentation must settle after " + name);
    assert.ok(geometry.arrowsRestored && geometry.hitTargetsRestored, "Arrowheads and hit targets must return after " + name);
  }

  await measure("idle", () => page.waitForTimeout(2000));
  // Background near the viewport margin avoids moving a card or granting write access.
  const startX = bounds.x + 20;
  const startY = bounds.y + 30;
  await measure("pan", async () => {
    await page.mouse.move(startX, startY);
    await page.mouse.down();
    for (let index = 0; index < 80; index++) {
      await page.mouse.move(startX + Math.sin(index / 8) * 160 + 170, startY + Math.cos(index / 8) * 70 + 80);
    }
    await page.mouse.up();
  });
  await measure("zoom", async () => {
    await page.mouse.move(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
    for (let index = 0; index < 32; index++) {
      await page.mouse.wheel(0, index < 16 ? -55 : 55);
      await page.waitForTimeout(20);
    }
  });
  await measure("hover", async () => {
    const points = await viewport.evaluate(element => {
      const outer = element.getBoundingClientRect();
      return [...element.querySelectorAll("[data-map-node]")].map(card => card.getBoundingClientRect())
        .filter(rect => rect.width > 0 && rect.left >= outer.left && rect.top >= outer.top && rect.right <= outer.right && rect.bottom <= outer.bottom)
        .slice(0, 10).map(rect => ({ x: rect.x + rect.width / 2, y: rect.y + rect.height / 2 }));
    });
    for (let repeat = 0; repeat < 3; repeat++) {
      for (const point of points) {
        await page.mouse.move(point.x, point.y);
        await page.waitForTimeout(45);
      }
      await page.mouse.move(startX, startY);
    }
  });
  await writeFile(join(output, "summary.json"), JSON.stringify({
    browser: browser.version(), configuration: "Debug", structure, errors, results
  }, null, 2));
  if (errors.length) {
    throw new Error("Browser errors: " + errors.join("; "));
  }
} catch (error) {
  const state = await page.evaluate(() => ({
    workspace: window.graphProfileWorkspace,
    graph: window.graphProfileReady,
    status: document.querySelector('[dataid="StatusText"]')?.textContent,
    name: document.querySelector('[dataid="WorkspaceName"]')?.textContent
  }));
  await writeFile(join(output, "failure.json"), JSON.stringify({ message: error.message, state, errors }, null, 2));
  console.log("Profile failed", JSON.stringify(state));
  throw error;
} finally {
  await browser.close();
}
