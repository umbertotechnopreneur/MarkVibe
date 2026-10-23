/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Created with AI: OpenAI Codex; synthetic rendering and source-resource regression checks; 2026-10-23
 * Human guidance: Umberto Giacobbi; optimize without regressions and run relevant JavaScript tests
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import test from "node:test";
import assert from "node:assert/strict";
import { build } from "esbuild";
import { fileURLToPath } from "node:url";
import { validateBundleConfiguration } from "../../scripts/client-inputs.mjs";

test("Release bundle validation rejects Debug output and source maps while retaining optimized Debug builds", () => {
  const optimized = { configuration: "Release", outputs: ["src/MarkVibe.Browser/wwwroot/js/markvibe.js"] };
  const debug = { configuration: "Debug", outputs: [...optimized.outputs, optimized.outputs[0] + ".map"] };
  assert.doesNotThrow(() => validateBundleConfiguration(optimized, "Release"));
  assert.doesNotThrow(() => validateBundleConfiguration(optimized, "Debug"));
  assert.doesNotThrow(() => validateBundleConfiguration(debug, "Debug"));
  assert.throws(() => validateBundleConfiguration(debug, "Release"), /Debug client assets/);
  assert.throws(() => validateBundleConfiguration(debug), /Debug client assets/);
  assert.throws(() => validateBundleConfiguration({ ...debug, configuration: "Release" }, "Release"), /source maps/);
  assert.throws(() => validateBundleConfiguration({ outputs: optimized.outputs }, "Release"), /configuration evidence/);
});

// Bundle the real hosts with inert DOM/Monaco boundaries; no browser or desktop is started.
const result = await build({
  stdin: {
    contents: `export { MapHost } from "./client/map.js";
      export { MapAtlasRenderer } from "./client/map-atlas.js";
      export { MapExplorationRenderer } from "./client/map-effects.js";
      export { SourceHost, disposeSourceModels } from "./client/source.js";
      export { models } from "./client/monaco-adapter.js";`,
    resolveDir: fileURLToPath(new URL("../../", import.meta.url))
  },
  bundle: true,
  write: false,
  format: "esm",
  platform: "node",
  plugins: [{
    name: "synthetic-host-boundaries",
    /** Substitute only browser integration boundaries, retaining host lifecycle and drawing code. */
    setup(builder) {
      builder.onResolve({ filter: /\/(markdown|workspaces|monaco-adapter)\.js$/ }, args => ({ path: args.path.split("/").at(-1), namespace: "fixture" }));
      builder.onLoad({ filter: /markdown\.js$/, namespace: "fixture" }, () => ({ contents: `
        export function createRoot() { throw new Error("DOM mounting is outside this unit check"); }
        export function textElement() { return {}; }
        export function uiText() { return {}; }
      ` }));
      builder.onLoad({ filter: /workspaces\.js$/, namespace: "fixture" }, () => ({ contents: `
        export function enableMapSaving() { throw new Error("No native calls in this check"); }
      ` }));
      builder.onLoad({ filter: /monaco-adapter\.js$/, namespace: "fixture" }, () => ({ contents: `
        export const models = [];
        export const monaco = {
          Uri: { parse(value) { return value; } },
          editor: {
            createModel(source) {
              const model = { disposed: false, getValue() { return source; }, dispose() { this.disposed = true; } };
              models.push(model);
              return model;
            },
            create() {
              return {
                layout() {}, dispose() {}, saveViewState() { return {}; }, restoreViewState() {},
                onDidScrollChange() { return { dispose() {} }; },
                onDidChangeCursorSelection() { return { dispose() {} }; }
              };
            }
          }
        };
      ` }));
    }
  }]
});
const { MapHost, MapAtlasRenderer, MapExplorationRenderer, SourceHost, disposeSourceModels, models } = await import(
  "data:text/javascript;base64," + Buffer.from(result.outputFiles[0].text).toString("base64")
);

// Node has no animation or document surface. Inert globals are scoped to this test process.
globalThis.requestAnimationFrame = () => { throw new Error("Unexpected animation request"); };
globalThis.cancelAnimationFrame = () => {};
globalThis.matchMedia = () => ({ matches: false });
globalThis.document = {
  documentElement: { dataset: { theme: "dark" } },
  createElement() { throw new Error("Unexpected DOM element creation"); },
  createElementNS() { throw new Error("Unexpected SVG element creation"); }
};

