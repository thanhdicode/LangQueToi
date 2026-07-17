# Làng Quê Tôi Steam Assets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tạo bộ graphical assets Steam đúng kích thước hiện hành từ key art Làng Quê Tôi và năm screenshot gameplay thật, rồi đóng production report mà không tích hợp Steamworks SDK.

**Architecture:** Layered/source artwork và export marketing nằm ngoài `Assets` để không tăng player build. Mỗi tỉ lệ có composition riêng; validator Pillow chỉ kiểm tra kích thước, mode và alpha, còn content rules được review bằng contact sheet.

**Tech Stack:** imagegen skill, Pillow, PNG/JPG sRGB, Steamworks current templates, Windows release build.

## Global Constraints

- Thực hiện sau tag `v0.8-windows-rc` và standalone smoke pass.
- Capsule chỉ có artwork, tên `Làng Quê Tôi` và subtitle chính thức nếu được dùng.
- Không review score, giải thưởng, giá, giảm giá, CTA hoặc marketing copy trên base capsules.
- Library Hero 3840×1240 không có bất kỳ chữ nào.
- Screenshot là gameplay thật từ standalone build, tối thiểu 1920×1080, 16:9; không dùng concept/mockup.
- Không kéo giãn title mark 500×500 thành library logo; tạo logo ngang riêng cùng nhận diện.
- Mỗi tỉ lệ có bố cục riêng từ cùng key-art family.
- Không upload Steam, không cấu hình App ID/SDK vì chưa có authority/account data.

---

### Task 1: Steam Key-Art Master and Logo

**Files:**
- Create: `Marketing/Steam/Source/key-art-master.png` (3840×2160)
- Create: `Marketing/Steam/Source/key-art-vertical.png` (1800×2400)
- Create: `Marketing/Steam/Source/logo-horizontal.png` (1280×720 transparent canvas)
- Create: `Marketing/Steam/Source/source-ledger.csv`

**Interfaces:**
- Consumes: approved menu art/palette and actual farming/fishing visual language
- Produces: horizontal/vertical master art plus transparent logo source

- [ ] **Step 1: Generate the horizontal master with imagegen**

Use this exact prompt:

```text
AAA-indie production pixel-art key art for the Vietnamese cozy farming game “Làng Quê Tôi”, 16:9 master composition at golden sunrise in the Mekong Delta. A small Vietnamese farmer in indigo áo bà ba stands between a vegetable field and rice paddy; left background has a modest wooden house, watering can and hoe; right background has a calm canal, sampan and fishing rod; coconut palms frame the scene without covering the center. Warm, inviting, readable silhouettes, original art, crisp hand-placed pixel clusters, no HUD, no UI, no text, no watermark. Use the established palette: #163E16 #2D6423 #4E9440 #482C12 #8C5A28 #B9823E #DAA520 #FFF5C3 #64AAE6 #AFE09B #205220 #3A6E8C #7AB8D4 #2A2858 #D2B96E #12230A. Leave controlled negative space for a logo in the upper-left/center depending on crop.
```

Edit the chosen output until it matches in-game proportions and does not depict mechanics absent from the build.

- [ ] **Step 2: Generate the vertical master as a re-composition**

Use the horizontal master as visual reference and request a portrait composition with the farmer/field/canal retained inside the central safe region. Do not simply crop the horizontal master. Export exactly 1800×2400.

- [ ] **Step 3: Generate a horizontal transparent logo**

Prompt:

```text
Transparent horizontal pixel-art logotype reading exactly “Làng Quê Tôi”, consistent with the approved 500×500 in-game title mark but composed for a wide Steam library logo. Bamboo-green lettering, muted-gold lacquer outline, small rice-ear and lotus details, cream highlight, dark shadow, no subtitle, no additional words, no background, no watermark. Remain legible at 462×174.
```

Place it on a transparent 1280×720 canvas without stretching the square in-game mark.

- [ ] **Step 4: Record provenance and commit sources**

Create CSV header `path,origin,tool_or_author,created_date,license,commercial_ok,notes`; record all three as original generated-and-edited project output.

```powershell
git add Marketing/Steam/Source
git commit -m "art: add Steam key art and logo sources"
```

---

### Task 2: Capture Five Real Gameplay Screenshots

