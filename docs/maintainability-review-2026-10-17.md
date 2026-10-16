<!-- VBWR B
Project: MarkVibe
Repository: https://github.com/umbertotechnopreneur/MarkVibe
Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
VibeWare: Human intent, AI execution, and plenty of tokens
Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
Created with AI: OpenAI Codex; code review and examples; 2026-10-17
Human guidance: Umberto Giacobbi; simplify code, drop old compatibility and keep documentation short
Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
Copyright (c) 2026 Umberto Giacobbi
License: MIT (see LICENSE)
VBWR E -->

# Code cleanup — 17 October 2026

This review covers AI requests, saved settings and the document map. It is not a review of every file.

## What changed

| Before | After |
| --- | --- |
| Separate AI command names for API and CLI | One set of commands, with the chosen connection sent explicitly. Old names are removed. |
| Repeated web request and settings-saving code | Shared helpers handle each job. |
| Map file reading and map editing mixed together | [File reading](../src/MarkVibe.Core/MapLayout.cs) and [editing state](../src/MarkVibe.Core/MapLayoutSession.cs) have separate files. |
| Long conditions that checked many things at once | Named checks for position, size and appearance. |
| Dense expressions and missing explanations | Short steps, useful variable names and comments on types and methods. |
| Extra ID generator for old browsers | Use the modern UUID API. Browser hosting requires HTTPS or localhost. |

File limits, cancellation and safety checks stay in place. No user files are deleted.

## Two examples

AI requests used to have separate `openai.edit` and `codex.edit` commands. Both now use `ai.edit`, with the connection specified in the request. The Windows code lives in [DesktopWindow.Ai.cs](../src/MarkVibe.Windows/DesktopWindow.Ai.cs).

[BrowserServices](../src/MarkVibe/BrowserServices.cs) used to send, wait and decode in one long expression. It now shows each step:

```csharp
var requestJson = EncodeArguments(arguments);
var responseJson = await Runtime.CallAsync(operation, requestJson, cancellationToken);
return Wire.Decode<T>(responseJson);
```

The shared web request code also keeps a timeout distinct from a user pressing Stop.

## What can improve next

- Split [the large AI send method](../src/MarkVibe/MainPage.Ai.cs) into request handling and screen updates.
- Separate [CLI process handling](../src/MarkVibe.Windows/CodexCliClient.cs) from reading its replies.
- Share the remaining native web request code in [key validation](../src/MarkVibe.Windows/OpenAiKeySettings.cs) and [Markdown editing](../src/MarkVibe.Windows/MarkdownAiClient.cs), keeping their different time and size limits.

## What was checked

Client bundling and Debug x64 compilation passed: zero errors. Existing dependency warnings remain.

No tests, app interaction or live AI requests ran. The existing smoke check was updated but not run. No new package was installed: the installed 0.1.0.25 does not include this cleanup.

Only current commands and file formats are supported. The rule is recorded in [AGENTS.md](../AGENTS.md); no old aliases or data migrations were added.