/** Provide only the source-host state needed to exercise actual asynchronous render and disposal paths. */
function sourceHost() {
  return Object.assign(Object.create(SourceHost.prototype), {
    revision: 0,
    container: { clientWidth: 800, isConnected: true },
    content: { replaceChildren() {} },
    capture() { return { sourceLine: 1 }; },
    emit() {}
  });
}

/** Model DOM identity and observable writes without introducing layout or a browser dependency. */
function drawingElement(events) {
  const attributes = new Map();
  const classes = new Set();
  let text = "";
  let hidden = false;
  const element = {
    children: [],
    parentNode: null,
    style: new Proxy({
      setProperty(name, value) { this[name] = value; },
      getPropertyValue(name) { return this[name] ?? ""; }
    }, {
      set(target, name, value) {
        events.push("write:style");
        target[name] = value;
        return true;
      }
    }),
    classList: {
      contains(name) { return classes.has(name); },
      add(...names) {
        for (const name of names) {
          this.toggle(name, true);
        }
      },
      remove(...names) {
        for (const name of names) {
          this.toggle(name, false);
        }
      },
      toggle(name, enabled) {
        if (classes.has(name) !== enabled) {
          events.push("write:class");
          if (enabled) {
            classes.add(name);
          } else {
            classes.delete(name);
          }
        }
      }
    },
    get textContent() { return text; },
    set textContent(value) { events.push("write:text"); text = value; },
    get hidden() { return hidden; },
    set hidden(value) { events.push("write:hidden"); hidden = value; },
    getAttribute(name) { return attributes.get(name) ?? null; },
    setAttribute(name, value) { events.push("write:attribute"); attributes.set(name, String(value)); },
    addEventListener() {},
    append(...children) {
      events.push("write:append");
      for (const child of children) {
        child.remove();
        this.children.push(child);
        child.parentNode = this;
      }
    },
    replaceChildren(...children) {
      events.push("write:replace");
      for (const child of this.children) {
        child.parentNode = null;
      }
      this.children = [];
      this.append(...children);
    },
    remove() {
      if (this.parentNode) {
        events.push("write:remove");
        this.parentNode.children = this.parentNode.children.filter(child => child !== this);
        this.parentNode = null;
      }
    }
  };
  return element;
}

/** Build a measured viewport and retained atlas surface to exercise camera and minimap updates. */
function atlasRenderer(events) {
  const host = {
    x: 0, y: 0, scale: .3, selected: "a.md",
    minX: 0, minY: 0, worldWidth: 1400, worldHeight: 900,
    metrics: { width: 200, height: 120 },
    small() { events.push("read:compact"); return false; },
    viewport: {
      get clientWidth() { events.push("read:width"); return 1000; },
      get clientHeight() { events.push("read:height"); return 700; }
    },
    body: drawingElement(events), world: drawingElement(events),
    map: { nodes: [{ path: "a.md", title: "First" }, { path: "b.md", title: "Second" }] },
    cards: new Map([
      ["a.md", { container: { hidden: false } }],
      ["b.md", { container: { hidden: false } }]
    ]),
    nodes: new Map([
      ["a.md", { x: 300, y: 600, color: "#123456" }],
      ["b.md", { x: 1000, y: 600, color: "#abcdef" }]
    ])
  };
  return Object.assign(Object.create(MapAtlasRenderer.prototype), {
    host, groups: new Map(), labels: new Map(), miniMarks: new Map(),
    overview: drawingElement(events), overviewLabels: drawingElement(events),
    scaleValue: drawingElement(events), scaleMode: drawingElement(events), scaleLabel: drawingElement(events),
    miniNodes: drawingElement(events), miniViewport: drawingElement(events)
  });
}