**Files:**
- Create: `Marketing/Steam/Screenshots/steam_screenshot_01_farming.png`
- Create: `Marketing/Steam/Screenshots/steam_screenshot_02_fishing.png`
- Create: `Marketing/Steam/Screenshots/steam_screenshot_03_ba_nam_shop.png`
- Create: `Marketing/Steam/Screenshots/steam_screenshot_04_inventory_hud.png`
- Create: `Marketing/Steam/Screenshots/steam_screenshot_05_world.png`
- Modify: `docs/qa/2026-07-17-windows-smoke-test.md`

**Interfaces:**
- Consumes: final `LangQueToi.exe`
- Produces: five 1920×1080 screenshots representing actual shipped gameplay

- [ ] **Step 1: Launch the release build at fixed resolution**

```powershell
Start-Process -FilePath (Resolve-Path 'Builds\Windows\LangQueToi\LangQueToi.exe') `
  -ArgumentList '-screen-width','1920','-screen-height','1080','-screen-fullscreen','0' `
  -WorkingDirectory (Resolve-Path 'Builds\Windows\LangQueToi')
```

- [ ] **Step 2: Capture the exact five states with an OS screenshot tool**

Capture only the game client area at native 1920×1080:

1. Active crop field with player watering/harvesting.
2. Active fishing state with bobber/fish feedback.
3. Bà Năm dialogue/shop with portrait and Vietnamese UI.
4. Inventory open with đồng balance and HUD visible.
5. Clear world view showing farm, canal and environmental art.

Do not composite, repaint, remove HUD selectively or place concept art behind gameplay.

- [ ] **Step 3: Verify screenshots and update smoke evidence**

Use Pillow to assert each image is exactly 1920×1080 and RGB/RGBA. Link each absolute/relative screenshot path to the corresponding smoke-test row.

- [ ] **Step 4: Commit screenshots**

```powershell
git add Marketing/Steam/Screenshots docs/qa/2026-07-17-windows-smoke-test.md
git commit -m "art: capture final Steam gameplay screenshots"
```

---

### Task 3: Compose Every Required Steam Export

**Files:**
- Create: `Marketing/Steam/Export/steam_header_capsule.png` (920×430)
- Create: `Marketing/Steam/Export/steam_small_capsule.png` (462×174)
- Create: `Marketing/Steam/Export/steam_main_capsule.png` (1232×706)
- Create: `Marketing/Steam/Export/steam_vertical_capsule.png` (748×896)
- Create: `Marketing/Steam/Export/steam_library_capsule.png` (600×900)
- Create: `Marketing/Steam/Export/steam_library_header.png` (920×430)
- Create: `Marketing/Steam/Export/steam_library_hero.png` (3840×1240)
- Create: `Marketing/Steam/Export/steam_library_logo.png` (1280×720 transparent)
- Create: `Marketing/Steam/Export/steam_shortcut_icon.png` (512×512)
- Create: `Marketing/Steam/Export/steam_app_icon.jpg` (184×184)
- Create: `Tools/Steam/validate_steam_assets.py`
- Create: `Marketing/Steam/steam-assets-contact-sheet.png`

**Interfaces:**
- Consumes: horizontal/vertical master, horizontal logo and screenshots
- Produces: complete Steam graphical asset package and validator exit code

- [ ] **Step 1: Compose each capsule individually**

Use the wide master for header/main/library header, with logo placement recomposed per safe area. Use the vertical master for vertical/library capsules. The small capsule uses a simplified close crop and logo occupying most of the width. Keep critical faces/objects away from edges.

Library Hero uses artwork only. Library Logo uses only transparent logotype. Shortcut/App icon use the rice-ear/lotus logomark with no tiny text.

- [ ] **Step 2: Implement exact automated validation**

Create `Tools/Steam/validate_steam_assets.py`:

