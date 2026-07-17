# Làng Quê Tôi Audio, Compliance, QA, and Windows Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xác minh quyền sử dụng asset, tích hợp BGM theo thời gian vào AudioManager hiện hữu, tạo regression/build gates có kết quả cấu trúc và xuất bản Windows x86_64 IL2CPP đã smoke-test.

**Architecture:** Compliance được kiểm tra từ dependency thật của enabled scenes và ledger CSV parse đúng chuẩn. Audio mở rộng `AudioManager` thay vì tạo controller cạnh tranh; release build chỉ chạy khi preflight trả về `fatalCount == 0`.

**Tech Stack:** Unity 6000.4.6f1, UnityEditor BuildPipeline, IL2CPP, LZ4HC, NUnit/EditMode tests, AudioSource crossfade, CSV RFC 4180 subset.

## Global Constraints

- Chủ project đã xác nhận ngày 2026-07-17 rằng có giấy phép thương mại bổ sung cho UI Basic Pack, Sprites Basic Pack và Sprout Sorry Pack.
- Ghi xác nhận đó vào ledger; không sửa nội dung readme gốc.
- `Cozy Lofi Beat - Split memmories` dùng theo Pixabay Content License và giữ URL nguồn chính xác.
- File audio không có nguồn hoặc xác nhận của chủ project không được reference bởi enabled-scene dependency trong release.
- Không thêm WeatherManager; adaptive audio chỉ dùng `DayCycleManager.OnHourChanged`/`CurrentTimeOfDay`.
- Không thay PlayerPrefs audio keys hoặc public API `PlaySFX`, `StopSFXLoop`, `SetBGMVolume`, `SetSFXVolume`, `SetAmbienceVolume`.
- Windows x86_64, IL2CPP, LZ4HC, Development Build tắt; output ngoài `Assets`.
- Không cấu hình Steamworks SDK, App ID, achievement hoặc cloud save.

---

### Task 1: Commercial License Ledger and Dependency Gate

**Files:**
- Create: `Assets/LangQueToi/Compliance/ThirdPartyAssets.csv`
- Create: `Assets/LangQueToi/Compliance/OWNER_LICENSE_CONFIRMATION.md`
- Create: `Assets/StreamingAssets/CREDITS.txt`
- Create: `Assets/LangQueToi/Audio/BGM/cozy-morning-189311.mp3`
- Create: `Assets/LangQueToi/Runtime/LQTCsv.cs`
- Create: `Assets/LangQueToi/Runtime/ValidationResult.cs`
- Create: `Assets/Scripts/Editor/LQTComplianceValidator.cs`
- Create: `Assets/LangQueToi/Tests/EditMode/CsvLedgerTests.cs`
- Create on execution: `Artifacts/Compliance/dependencies.json`

**Interfaces:**
- Consumes: `EditorBuildSettings.scenes`, `AssetDatabase.GetDependencies`, owner license confirmation
- Produces: `LQTComplianceValidator.Validate()` returning `ValidationResult`; command-line entry fails on referenced `commercial_ok != Y`

- [ ] **Step 1: Record the owner confirmation verbatim and source licenses**

Create `OWNER_LICENSE_CONFIRMATION.md`:

```markdown
# Owner License Confirmation

On 2026-07-17, the project owner confirmed possession of commercial-use rights for:

- Sprout Lands UI Basic Pack
- Sprout Lands Sprites Basic Pack
- Sprout Sorry Pack

The confirmation was provided after the production audit identified these three package roots as the commercial-release blocker. This record applies only to those package roots; it does not automatically cover unrelated audio or other third-party assets.

This record documents the owner's instruction for the production pipeline. Original package readme files remain unchanged. The owner is responsible for retaining purchase receipts or license grants outside the public repository.
```

Create CSV header:

```csv
path,category,source_url,license,author,evidence,attribution_required,commercial_ok,notes
```

