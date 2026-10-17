<!-- VBWR B

Project: MarkVibe
Repository: https://github.com/umbertotechnopreneur/MarkVibe
Creator: Umberto Giacobbi | https://umbertogiacobbi.biz

VibeWare: Human intent, AI execution, and plenty of tokens
Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto

Created with AI: OpenAI Codex; document the future Microsoft Store prerequisites; 2026-10-16
Human guidance: Umberto Giacobbi; local Debug MSIX now, Store identity unavailable
Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md

Copyright (c) 2026 Umberto Giacobbi
License: MIT (see LICENSE)

VBWR E -->

# Future Microsoft Store distribution

The current MSIX is a **local development package**, signed with the generated
MarkVibe development certificate. It can be installed locally after trusting that
certificate. The current package identity is not evidence of a reserved Microsoft
Store product, and no Store submission or publication has been performed.

Before preparing a Store package, reserve the product in Partner Center and copy
these exact values from its **Product identity** page:

- `Package/Identity/Name`
- `Package/Identity/Publisher`
- `Package/Properties/PublisherDisplayName`

These values are currently unavailable. Do not infer them from the local package,
the publisher's display name or another VibeWare product. Microsoft documents the
[three required manifest values](https://learn.microsoft.com/windows/apps/publish/view-app-identity-details).

The future Store route should use a separately requested optimized Release build,
those reserved identity values and a Store-compatible version. Store version
guidance reserves the fourth version component as zero and requires a nonzero
first component; the local `0.1.0.x` development sequence should not be reused as
a Store version. See Microsoft's [package requirements](https://learn.microsoft.com/windows/apps/publish/publish-your-app/msix/app-package-requirements).

Microsoft signs MSIX packages during Store publication, so this route does not
need a generated development certificate. An unsigned package alone does not
prove Store readiness: identity association, certification and listing information
still need to be completed. See [MSIX Store distribution](https://learn.microsoft.com/windows/apps/distribute-through-store/how-to-distribute-your-win32-app-through-microsoft-store).

No Store build, certification checks, account changes or upload are included in
local Debug delivery. Do not install tools, publish a listing or submit packages
without a separate request.