test("atlas camera updates batch viewport reads and preserve unchanged zoom labels", context => {
  const events = [];
  context.mock.method(globalThis.document, "createElement", () => drawingElement(events));
  const atlas = atlasRenderer(events);
  atlas.updateZoom();
  const firstWrite = events.findIndex(event => event.startsWith("write:"));
  assert.equal(events.slice(firstWrite).some(event => event.startsWith("read:")), false);
  assert.equal(atlas.labels.size, 2);
  const labels = [...atlas.labels.values()];
  events.length = 0;
  atlas.updateZoom();
  assert.equal(events.some(event => event.startsWith("write:")), false);
  atlas.host.x = 15;
  events.length = 0;
  atlas.updateZoom();
  assert.deepEqual([...atlas.labels.values()], labels);
  assert.equal(events.includes("write:text"), false);
  assert.equal(events.slice(events.findIndex(event => event.startsWith("write:"))).some(event => event.startsWith("read:")), false);
  atlas.host.scale = .8;
  atlas.updateZoom();
  assert.equal(atlas.overviewLabels.hidden, true);
  assert.equal(atlas.scaleMode.textContent, "Documents");
  assert.equal(atlas.scaleValue.textContent, "80% · ");
});

test("minimap moves retain SVG marks and reconcile hidden, removed and selected documents", context => {
  const events = [];
  context.mock.method(globalThis.document, "createElementNS", () => drawingElement(events));
  const atlas = atlasRenderer(events);
  atlas.drawMinimap();
  const first = atlas.miniMarks.get("document:a.md");
  const second = atlas.miniMarks.get("document:b.md");
  events.length = 0;
  atlas.drawMinimap();
  assert.equal(events.some(event => event.startsWith("write:")), false);
  atlas.host.nodes.get("a.md").x = 420;
  events.length = 0;
  atlas.drawMinimap();
  assert.equal(atlas.miniMarks.get("document:a.md"), first);
  assert.equal(atlas.miniMarks.get("document:b.md"), second);
  assert.equal(first.getAttribute("x"), "420");
  assert.equal(events.filter(event => event === "write:attribute").length, 1);
  assert.equal(events.includes("write:replace"), false);
  atlas.host.selected = "b.md";
  atlas.drawMinimap();
  assert.equal(first.getAttribute("fill"), "#123456");
  assert.equal(second.getAttribute("fill"), "var(--accent)");
  atlas.host.cards.get("a.md").container.hidden = true;
  atlas.drawMinimap();
  assert.equal(atlas.miniMarks.has("document:a.md"), false);
  assert.equal(first.parentNode, null);
  atlas.host.cards.delete("b.md");
  atlas.drawMinimap();
  assert.equal(atlas.miniMarks.size, 0);
  assert.equal(atlas.miniNodes.children.length, 0);
});

test("relation focus retains unchanged arrow markers and updates incoming, outgoing and route directions", context => {
  const events = [];
  const edgePaths = [["a.md", "b.md"], ["b.md", "a.md"], ["b.md", "c.md"], ["c.md", "d.md"], ["a.md", "c.md"]]
    .map(([source, target]) => ({
      edge: { source, target }, path: drawingElement(events), group: drawingElement(events)
    }));
  edgePaths[4].path.classList.toggle("route-connection", true);
  const host = {
    id: "fixture", selected: "a.md", edgePaths,
    cards: new Map(["a.md", "b.md", "c.md", "d.md"].map(path => [path, { container: drawingElement(events) }]))
  };
  const effects = Object.assign(Object.create(MapExplorationRenderer.prototype), {
    host, state: { lensEnabled: true }, hovered: null
  });
  effects.applyLens();
  assert.deepEqual(edgePaths.map(item => item.path.getAttribute("marker-end")), [
    "url(#arrow-outgoing-fixture)", "url(#arrow-incoming-fixture)", "url(#arrow-fixture)",
    "url(#arrow-fixture)", "url(#arrow-route-fixture)"
  ]);
  context.mock.method(effects, "applyLens");
  events.length = 0;
  effects.focus("a.md");
  effects.focus("a.md");
  effects.focus(null);
  assert.equal(effects.applyLens.mock.callCount(), 0);
  assert.equal(events.includes("write:attribute"), false);
  effects.focus("b.md");
  assert.equal(effects.applyLens.mock.callCount(), 1);
  assert.equal(events.filter(event => event === "write:attribute").length, 3);
  assert.deepEqual(edgePaths.map(item => item.path.getAttribute("marker-end")), [
    "url(#arrow-incoming-fixture)", "url(#arrow-outgoing-fixture)", "url(#arrow-outgoing-fixture)",
    "url(#arrow-fixture)", "url(#arrow-route-fixture)"
  ]);
  effects.state.lensEnabled = false;
  effects.applyLens();
  assert.deepEqual(edgePaths.map(item => item.path.getAttribute("marker-end")), [
    "url(#arrow-fixture)", "url(#arrow-fixture)", "url(#arrow-fixture)", "url(#arrow-fixture)", "url(#arrow-route-fixture)"
  ]);
  events.length = 0;
  effects.applyLens();
  assert.equal(events.includes("write:attribute"), false);
});