```python
from pathlib import Path
from PIL import Image
import json
import sys

ROOT = Path(__file__).resolve().parents[2]
EXPORT = ROOT / "Marketing" / "Steam" / "Export"
SHOTS = ROOT / "Marketing" / "Steam" / "Screenshots"

EXPECTED = {
    "steam_header_capsule.png": (920, 430),
    "steam_small_capsule.png": (462, 174),
    "steam_main_capsule.png": (1232, 706),
    "steam_vertical_capsule.png": (748, 896),
    "steam_library_capsule.png": (600, 900),
    "steam_library_header.png": (920, 430),
    "steam_library_hero.png": (3840, 1240),
    "steam_library_logo.png": (1280, 720),
    "steam_shortcut_icon.png": (512, 512),
    "steam_app_icon.jpg": (184, 184),
}

findings = []
for name, size in EXPECTED.items():
    path = EXPORT / name
    if not path.exists():
        findings.append({"severity": "fatal", "path": str(path), "message": "missing"})
        continue
    with Image.open(path) as image:
        if image.size != size:
            findings.append({"severity": "fatal", "path": str(path), "message": f"size {image.size} != {size}"})
        if name == "steam_library_logo.png" and "A" not in image.mode:
            findings.append({"severity": "fatal", "path": str(path), "message": "logo lacks alpha"})
        if name == "steam_app_icon.jpg" and image.format != "JPEG":
            findings.append({"severity": "fatal", "path": str(path), "message": "app icon is not JPEG"})

shots = sorted(SHOTS.glob("steam_screenshot_*.png"))
if len(shots) != 5:
    findings.append({"severity": "fatal", "path": str(SHOTS), "message": f"expected 5 screenshots, found {len(shots)}"})
for path in shots:
    with Image.open(path) as image:
        width, height = image.size
        if width < 1920 or height < 1080 or width * 9 != height * 16:
            findings.append({"severity": "fatal", "path": str(path), "message": f"invalid screenshot size {image.size}"})

report = {
    "passed": not any(item["severity"] == "fatal" for item in findings),
    "fatalCount": sum(item["severity"] == "fatal" for item in findings),
    "findings": findings,
}
report_path = ROOT / "Artifacts" / "Steam" / "steam-validation.json"
report_path.parent.mkdir(parents=True, exist_ok=True)
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(report, ensure_ascii=False, indent=2))
sys.exit(0 if report["passed"] else 1)
```

- [ ] **Step 3: Generate and review a contact sheet**

Create a labeled contact sheet showing every export at consistent preview height. Confirm manually:

- title legible in small capsule;
- no forbidden text;
- hero has zero text;
- no stretched pixels;
- character face and important gameplay motifs remain inside safe areas;
- capsules accurately represent actual gameplay.

- [ ] **Step 4: Run validator and commit exports**

```powershell
python Tools\Steam\validate_steam_assets.py
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
git add Marketing/Steam/Export Marketing/Steam/steam-assets-contact-sheet.png Tools/Steam Artifacts/Steam
git commit -m "art: export validated Steam graphical assets"
```

---

### Task 4: Close the Final Production Report

**Files:**
- Modify: `Artifacts/Production/final-production-report.json`
- Create: `docs/release/2026-07-17-lang-que-toi-release-notes.md`
- Create: `docs/release/2026-07-17-steam-assets-checklist.md`

**Interfaces:**
- Consumes: localization, visual, compliance, D4, build, smoke and Steam structured reports
- Produces: final five-condition production result and artifact index

- [ ] **Step 1: Re-run every gate from a clean tree**

Run EditMode tests, localization validator, visual validator, compliance validator, D4 suite, Windows build, standalone smoke checklist and Steam validator in that order. Store logs under `Artifacts/Logs`.

Expected: every automated result has `passed=true` and `fatalCount=0`; manual smoke rows are all `PASS`.

- [ ] **Step 2: Generate the final production report**

Require these five booleans:

```text
localizationPassed
visualAndGameplayRegressionPassed
windowsBuildAndSmokePassed
commercialLicensingPassed
steamAssetsPassed
```

`productionReady` is their logical AND. Include commit hash, Unity version, build manifest hash, artifact paths and every remaining warning.

- [ ] **Step 3: Write release notes and Steam checklist**

Release notes state the Vietnamese re-theme, preserved gameplay/save compatibility, Bà Năm UI, audio changes, supported Windows target and explicit absence of Steamworks SDK integration. Checklist links each required graphical file and screenshot.

- [ ] **Step 4: Commit and tag the completed release candidate**

```powershell
git add Artifacts/Production docs/release
git commit -m "release: complete Làng Quê Tôi production candidate"
git tag v1.0.0-lqt-rc1
```
