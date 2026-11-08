/* VBWR B
 * Project: MarkVibe | Creator: Umberto Giacobbi
 * Modified with AI: OpenAI Codex; isolate unsafe and ambiguous local picker names.
 * Human guidance: Umberto Giacobbi; harden the Linux browser workspace.
 * Copyright (c) 2026 Umberto Giacobbi | License: MIT (see LICENSE)
 * VBWR E */
import { isIgnored, isRelevant, limits, normalizePath, WorkspaceError } from "./path.js";

/** Stages local picker names before exposing files, so enumeration order never selects an alias. */
export class LocalEntrySelection {
  names = new Map();
  ambiguous = new Set();
  skippedNames = false;

  /** Registers one raw relative path. A null resource represents a directory, including an implicit picker prefix. */
  register(rawPath, resource = null) {
    let path;
    try {
      path = normalizePath(rawPath, true);
    } catch (error) {
      if (!(error instanceof WorkspaceError) || !["invalid-path", "outside-workspace"].includes(error.code)) {
        throw error;
      }
      this.skippedNames = true;
      return false;
    }
    if (isIgnored(path) || this.ambiguous.has(path)) {
      return false;
    }

    const previous = this.names.get(path);
    if (previous) {
      if (previous.rawPath === rawPath && previous.resource === null && resource === null) {
        return true;
      }
      this.ambiguous.add(path);
      this.skippedNames = true;
      return false;
    }
    if (this.names.size >= limits.visited) {
      throw new WorkspaceError("folder-too-large", "Choose a smaller documentation folder.");
    }
    this.names.set(path, { rawPath, resource });
    return true;
  }

  /** Registers directory prefixes supplied implicitly by a directory input before recording its leaf. */
  addImportedFile(rawPath, resource) {
    // A long leaf must still register its valid parents, which can collide with another tree.
    const prefix = rawPath.slice(0, limits.pathLength + 1);
    for (let slash = prefix.indexOf("/"); slash >= 0; slash = prefix.indexOf("/", slash + 1)) {
      if (!this.register(rawPath.slice(0, slash))) {
        return;
      }
    }
    this.register(rawPath, resource);
  }

  /** Returns only unambiguous relevant resources after all directory and filename spellings are known. */
  entries() {
    const entries = new Map();
    for (const [path, { resource }] of this.names) {
      if (resource === null || !isRelevant(path) || hasAmbiguousPrefix(path, this.ambiguous)) {
        continue;
      }
      if (entries.size >= limits.entries) {
        throw new WorkspaceError("too-many-files", "Choose a workspace with fewer than 4,096 relevant files.");
      }
      entries.set(path, resource);
    }
    return entries;
  }
}

/** Checks a canonical destination and each directory above it against known picker collisions. */
export function hasAmbiguousPrefix(path, ambiguous) {
  for (let slash = path.indexOf("/"); slash >= 0; slash = path.indexOf("/", slash + 1)) {
    if (ambiguous.has(path.slice(0, slash))) {
      return true;
    }
  }
  return ambiguous.has(path);
}
