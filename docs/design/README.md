# MarkVibe visual assets

The chosen sidebar is [proposal 2: segmented tabs](sidebar/02-segmented-tabs.png).
The four proposals are design references, not screenshots of a running build.

Thirteen standalone transparent app-bar icons and the About hero were created
with the built-in `image_gen.imagegen` tool. Exact prompts and reference roles
are recorded in [imagegen-prompts.json](imagegen-prompts.json).

Production assets live in `src/MarkVibe.Browser/wwwroot/icons/appbar/` and
`src/MarkVibe.Browser/wwwroot/images/`. The original VibeWare logo is supplied
by its creator, Umberto Giacobbi, from the VibeWare branding assets.

Keep accessible labels and localized tooltips when displaying icon-only actions.
Theme colors and dialog templates belong in the app's shared XAML dictionaries;
document typography is provided by the scoped reader CSS themes.
