# Browser capabilities

The original source/API baseline was assessed on 2026-10-10. Local runtime evidence was added on 2026-10-11: Edge 155.0.4283.45 passed 17 automated checks at root and 17 at a development subpath. Other browsers/devices have not been run. Version baselines below remain the original MDN assessment, not a guarantee for a device or policy.

| Browser/device | Connected user directory | Imported directory | Selected files / ZIP | Runtime status |
| --- | --- | --- | --- | --- |
| Desktop Edge 155.0.4283.45 | Real persisted browser directory handle tested; OS directory picker/permission dialogs remain unverified | Synthetic folder selection with relative paths passed | Synthetic selected files and ZIP passed | Automated development checks passed; narrow viewports are emulated |
| Desktop Chrome | Directory picker in a secure context; original baseline 86 | Relative directory paths available in source baseline | Implemented fallback, not run in Chrome | Unverified |
| Desktop Firefox | Directory picker unavailable in cited data | Directory input baseline 50 | Planned source path | Unverified |
| Desktop Safari | Directory picker unavailable in cited data | Directory input baseline 11.1 | Planned source path | Unverified |
| Chrome Android | Directory picker baseline 132; actual device/policy still matters | Directory input baseline 132 | Planned source path | Unverified |
| iPhone / iPad Safari | Connected picker unavailable in cited data | Directory input effective from 18.4; earlier property presence was insufficient | Planned source path | Unverified |
| Other mobile browsers / embedded webviews | Detect on device | Detect and verify relative paths | Fallback when file selection is usable | Unverified |

The UI tests isSecureContext and the existence of showDirectoryPicker. Folder import separately checks the directory-input capability and the actual webkitRelativePath values returned by a selection. It never grants a mode solely from a browser's name.

A system file picker can differ by device, installed file provider and policy. Select related files or a ZIP if an import does not expose relative paths. API presence and device-specific picker behavior must both be validated.

## Permission and persistence

A connected folder is opened in read mode. Directory enumeration and reads query permission. Startup resumes only when read permission is already granted; it never requests new permission. The explicit reopen action can request read permission. IndexedDB is best effort: a stored handle does not promise retained permission.

A denied, revoked or unavailable permission has a recovery path in source, with OS permission denial/revocation still pending real-device validation. Imported folders, files and ZIPs require reselection after restart, then restore their remembered page/position. Settings can disable and clear this remembered state. No automatic write permission is requested. The map's explicit **Save layout** button can request read/write access for `.markvibe-map.json`, after which map edits autosave; ink retains its download behavior. The automated handle check uses genuine synthetic OPFS files; OPFS availability does not grant access to user-selected directories. Map write permission and autosave have not yet been runtime-verified.

## Source view and mobile behavior

The reader's **Save notes** click can also request folder write access. Connected
notes autosave through contained nested sidecars with document and revision checks;
imported notes use downloads. Map and reader notes share the appearance picker and
editor, while narrow/coarse map mode retains positions in a list. These new routes
have static/source evidence only; permission dialogs and physical input remain unverified.

The Windows wrapper adds a native provider independently of browser picker support:
OS file/folder dialogs, automatic native reopening, sidecar writes and direct PDF
saving. Browser providers retain their existing read permissions and download ink
copies. Drawing mode accepts mouse/pen in paragraph bands; touch does not create ink.

Monaco is a progressive desktop enhancement. A coarse pointer, narrow source pane, limited reported memory or an editor failure selects the lightweight source view. The fallback retains select/copy, find, line numbers and source-position navigation.

Mobile reading uses single panes, full-width sidebar switching and touch-sized buttons. Split requires a wide viewport. Verify safe-area, virtual keyboard, orientation changes and native file selection on real iOS/iPadOS and Android devices.

## Primary references

- [Directory picker API requirements](https://developer.mozilla.org/en-US/docs/Web/API/Window/showDirectoryPicker)
- [Directory input and relative paths](https://developer.mozilla.org/en-US/docs/Web/API/HTMLInputElement/webkitdirectory)
- [Read permission querying](https://developer.mozilla.org/en-US/docs/Web/API/FileSystemHandle/queryPermission)
- [Window compatibility data](https://github.com/mdn/browser-compat-data/blob/main/api/Window.json)
- [HTMLInputElement compatibility data](https://github.com/mdn/browser-compat-data/blob/main/api/HTMLInputElement.json)
- [FileSystemHandle compatibility data](https://github.com/mdn/browser-compat-data/blob/main/api/FileSystemHandle.json)