test("graph camera batches input, reads before writes and flushes final pan without changing stroke sizes", context => {
  let nextId = 0;
  const queued = new Map();
  context.mock.method(globalThis, "requestAnimationFrame", callback => {
    queued.set(++nextId, callback);
    return nextId;
  });
  context.mock.method(globalThis, "cancelAnimationFrame", id => queued.delete(id));
  const events = [];
  const strokes = [];
  const atlas = atlasRenderer(events);
  const host = Object.assign(Object.setPrototypeOf(atlas.host, MapHost.prototype), {
    atlas,
    scale: .8,
    effects: { cancelRectangle() {} },
    graph: { style: { setProperty(name, value) { events.push("write:stroke"); strokes.push([name, value]); } } }
  });
  for (let position = 1; position <= 100; position++) {
    host.x = position;
    host.scheduleTransform();
  }
  assert.equal(queued.size, 1);
  assert.equal(host.world.style.transform, undefined);
  const [frameId, callback] = queued.entries().next().value;
  queued.delete(frameId);
  callback();
  assert.equal(host.world.style.transform, "translate(100px,0px) scale(0.8)");
  assert.equal(strokes.length, 2);
  assert.equal(events.slice(events.findIndex(event => event.startsWith("write:"))).some(event => event.startsWith("read:")), false);

  events.length = 0;
  host.x = 150;
  host.scheduleTransform();
  host.x = 170;
  host.transform();
  assert.equal(queued.size, 0);
  assert.equal(host.world.style.transform, "translate(170px,0px) scale(0.8)");
  assert.equal(strokes.length, 2);
  assert.equal(events.slice(events.findIndex(event => event.startsWith("write:"))).some(event => event.startsWith("read:")), false);

  host.scale = .5;
  host.transform();
  assert.deepEqual(strokes.slice(-2), [["--edge-hit-width", "28px"], ["--edge-hover-width", "6px"]]);
  assert.equal(strokes.length, 4);
  host.graph = { style: host.graph.style };
  host.transform();
  assert.equal(strokes.length, 6);
  host.scheduleTransform();
  host.cancelScheduledTransform();
  assert.equal(queued.size, 0);
});

