# Reader and code themes

The shared catalog is `src/MarkVibe.Core/Presentation/themes.json`. The shell consumes its IDs and labels; the browser consumes the same file. Document palettes and typography are independent of the OpenSilver application theme and of Monaco's editor theme.

Preferences persist four IDs independently: `LightDocumentTheme` and `DarkDocumentTheme` default to `markvibe`; `LightCodeTheme` and `DarkCodeTheme` default to `auto`. JSON uses camelCase. An unknown ID or an ID unsupported in the current mode falls back to the corresponding default. Automatic code styling follows the selected document theme; with MarkVibe it keeps the existing highlight colors.

The catalog includes eight light and seven dark preview themes, alongside MarkVibe. It includes the ten light and thirteen dark code choices from Markdown Preview Enhanced; Default is available in both modes, so there are 22 distinct imported code assets. Technical, Editorial and Compact remain the original MarkVibe document styles. Imported themes retain their own font families, weights, heading typography, spacing and colors, while the application controls the reading viewport and responsive content width.

## Sources and offline assets

The CSS snapshot came from the user's installed Markdown Preview Enhanced 0.8.40, `crossnote/styles/preview_theme` and `crossnote/styles/prism_theme`. Assets live under `client/themes` as `.css.txt`, allowing esbuild's text loader to include them in the local application bundle. This suffix is intentional: these files must never be linked as application-wide CSS.

Google Fonts imports were removed from the preview copies. The adapter also removes all imports and external resource URLs before parsing any theme. Embedded PNG/JPEG/GIF/WebP textures are allowed. No fonts are downloaded or bundled. Typography uses the original local/system font stacks, including a serif stack for Newsprint and distinct heading families in Night. If Raleway, Source Sans Pro, PT Serif or another named font is absent, the operating system chooses the next available family. Export includes the same font stacks but does not embed font files; appearance can therefore differ on another computer.

The source snapshot and its modifications are covered in `THIRD-PARTY-NOTICES.txt`. Upstream provenance is [Markdown Preview Enhanced](https://github.com/shd101wyy/markdown-preview-enhanced), [Crossnote](https://github.com/shd101wyy/crossnote), [Prism](https://github.com/PrismJS/prism), [Prism themes](https://github.com/PrismJS/prism-themes), [Docsify](https://github.com/docsifyjs/docsify) and [Pen Paper Coffee](https://github.com/nylki/pen-paper-coffee-syntax). Crossnote credits [Typora default themes](https://github.com/typora/typora-default-themes) for Gothic, Newsprint and Night. These credits describe the upstream lineage; the bundled CSS is the Crossnote adaptation distributed with Markdown Preview Enhanced.

## DOM and export adapter

`resolveReaderTheme(preferences, mode)` returns the normalized mode, document ID, effective code ID, document palette (or null for MarkVibe), and compiled CSS. `applyReaderTheme(shadowRoot, article, theme)` updates a style element in the reader ShadowRoot and decorates its article. It rejects ordinary document roots. `decorateThemeArticle(article, theme)` applies the same article classes and code aliases to a standalone export document; append `theme.css` after the base and export layout CSS, and use its palette for the export page background and foreground.

The adapter parses trusted bundled CSS in a detached constructable stylesheet. Every ordinary selector is scoped to the Markdown article. It translates `html`, `body` and `.markdown-preview`, adapts direct-child selectors to Markdig's source sections, preserves conditional rules, and drops site navigation, font faces, keyframes and global at-rules. Reader surface variables are scoped to `.reader`. The selected document palette can also paint the opaque XAML reader surface; it must not replace application chrome tokens or recolor stored ink strokes.

Code parsing remains highlight.js. Presentation aliases map its token classes to Prism's theme vocabulary; `decorateCodeTokens(article)` must run after highlighting (the article decorator also calls it). This preserves the existing language support and sanitizer. Token grammars differ, so it is an adaptation of each theme rather than identical Prism output. Mermaid source blocks are excluded. MarkVibe handles diagram rendering separately.

## XAML resource organization

Right-click a document in Read or Source to change the same four theme choices
available in Settings. Context choices save for the opened folder and stay
independent for light and dark appearance. Settings supplies global defaults;
“Use global reading defaults” removes a folder's override. “Underline hyperlinks”
can suppress link underlines across document themes and exports.

The context menu also offers the theme's original font or an installed monospace
family, with font previews. The override covers document text, headings, tables
and code; Mermaid retains its diagram typography. No fonts are downloaded.
On Windows, folder choices use the stable identity of the opened root, so another
folder keeps its own choices after restarting. Storage stays in the local app
profile and does not write files into the documentation folder. Browser choices
follow the workspace identity; reopening a retained folder keeps that identity.

Keep `AppStyles.xaml` as the single application merger. Load `ThemeResources.xaml` before styles that consume its brushes and dimensions, then `Buttons.xaml`, `Navigation.xaml` and `Dialogs.xaml`. Retain stable keyed resources at this boundary. Resource dictionaries must resolve shared resources from the application scope, and must not duplicate the merger recursively. Keep any `BasedOn` dependency earlier in the lookup chain. Use explicit application/component resource URIs supported by the existing OpenSilver build.

Map light/dark application palettes to semantic brush objects such as Surface, Text, Muted, Border and Accent. Update the existing brush values when the active palette changes so controls holding static brush references repaint consistently. Use the supported system accent brush for active controls and a shared elevation-16 DropShadowEffect for elevated surfaces. The reader's imported background/foreground are a separate document surface palette. CSS selectors and font rules remain in the DOM; they are not converted to XAML control styles.

## Validation status

The parent UI integration completed a client bundle and solution build using existing .NET SDK 10.0.401 and Node 24.21.0, with zero warnings and errors. These are compilation results; no tests or app/browser launch were performed. Settings use two columns by default and reflow below a 680-pixel content width, with compact action grids and shared wrapping icon captions.

Suggested later acceptance covers all preview/code combinations, rapid light/dark switching, persisted independent selections, fonts with unavailable families, source/ink anchors, and standalone HTML/PDF export with network disabled.