Rows under the three package roots use `license=Owner-held commercial license`, `evidence=OWNER_LICENSE_CONFIRMATION.md`, `commercial_ok=Y`, and author `Cup Nooble`. Add the Split Memories MP3 row with source `https://pixabay.com/music/beats-cozy-lofi-beat-split-memmories-248205/`, license `Pixabay Content License`, author `IdoBerg`, `commercial_ok=Y`.

Download the day track from `https://pixabay.com/music/acoustic-group-cozy-morning-189311/` to `Assets/LangQueToi/Audio/BGM/cozy-morning-189311.mp3` and add author `folk_acoustic_music`, license `Pixabay Content License`, `commercial_ok=Y`.

All remaining `.wav/.mp3/.ogg` files receive a row. Root project audio covered by owner documentation uses `evidence=OWNER_LICENSE_CONFIRMATION.md`; any asset not covered is `commercial_ok=N` and must be unreferenced before Task 4. `On the Farm.wav` is not used by the release plan unless separate evidence is added.

- [ ] **Step 2: Write CSV parser tests before implementation**

Tests must prove quoted commas and escaped quotes parse correctly:

```csharp
[Test]
public void CsvParser_ParsesQuotedCommaAndEscapedQuote()
{
    string row = "\"a,b.wav\",SFX,\"https://example.test/a\",\"Owner \"\"Grant\"\"\",Author,evidence,Y,Y,note";
    string[] values = LQTCsv.ParseRow(row).ToArray();
    Assert.That(values[0], Is.EqualTo("a,b.wav"));
    Assert.That(values[3], Is.EqualTo("Owner \"Grant\""));
    Assert.That(values.Length, Is.EqualTo(9));
}
```

- [ ] **Step 3: Implement the deterministic CSV parser**

Place `LQTCsv` in namespace `LangQueToi` and use this parser; do not use `Split(',')`:

```csharp
using System;
using System.Collections.Generic;

namespace LangQueToi
{
public static class LQTCsv
{
public static IEnumerable<string> ParseRow(string row)
{
    var value = new System.Text.StringBuilder();
    bool quoted = false;
    for (int i = 0; i < row.Length; i++)
    {
        char c = row[i];
        if (c == '"')
        {
            if (quoted && i + 1 < row.Length && row[i + 1] == '"')
            {
                value.Append('"');
                i++;
            }
            else quoted = !quoted;
        }
        else if (c == ',' && !quoted)
        {
            yield return value.ToString();
            value.Clear();
        }
        else value.Append(c);
    }
    if (quoted) throw new FormatException("Unclosed CSV quote.");
    yield return value.ToString();
}
}
}
```

- [ ] **Step 4: Create the shared structured validation result**

Place the following types in namespace `LangQueToi` with `using System;` and `using System.Collections.Generic;`:

```csharp
[Serializable]
public sealed class ValidationResult
{
    public bool passed = true;
    public int fatalCount;
    public int warningCount;
    public List<ValidationFinding> findings = new();

    public void Fatal(string code, string message)
    {
        fatalCount++;
        passed = false;
        findings.Add(new ValidationFinding("fatal", code, message));
    }

    public void Warning(string code, string message)
    {
        warningCount++;
        findings.Add(new ValidationFinding("warning", code, message));
    }

    public void Merge(ValidationResult other)
    {
        fatalCount += other.fatalCount;
        warningCount += other.warningCount;
        findings.AddRange(other.findings);
        passed = fatalCount == 0;
    }
}

[Serializable]
public sealed class ValidationFinding
{
    public ValidationFinding(string severity, string code, string message)
    { this.severity = severity; this.code = code; this.message = message; }
    public string severity;
    public string code;
    public string message;
}
```

- [ ] **Step 5: Validate actual build dependencies**

