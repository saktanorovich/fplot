# Spec: Clipboard Copy & Paste

**Status:** Draft — Claude-authored from existing code, pending user review
**Slug:** `clipboard-copy-paste`

## Summary
The user copies all points to the clipboard in a spreadsheet-friendly format, and pastes points from the
clipboard (from a spreadsheet or CSV text) to replace the current data.

## Tech Stack
- Language/runtime: C# on .NET 10 (`net10.0`)
- UI: Avalonia 11.3.11 (`TopLevel.Clipboard`)
- MVVM: CommunityToolkit.Mvvm 8.4.0
- Tests: **xUnit** (proposed; confirm). Fake `IPointsIoService` for view-model tests.

## Acceptance Criteria
1. **Copy format.** Copy puts every point on the clipboard as one line per point, `x<TAB>y`, numbers
   formatted in the **current culture** (so it pastes into Excel/Numbers as two columns).
2. **Copy disabled when empty.** Copy is disabled when there are no points.
3. **Paste replaces points.** Paste replaces all current points with the clipboard's points and clears
   unsaved-change state (loaded points are the new baseline).
4. **Paste accepts both formats.** Each line is parsed as a tab-separated row (current culture, falling back
   to invariant) if it contains a tab, else as CSV `x,y` (invariant). Lines that do not parse are skipped.
   Line breaks `\n`, `\r`, `\r\n` are all accepted.
5. **Paste path label.** After a successful paste the path label is `<MachineName>/Clipboard`, so Save is
   disabled (not a file) until Save As.
6. **Empty paste is a no-op.** If the clipboard has no text or no parseable lines, points and path are unchanged.

## Test Approach
- **Unit:** `PointViewModel.ToTabular`/`TryParse` across `en-US` and a comma-decimal culture such as `de-DE`
  (criteria 1, 4); `MainViewModel` Copy/Paste with a fake `IPointsIoService` (2, 3, 5, 6).
- **Integration (headless):** `FileUtils.SetClipboardAsync` → `ReadLinesAsync(clipboard)` round-trip.

## Smoke Test
```bash
cd src && dotnet build FPlot.sln && dotnet run --project FPlot
```
Copy → paste into a spreadsheet gives two columns; copy two columns from a spreadsheet → Paste → grid and
plot show them, path label reads `<machine>/Clipboard`.

## Known gaps / questions for review
- Copy in a comma-decimal culture (e.g. `1,5<TAB>2,5`) is not re-parseable as CSV if the tab is lost — acceptable?
- Paste discards unsaved edits without confirmation — intended?

## Out of Scope
Copying only selected rows; pasting to append instead of replace; rich clipboard formats.
