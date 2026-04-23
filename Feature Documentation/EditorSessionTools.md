# Editor tools (session reports and name rules)

## Session HTML reports

### Menu toggle — `GenerateSessionReports`

**`Assets/Editor/GenerateSessionReports.cs`**

- **Path:** `Edit > Generate Session Reports` (checkmark when enabled).
- **Storage:** `EditorPrefs` key **`Petropolis_GenerateSessionReports`** (default **true**).
- **Public API:** `GenerateSessionReports.IsEnabled`.

When **disabled**, `SessionTracker.TryWriteSessionReport` exits early in the Editor with a log that generation is off.

### Play Mode exit — `SessionReportPlayModeExit`

**`Assets/Editor/SessionReportPlayModeExit.cs`**

- Subscribes to **`EditorApplication.playModeStateChange`**.
- On **`ExitingPlayMode`**, if session reports are enabled, calls **`SessionTracker.WriteReportIfNeeded()`**.
- Rationale: Unity does not always invoke **`OnApplicationQuit`** or reliable **`OnDestroy` order** in the Editor, so this path ensures HTML output when you **stop** Play Mode.

### Runtime

In **builds**, `SessionTracker.ShouldGenerateReport()` returns **true** (no Editor toggle).

## Mandatory name selection

**`Assets/Editor/MandatoryNameSelectionMenu.cs`**

- **Path:** `Edit > Mandatory Name Selection` (checked = mandatory).
- **Storage:** `PlayerPrefs` key **`NameSelectionSettings.PrefsKey`** = `Petropolis_MandatoryNameSelection` (1 = on, 0 = off).
- Read at runtime by **`NameSelectionSettings.IsMandatory`**.

## Related

- [SessionReportsAndTracking.md](./SessionReportsAndTracking.md) — `ReportWriter` output.
- [PlayerPreferencesAndSettingsUI.md](./PlayerPreferencesAndSettingsUI.md) — name settings.

## File reference

- `Assets/Editor/GenerateSessionReports.cs`, `SessionReportPlayModeExit.cs`, `MandatoryNameSelectionMenu.cs`