Collect every enabled scene path, call `AssetDatabase.GetDependencies(scenePaths, true)`, and require a matching ledger row for each dependency under `_SproutLandsAssets` and every audio extension. Fail when a referenced row has `commercial_ok` other than `Y`, the evidence path is missing, or attribution-required content is absent from `CREDITS.txt`.

The credits file includes:

```text
Sprout Lands assets — Cup Nooble — used under owner-held commercial license.
Cozy Lofi Beat - Split memmories — IdoBerg — Pixabay Content License.
Cozy Morning — folk_acoustic_music — Pixabay Content License.
Nunito — The Nunito Project Authors — SIL Open Font License 1.1.
```

- [ ] **Step 6: Run compliance tests/gate and commit**

Expected: every reachable third-party/audio dependency has `commercial_ok=Y`; unreferenced unknown files may remain in the repository but are reported as warnings.

```powershell
git add Assets/LangQueToi/Compliance Assets/StreamingAssets Assets/Scripts/Editor/LQTComplianceValidator.cs Assets/LangQueToi/Runtime Assets/LangQueToi/Tests Artifacts/Compliance
git commit -m "docs: add commercial asset compliance gate"
```

---

### Task 2: Time-of-Day Music Crossfade in Existing AudioManager

**Files:**
- Modify: `Assets/Scripts/Audio/AudioManager.cs`
- Modify through Unity serialization: `Assets/Scenes/MainScene.unity`
- Create: `Assets/LangQueToi/Runtime/MusicTimePolicy.cs`
- Create: `Assets/LangQueToi/Tests/EditMode/MusicTimePolicyTests.cs`
- Create: `Assets/Scripts/Editor/LQTAudioSceneSetup.cs`

**Interfaces:**
- Consumes: `DayCycleManager.OnHourChanged`, `CurrentHour`, day/evening AudioClips
- Produces: `MusicTimePolicy.ForHour(float)`, two-source BGM crossfade; preserves existing AudioManager public API

- [ ] **Step 1: Write time-policy tests**

```csharp
[TestCase(6f, MusicPeriod.Day)]
[TestCase(12f, MusicPeriod.Day)]
[TestCase(16.99f, MusicPeriod.Day)]
[TestCase(17f, MusicPeriod.Evening)]
[TestCase(23f, MusicPeriod.Evening)]
[TestCase(0f, MusicPeriod.Evening)]
[TestCase(5.99f, MusicPeriod.Evening)]
public void ForHour_SelectsExpectedPeriod(float hour, MusicPeriod expected)
    => Assert.That(MusicTimePolicy.ForHour(hour), Is.EqualTo(expected));
```

- [ ] **Step 2: Implement the pure policy**

```csharp
using UnityEngine;

namespace LangQueToi
{
    public enum MusicPeriod { Day, Evening }

    public static class MusicTimePolicy
    {
        public static MusicPeriod ForHour(float hour)
        {
            float wrapped = Mathf.Repeat(hour, 24f);
            return wrapped >= 17f || wrapped < 6f
                ? MusicPeriod.Evening
                : MusicPeriod.Day;
        }
    }
}
```

- [ ] **Step 3: Extend AudioManager with serialized secondary source/clips**

Add:

```csharp
[Header("Adaptive BGM")]
[SerializeField] private AudioSource bgmSecondarySource;
[SerializeField] private AudioClip dayBgm;
[SerializeField] private AudioClip eveningBgm;
[SerializeField, Min(0.1f)] private float musicCrossfadeDuration = 2f;

private bool _primaryBgmActive = true;
private Coroutine _musicCrossfadeRoutine;
```

Subscribe/unsubscribe `DayCycleManager.Instance.OnHourChanged`. On startup select from `CurrentHour`. Implement ping-pong crossfade using the current volume of both sources as fade origins, so an interrupted transition remains continuous. At completion stop/clear the outgoing source. Both targets equal clip volume × `_bgmMultiplier`.

Do not create a weather bridge. Keep scene-transition and sleep fade behavior by applying those fades to both BGM sources.