test("camera gestures freeze strokes, settle the current pointer and cancel obsolete restoration timers", context => {
  let nextTimer = 0;
  const timers = new Map();
  const cancelledFrames = [];
  context.mock.method(globalThis, "setTimeout", callback => {
    timers.set(++nextTimer, callback);
    return nextTimer;
  });
  context.mock.method(globalThis, "clearTimeout", timer => timers.delete(timer));
  context.mock.method(globalThis, "cancelAnimationFrame", frame => cancelledFrames.push(frame));
  const events = [];
  const strokes = [];
  const focused = [];
  let geometryUpdates = 0;
  let connectionAnimations = 0;
  const atlas = atlasRenderer(events);
  const target = Object.assign(drawingElement(events), {
    dataset: { mapNode: "b.md" },
    closest() { return this; }
  });
  const host = Object.assign(Object.setPrototypeOf(atlas.host, MapHost.prototype), {
    atlas,
    scale: .8,
    cards: new Map([["b.md", { container: target }]]),
    effects: { cancelRectangle() {}, focus(path) { focused.push(path); } },
    graph: { style: { setProperty(name, value) { strokes.push([name, value]); } } },
    root: { elementFromPoint() { return target; } },
    pointerPosition: { x: 12, y: 14 },
    updateEdges() { geometryUpdates++; },
    animateConnections() { connectionAnimations++; }
  });
  host.transform();
  host.connectionFrame = 99;
  target.classList.add("hovered");
  host.beginCameraGesture();
  assert.equal(host.cameraGesture, true);
  assert.equal(host.body.classList.contains("map-camera-moving"), true);
  assert.equal(target.classList.contains("hovered"), false);
  assert.deepEqual(cancelledFrames, [99]);
  assert.equal(host.connectionFrame, null);
  assert.equal(geometryUpdates, 1);
  assert.equal(timers.size, 1);
  host.scale = .5;
  host.transform();
  assert.equal(strokes.length, 2);
  host.beginCameraGesture();
  assert.equal(geometryUpdates, 1);
  assert.equal(timers.size, 1);

  const [timerId, settle] = timers.entries().next().value;
  timers.delete(timerId);
  settle();
  assert.equal(host.cameraGesture, false);
  assert.equal(host.body.classList.contains("map-camera-moving"), false);
  assert.equal(strokes.length, 4);
  assert.equal(target.classList.contains("hovered"), true);
  assert.deepEqual(focused, [null, "b.md"]);
  assert.equal(connectionAnimations, 1);
  assert.equal(timers.size, 0);

  host.beginCameraGesture();
  host.cancelCameraGesture();
  assert.equal(timers.size, 0);
  assert.equal(host.cameraGesture, false);
  assert.equal(connectionAnimations, 1);
  host.finishCameraGesture();
  assert.equal(connectionAnimations, 1);

  host.drag = { pointer: 7 };
  host.beginCameraGesture();
  host.finishCameraGesture();
  assert.equal(target.classList.contains("hovered"), false);
  assert.equal(connectionAnimations, 1);
  assert.equal(timers.size, 0);
});

test("suspending the map cancels camera restoration and flushes final coordinates without restoring hover", context => {
  let nextId = 0;
  const timers = new Map();
  const frames = new Map();
  context.mock.method(globalThis, "setTimeout", callback => {
    timers.set(++nextId, callback);
    return nextId;
  });
  context.mock.method(globalThis, "clearTimeout", id => timers.delete(id));
  context.mock.method(globalThis, "requestAnimationFrame", callback => {
    frames.set(++nextId, callback);
    return nextId;
  });
  context.mock.method(globalThis, "cancelAnimationFrame", id => frames.delete(id));
  const events = [];
  const releasedPointers = [];
  const atlas = atlasRenderer(events);
  const host = Object.assign(Object.setPrototypeOf(atlas.host, MapHost.prototype), {
    atlas, scale: .8, cards: new Map(),
    effects: { cancelRectangle() {}, cancelPulses() {}, focus() {} },
    graph: drawingElement(events),
    root: { elementFromPoint() { throw new Error("A suspended map must not restore pointer hover"); } },
    pointerPosition: { x: 12, y: 14 },
    updateEdges() {},
    setHoveredEdge() {},
    animateConnections() { throw new Error("A suspended map must not restart connections"); }
  });
  host.viewport.hasPointerCapture = pointer => pointer === 7;
  host.viewport.releasePointerCapture = pointer => releasedPointers.push(pointer);
  host.transform();
  host.beginCameraGesture();
  host.x = 170;
  host.scale = .5;
  host.scheduleTransform();
  host.cameraFrame = requestAnimationFrame(() => { throw new Error("Obsolete camera animation ran"); });
  host.drag = { pointer: 7 };
  host.setSuspended(true);
  assert.equal(host.suspended, true);
  assert.equal(host.body.inert, true);
  assert.equal(host.cameraGesture, false);
  assert.equal(host.cameraFrame, null);
  assert.equal(host.drag, null);
  assert.equal(timers.size, 0);
  assert.equal(frames.size, 0);
  assert.deepEqual(releasedPointers, [7]);
  assert.equal(host.world.style.transform, "translate(170px,0px) scale(0.5)");
  assert.equal(host.graph.style.getPropertyValue("--edge-hit-width"), "28px");
  assert.equal(host.graph.style.getPropertyValue("--edge-hover-width"), "6px");
  host.setSuspended(false);
  assert.equal(host.body.inert, false);
  assert.equal(host.suspended, false);
});

