# Core parser regression tests

Run from the repository root with the .NET 9 SDK:

```sh
dotnet test tests/MarkdownViewer.Core.Tests/MarkdownViewer.Core.Tests.csproj --configuration Release
```

This standalone xUnit project references the real core library and exercises both
`ParseTextAsync` and `ParseStreamAsync`. It does not require a windowing system,
fonts, or the demo application's browser workload, so the same command works in CI.

Coverage includes four-space and tab indentation, CRLF input, extra/trailing
whitespace, Unicode, a reduced reproduction of issue #10, mixed paragraphs/lists,
fenced code with and without a language, and math-block dispatch.

Empty fenced blocks and the stream splitter's handling of blank lines in indented
or tilde-fenced blocks are existing limitations outside this fix. The suite does
not encode those limitations as required behavior or claim they are fixed.
