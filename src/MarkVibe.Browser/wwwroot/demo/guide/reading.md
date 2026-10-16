# Find your reading rhythm

Choose a document presentation that suits the material. Technical documentation keeps a clear hierarchy, editorial reading gives prose more room, and compact reading makes long reference pages easier to scan.

## Finding your place

Headings appear in the outline. Following a section records the document and anchor in browser history. Back and Forward return to a source block and reading position.

The Source view is read-only. On small screens a lightweight source view offers text selection, copying, line numbers and search. A wide screen can show source and rendered content together.

## Small examples

### Code

~~~csharp
public sealed record Page(string Path, string Title);

var page = new Page("guide/setup.md", "Prepare a workspace");
Console.WriteLine(page.Title);
~~~

### A sequence

~~~mermaid
sequenceDiagram
    participant Reader
    participant Workspace
    Reader->>Workspace: Choose a document
    Workspace-->>Reader: Markdown source
    Reader->>Reader: Render and explore
~~~

### Reading checklist

- [x] Open a page
- [x] Follow a [setup link](setup.md#installation)
- [x] Look at incoming and outgoing links in Map
- [ ] Try another collection

## Café notes

Unicode headings are preserved in their generated anchors. Try [this section again](#caf%C3%A9-notes).

## A diagram with an error

The following diagram is intentionally invalid. A diagram error should keep the rest of the document readable and expose its source.

~~~mermaid
flowchart LR
    A[Incomplete
~~~

## Continue exploring

Return to [Field notes](../README.md) or read [the browser capability notes](../reference/browser-notes.md).