test("graph previews coalesce pointer bursts and flush final coordinates without a late redraw", context => {
  let nextId = 0;
  const queued = new Map();
  context.mock.method(globalThis, "requestAnimationFrame", callback => {
    queued.set(++nextId, callback);
    return nextId;
  });
  context.mock.method(globalThis, "cancelAnimationFrame", id => queued.delete(id));
  const drawn = [];
  const host = Object.assign(Object.create(MapHost.prototype), {
    position: 0,
    syncCards() { drawn.push(this.position); },
    updateBounds() {},
    updateEdges() {}
  });
  for (let position = 1; position <= 100; position++) {
    host.position = position;
    host.scheduleGeometryUpdate();
  }
  assert.equal(queued.size, 1);
  const [frameId, callback] = queued.entries().next().value;
  queued.delete(frameId);
  callback();
  assert.deepEqual(drawn, [100]);
  host.position = 101;
  host.scheduleGeometryUpdate();
  host.flushGeometryUpdate();
  assert.deepEqual(drawn, [100, 101]);
  assert.equal(queued.size, 0);
  host.scheduleGeometryUpdate();
  host.cancelGeometryUpdate();
  assert.equal(queued.size, 0);
  assert.deepEqual(drawn, [100, 101]);
});

test("stationary graph edges retain SVG paths and moved endpoints update visible and hit geometry together", () => {
  const written = [];
  const edge = {
    edge: { source: "a.md", target: "b.md" },
    path: { setAttribute(name, value) { written.push(["visible", name, value]); } },
    hitPath: { setAttribute(name, value) { written.push(["hit", name, value]); } },
    group: { style: {} }
  };
  const geometry = new Map([
    ["a.md", { x: 0, y: 0, width: 100, height: 80 }],
    ["b.md", { x: 300, y: 0, width: 100, height: 80 }]
  ]);
  const host = Object.assign(Object.create(MapHost.prototype), {
    metrics: {},
    edgePaths: [edge],
    cardGeometry() { return geometry; },
    atlas: { connectionGeometry(path, value) { return value; } },
    effects: { updateGeometry() {} }
  });
  host.updateEdges();
  host.updateEdges();
  assert.equal(written.length, 2);
  assert.equal(written[0][2], written[1][2]);
  geometry.get("b.md").x += 20;
  host.updateEdges();
  assert.equal(written.length, 4);
  assert.equal(written[2][2], written[3][2]);
  assert.notEqual(written[0][2], written[2][2]);
  geometry.delete("b.md");
  host.updateEdges();
  assert.equal(edge.group.style.display, "none");
});

test("source model eviction skips a visible model and still releases older unused documents", async context => {
  context.mock.method(globalThis, "matchMedia", () => ({ matches: false }));
  const persistent = sourceHost();
  const browsing = sourceHost();
  context.after(() => disposeSourceModels());
  await persistent.render({ workspaceId: "fixture", path: "visible.md", source: "# Visible", preferences: {} });
  for (let index = 0; index < 5; index++) {
    await browsing.render({ workspaceId: "fixture", path: `page-${index}.md`, source: `# Page ${index}`, preferences: {} });
  }
  assert.equal(models.length, 6);
  assert.equal(models[0].disposed, false);
  assert.equal(models.filter(model => !model.disposed).length, 3);
  assert.equal(models[1].disposed, true);
  assert.equal(models.at(-1).disposed, false);
  persistent.saveEditor();
  browsing.saveEditor();
});

test("replacing source releases detached fallback rows and editor subscriptions", () => {
  const released = [];
  const host = Object.assign(sourceHost(), {
    textarea: { value: "large old document" },
    lineElements: [{}],
    pane: {},
    editor: { dispose() { released.push("editor"); } },
    scrollSubscription: { dispose() { released.push("scroll"); } },
    selectionSubscription: { dispose() { released.push("selection"); } }
  });
  host.saveEditor();
  assert.deepEqual(released, ["editor", "scroll", "selection"]);
  for (const name of ["editor", "scrollSubscription", "selectionSubscription", "textarea", "lineElements", "pane"]) {
    assert.equal(host[name], null);
  }
});
