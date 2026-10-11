# Engineering Lessons

Keep durable, repository-specific findings here; omit temporary commands and plans.

- A successful build does not prove startup, visible UI, native dialogs or hardware
  behavior. Record which layer was actually exercised; cold-launch desktop changes.
- Keep typed, bounded contracts across C#, JavaScript and native IPC. Check the
  sender origin and enforce file containment in the native service, beyond the UI.
- Persist and export through temporary files and atomic replacement. Preserve
  unknown formats and externally changed files; never turn a conflict into success.
- Cancel and dispose obsolete readers, workers, image leases and export views.
  OpenSilver DOM hosts must attach to a connected element before rendering.
- Use synthetic documents and isolated profiles for validation; private files,
  credentials and business notes do not belong in fixtures, screenshots or logs.
- Suggest checks relevant to the final change, but run tests only on a current,
  explicit user request. A previous validation approval is not continuing permission.
- When Markdown changes, repeated quotes cannot identify the original block safely.
  Require a unique quote and explicit review; keep ambiguous annotations unresolved.
- OpenSilver applies viewport layout asynchronously. Wait for actual bounds before
  asserting responsiveness; a completed browser resize does not imply managed layout completion.
- Queue completed ink strokes while native autosave is pending. Navigation/export
  must await the event queue; disabling drawing during each write loses fast strokes.
- Clean repository `artifacts/` before each requested Release/Debug distribution
  build, keep temporary packaging files there and remove them after packaging.
  Never create build/test output in Codex directories.
- Ask before each branch/worktree creation. Package creation, installation and
  publication are separate actions; record only the evidence actually obtained.