- [ ] **Step 4: Configure MainScene idempotently**

`LQTAudioSceneSetup` finds the existing AudioManager object by component, adds one secondary AudioSource only if missing, copies output mixer group/spatialBlend/loop from primary, and binds:

```text
dayBgm = Assets/LangQueToi/Audio/BGM/cozy-morning-189311.mp3
eveningBgm = Assets/_SproutLandsAssets/Audio/BGM/idoberg-cozy-lofi-beat-split-memmories-248205.mp3
```

Both clips must have ledger `commercial_ok=Y`; otherwise the setup throws and makes no scene write.

- [ ] **Step 5: Test compile, transitions and commit**

Run EditMode tests, then Play Mode at 16:55 until 17:05 and across sleep/wake. Verify no click, no doubled peak and slider volume applies during/after crossfade.

```powershell
git add Assets/Scripts/Audio/AudioManager.cs Assets/Scripts/Editor/LQTAudioSceneSetup.cs Assets/LangQueToi/Runtime Assets/LangQueToi/Tests Assets/Scenes/MainScene.unity
git commit -m "feat: add time-of-day music crossfade"
```

---

### Task 3: Structured D4 Regression Suite

**Files:**
- Modify: `Assets/LangQueToi/Runtime/ValidationResult.cs`
- Create: `Assets/Scripts/Editor/LQTRegressionSuiteD4.cs`
- Create on execution: `Artifacts/QA/d4-regression.json`
- Create on execution: `Artifacts/QA/editmode-results.xml`

**Interfaces:**
- Consumes: localization, visual, compliance validators
- Produces: `ValidationResult LQTRegressionSuiteD4.Run()` and `RunFromCommandLine()`

- [ ] **Step 1: Verify the shared structured result contract**

```csharp
var first = new ValidationResult();
first.Warning("W1", "warning");
var second = new ValidationResult();
second.Fatal("F1", "fatal");
first.Merge(second);
Assert.That(first.passed, Is.False);
Assert.That(first.fatalCount, Is.EqualTo(1));
Assert.That(first.warningCount, Is.EqualTo(1));
Assert.That(first.findings.Count, Is.EqualTo(2));
```

- [ ] **Step 2: Implement the ten exact checks**

Run and merge findings for:

1. Unity compilation status has no errors.
2. Missing scripts in enabled scenes and project-owned prefabs.
3. Duplicate/missing GUID and diff guard for existing `.meta` identity.
4. Enabled scenes exactly MenuScene then MainScene.
5. TMP fonts/sprites non-null and Vietnamese glyph coverage.
6. Localization catalog/data/static coverage.
7. Currency unit tests.
8. Save schema and baseline-save compatibility evidence.
9. Visual bindings/dimensions/dialogue API.
10. Compliance ledger for all reachable dependencies.

Do not mark compile passed merely because the method executes. Query `EditorUtility.scriptCompilationFailed` and fail when true.

- [ ] **Step 3: Run tests and D4 suite**

Run Unity EditMode tests, then `LQTRegressionSuiteD4.RunFromCommandLine` in a separate clean batch invocation.

Expected: `passed=true`, `fatalCount=0`. Warnings are individually listed and cannot include missing license, missing reference, missing glyph, save mismatch or English player-facing text.

- [ ] **Step 4: Commit regression tooling**

```powershell
git add Assets/Scripts/Editor Assets/LangQueToi/Tests Artifacts/QA
git commit -m "test: add structured D4 regression suite"
```

---

### Task 4: Gated Windows x86_64 IL2CPP Release Build

**Files:**
- Create: `Assets/Scripts/Editor/LQTWindowsReleaseBuild.cs`
- Create on execution: `Artifacts/Build/windows-release-report.json`
- Create on execution: `Artifacts/Build/windows-release-manifest.txt`
- Build output: `Builds/Windows/LangQueToi/`

**Interfaces:**
- Consumes: `LQTRegressionSuiteD4.Run()`
- Produces: `LQTWindowsReleaseBuild.BuildFromCommandLine()` and `LangQueToi.exe`

- [ ] **Step 1: Implement fail-closed preflight**

```csharp
ValidationResult validation = LQTRegressionSuiteD4.Run();
if (!validation.passed || validation.fatalCount != 0)
    throw new BuildFailedException(
        $"D4 preflight failed with {validation.fatalCount} fatal findings.");
```

Never continue from a `void` validation method.

- [ ] **Step 2: Configure exact build settings and build**

Set `BuildTarget.StandaloneWindows64`, `ScriptingImplementation.IL2CPP`, `Il2CppCompilerConfiguration.Release`, `BuildOptions.CompressWithLz4HC`, and no `Development`/`AllowDebugging`. Use only enabled scenes in current order. Output path is absolute and must resolve under `<repo>/Builds/Windows/LangQueToi`; otherwise throw before touching the filesystem.

- [ ] **Step 3: Serialize BuildReport and manifest**

JSON contains result, Unity version, total time, total size, errors, warnings, output and scenes. Manifest recursively lists relative output files and SHA-256 hashes. A successful report requires `BuildResult.Succeeded` and executable existence.

- [ ] **Step 4: Run the release build from command line**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe' `
  -batchmode -quit -projectPath (Get-Location).Path `
  -executeMethod LQTWindowsReleaseBuild.BuildFromCommandLine `
  -logFile (Join-Path (Get-Location).Path 'Artifacts\Logs\windows-release-build.log')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

Expected: `Builds/Windows/LangQueToi/LangQueToi.exe` exists and report result is `Succeeded`.

- [ ] **Step 5: Commit build tooling and reports, not binaries**

```powershell
git add Assets/Scripts/Editor/LQTWindowsReleaseBuild.cs Artifacts/Build
git commit -m "build: produce gated Windows IL2CPP release"
```

The ignored `Builds/` directory remains outside Git.

---

### Task 5: Standalone Smoke Test and Production Report

**Files:**
- Create: `docs/qa/2026-07-17-windows-smoke-test.md`
- Create: `Assets/Scripts/Editor/LQTProductionReport.cs`
- Create on execution: `Artifacts/Production/final-production-report.json`

**Interfaces:**
- Consumes: release executable, D3/D4 reports, compliance ledger, Steam validator from the next plan
- Produces: explicit pass/fail evidence; never infers status by substring search

- [ ] **Step 1: Execute the standalone matrix**

Launch `LangQueToi.exe` at 1920×1080 and record each item as pass/fail with screenshot/evidence path:

1. Menu, title, background, Nunito.
2. New Game.
3. Load baseline save.
4. Farming and harvest.
5. Fishing cast/bite/reel/catch.
6. Inventory full notification.
7. Bà Năm dialogue, portrait, greeting, buy/sell.
8. Three icons via `WeatherIconUI.SetWeather` test harness; no weather simulation claim.
9. Sleep, autosave, exit, relaunch, load.
10. Audio sliders, 17:00 crossfade and sleep fade.

Run for at least five continuous minutes after the loop and record observed console/player log errors.

- [ ] **Step 2: Generate the production report from structured inputs**

`LQTProductionReport` deserializes JSON reports and requires their boolean `passed` fields. It also requires build executable/manifest, smoke-test checklist with every row `PASS`, compliance fatal count zero and Steam asset validation pass. It must not search for strings such as `6/6`, `PASS: 10` or `Succeeded` inside arbitrary text.

- [ ] **Step 3: Run, review and commit**

Expected before the Steam plan: production report is false only for the explicit `steam-assets` check; all game/build checks pass.

```powershell
git add docs/qa Assets/Scripts/Editor/LQTProductionReport.cs Artifacts/Production
git commit -m "test: document standalone Windows release smoke test"
git tag v0.8-windows-rc
```
