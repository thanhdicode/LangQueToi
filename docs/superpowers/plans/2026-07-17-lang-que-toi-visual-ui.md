# Làng Quê Tôi Visual Identity and UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay nhận diện menu/UI có mục tiêu bằng art miền Tây Nam Bộ, thêm Nunito tiếng Việt và tích hợp Bà Năm vào dialogue 960×256 mà không phá API hoặc animation hiện hữu.

**Architecture:** Asset mới nằm dưới `Assets/LangQueToi`, không ghi đè asset nguồn. Một Editor setup tool cấu hình import và rebind reference bằng hierarchy path; runtime dialogue nhận một presentation value object nhưng giữ nguyên `Open()` và `PlayDialogue(string)`.

**Tech Stack:** Unity 6000.4.6f1, UGUI, TextMesh Pro, imagegen skill, PNG pixel art, UnityEditor TextureImporter/SerializedObject.

## Global Constraints

- Thực hiện sau plan foundation/localization và tag `v0.3-localization`.
- Title mark trong game là PNG trong suốt 500×500; rect hiện hữu 600×600 giữ nguyên.
- Dialogue frame là 960×256, 9-slice lacquer/gold.
- Weather chỉ có ba icon `Sunny`, `Cloudy`, `Rainy`; không tạo `WeatherManager`.
- Palette dùng đúng token/hex trong design spec; red lantern `#C41E1C` chỉ dùng cho đèn lồng.
- Font là Nunito Regular/Bold, đủ glyph tiếng Việt và `₫`.
- Giữ GameObject name, animator, GUID và asset gốc; asset mới có GUID mới rồi rebind.
- Không dùng Pillow để vẽ primary art; chỉ dùng cho resize, crop, composition và dimension validation.
- Asset từ Basic/Sprout Sorry không được coi là commercial-safe nếu chưa có license bổ sung.

---

### Task 1: Generate and Curate the Production Art Set

**Files:**
- Create: `Assets/LangQueToi/Art/Menu/menu-background.png` (1920×1080)
- Create: `Assets/LangQueToi/Art/Menu/title-mark.png` (500×500 transparent)
- Create: `Assets/LangQueToi/Art/Menu/menu-character.png` (400×400 transparent)
- Create: `Assets/LangQueToi/Art/Dialogue/dialogue-frame.png` (960×256 transparent)
- Create: `Assets/LangQueToi/Art/Characters/ba-nam-portrait.png` (256×256 transparent)
- Create: `Assets/LangQueToi/Art/HUD/coin-dong.png` (64×64 transparent)
- Create: `Assets/LangQueToi/Art/HUD/weather-sunny.png` (90×90 transparent)
- Create: `Assets/LangQueToi/Art/HUD/weather-cloudy.png` (90×90 transparent)
- Create: `Assets/LangQueToi/Art/HUD/weather-rainy.png` (90×90 transparent)
- Create: `Assets/LangQueToi/Art/HUD/notification-frame.png` (640×128 transparent)
- Create: `Assets/LangQueToi/Art/Shop/ba-nam-shop-header.png` (512×128 transparent)
- Create: `Assets/LangQueToi/Art/art-source-ledger.csv`

**Interfaces:**
- Consumes: approved palette and visual direction
- Produces: eleven individually composed, production-quality raster assets with provenance rows

- [ ] **Step 1: Use the imagegen skill for the menu key art**

Generate with this prompt, then select the strongest composition and edit it rather than regenerating unrelated variants:

```text
Production pixel-art key art for a cozy Vietnamese farming game titled “Làng Quê Tôi”. Wide 16:9 sunrise over the Mekong Delta: rice paddies and vegetable beds, a calm narrow canal with a small wooden sampan, coconut palms, modest wooden riverside houses, warm golden haze, restrained red lantern accents only. Hand-placed high-quality pixel art, crisp clusters, readable silhouettes, no blur, no gradients pretending to be pixels, no UI, no text, no watermark. Palette anchors: bamboo #163E16 #2D6423 #4E9440, aged wood #482C12 #8C5A28 #B9823E, canal #3A6E8C #7AB8D4, sky #64AAE6 #AFE09B, cream #FFF5C3, muted gold #DAA520, shadow #12230A. Keep central and upper-left negative space usable for a title mark.
```

Export the final composition to exactly 1920×1080 using nearest-neighbor only after pixel cleanup.

- [ ] **Step 2: Generate title mark and menu character separately**

Title prompt:

```text
Square 500×500 transparent pixel-art logo mark for “Làng Quê Tôi”. Vietnamese village identity: bamboo-green title lettering, muted-gold lacquer outline, tiny lotus corner details and a subtle rice-ear motif. Highly legible at 300 pixels, centered, transparent background, no English words, no subtitle, no watermark. Use #163E16 #2D6423 #C89B26 #DAA520 #FFF5C3 #1C0C04. Crisp deliberate pixel grid.
```

Character prompt:

```text
400×400 transparent pixel-art bust/three-quarter character for a cozy Mekong Delta farming game menu. Friendly young Vietnamese farmer, simple indigo áo bà ba, warm straw nón lá, welcoming relaxed pose, proportions and pixel density compatible with Sprout Lands-style cozy farming sprites but original design, no copied character, no text, no UI, transparent background. Colors #2A2858 #D0A573 #D2B96E #163E16 #12230A.
```

- [ ] **Step 3: Generate Bà Năm and the UI pieces as separate assets**

Bà Năm prompt:

```text
256×256 transparent pixel-art portrait of Bà Năm, a kind older Vietnamese shopkeeper from the Mekong Delta. Warm expressive face, subtle smile, indigo áo bà ba, nón lá resting behind her head, readable at 128 pixels, original character, clean silhouette, no text, no frame, transparent background. Palette #2A2858 #D0A573 #D2B96E #482C12 #FFF5C3 #12230A.
```

UI family prompt:

```text
Original cohesive pixel-art UI asset family for a Vietnamese Mekong farming game: lacquer-brown and aged-wood frames, muted-gold layered borders, cream text area, tiny lotus corner motifs, 4-pixel design grid. Produce isolated elements on transparency: a wide 960×256 dialogue frame with an empty portrait zone at left and text zone at right; a 640×128 notification frame; a 512×128 Bà Năm shop header with no text. No letters, no icons, no watermark. Colors #1C0C04 #482C12 #8C5A28 #B9823E #C89B26 #DAA520 #FFF5C3 #12230A.
```

Icon prompt:

```text
Original pixel-art HUD icon set on transparency, strong silhouette and consistent 4-pixel grid: one Vietnamese đồng coin with rice-ear engraving in muted gold, plus three separate weather icons for sunny, cloudy, rainy. No letters, no gradients, no watermark. Gold #DAA520 #FFDC50 #8C640A; sky #64AAE6; cloud cream #FFF5C3; rain/canal #3A6E8C #7AB8D4; shadow #12230A.
```

Split/export icons into the exact target files and dimensions listed above without resampling blur.

- [ ] **Step 4: Record provenance and perform visual review**

Create `art-source-ledger.csv` with this exact header:

```csv
path,origin,tool_or_author,created_date,license,commercial_ok,notes
```

Every new row uses `origin=original-generated-and-edited`, `license=project-owned-output`, `commercial_ok=Y`, and notes the prompt family plus manual pixel cleanup performed. Review at 1×, 2× and target screen size for stray alpha, broken borders, inconsistent light direction and text accidentally baked into no-text assets.

- [ ] **Step 5: Commit source art**

```powershell
git add Assets/LangQueToi/Art
git commit -m "art: add Làng Quê Tôi visual identity assets"
```

---

### Task 2: Import Settings and Dimension Validation

**Files:**
- Create: `Assets/Scripts/Editor/LQTArtImporter.cs`
- Create: `Assets/LangQueToi/Tests/EditMode/ArtDimensionTests.cs`
- Create on execution: `Artifacts/Visual/art-validation.json`

**Interfaces:**
- Consumes: exact art paths from Task 1
- Produces: deterministic sprite import settings and `LQTArtImporter.ConfigureAndValidateFromCommandLine()`

- [ ] **Step 1: Write failing dimension tests**

Create tests using `AssetDatabase.LoadAssetAtPath<Texture2D>` and assert exact width/height for every file. Also assert `TextureImporter.textureType == Sprite`, `filterMode == Point`, `textureCompression == Uncompressed`, `mipmapEnabled == false`, and `alphaIsTransparency == true`.

For `dialogue-frame.png`, assert sprite border equals `(24, 24, 24, 24)`.

- [ ] **Step 2: Run the tests and verify import assertions fail**

Expected: dimensions pass if exports are correct; at least one importer setting fails before the importer tool runs.

- [ ] **Step 3: Implement deterministic import configuration**

Use this core method in `LQTArtImporter.cs`:

```csharp
private static void Configure(string path, bool sliced, int maxSize)
{
    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
    importer.textureType = TextureImporterType.Sprite;
    importer.spriteImportMode = SpriteImportMode.Single;
    importer.spritePixelsPerUnit = 16f;
    importer.filterMode = FilterMode.Point;
    importer.textureCompression = TextureImporterCompression.Uncompressed;
    importer.mipmapEnabled = false;
    importer.alphaIsTransparency = true;
    importer.maxTextureSize = maxSize;
    importer.wrapMode = TextureWrapMode.Clamp;
    importer.spriteBorder = sliced ? new Vector4(24f, 24f, 24f, 24f) : Vector4.zero;
    importer.SaveAndReimport();
}
```

Use max size 2048 for menu background, 1024 for dialogue/notification/header, and 512 for all remaining assets. Configure `dialogue-frame`, `notification-frame`, and `ba-nam-shop-header` as sliced.

- [ ] **Step 4: Run validation and tests**

Execute `LQTArtImporter.ConfigureAndValidateFromCommandLine`, then all EditMode tests.

Expected: all art dimension/import tests pass; JSON report has zero errors.

- [ ] **Step 5: Commit importer metadata**

```powershell
git add Assets/LangQueToi/Art Assets/Scripts/Editor/LQTArtImporter.cs Assets/LangQueToi/Tests/EditMode/ArtDimensionTests.cs Artifacts/Visual
git commit -m "build: configure Làng Quê Tôi sprite imports"
```

---

### Task 3: Nunito Vietnamese TMP Fonts

**Files:**
- Create: `Assets/LangQueToi/Fonts/Nunito-Regular.ttf`
- Create: `Assets/LangQueToi/Fonts/Nunito-Bold.ttf`
- Create: `Assets/LangQueToi/Fonts/OFL.txt`
- Create: `Assets/LangQueToi/Fonts/LQT_Nunito_Regular_SDF.asset`
- Create: `Assets/LangQueToi/Fonts/LQT_Nunito_Bold_SDF.asset`
- Create: `Assets/Scripts/Editor/LQTFontBuilder.cs`
- Create: `Assets/LangQueToi/Tests/EditMode/VietnameseGlyphTests.cs`

**Interfaces:**
- Consumes: official Nunito family under SIL Open Font License
- Produces: two static-populated TMP SDF assets covering every character used by catalog and serialized Vietnamese text

- [ ] **Step 1: Acquire font files from the official Google Fonts source**

Browse the official Google Fonts Nunito page/repository at execution time. Download the family ZIP from `https://fonts.google.com/specimen/Nunito`, copy `Nunito-Regular.ttf`, `Nunito-Bold.ttf` and its `OFL.txt` into the exact paths above, and record the source URL and retrieval date in `Assets/LangQueToi/Art/art-source-ledger.csv` with `commercial_ok=Y`.

- [ ] **Step 2: Write a failing glyph coverage test**

The test loads both TMP assets and checks every distinct character in this exact corpus:

```text
ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789
ÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬĐÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴ
àáảãạăằắẳẵặâầấẩẫậđèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵ
₫…“”‘’.,:;!?()[]{}+-×/\%&
```

For each character call `font.HasCharacter(character)` and fail with its Unicode code point.

- [ ] **Step 3: Build the two TMP assets**

`LQTFontBuilder.BuildFromCommandLine()` loads each TTF, calls `TMP_FontAsset.CreateFontAsset(font)`, sets `atlasPopulationMode = AtlasPopulationMode.Dynamic`, calls `TryAddCharacters(corpus, out string missing)`, fails when `missing` is non-empty, then sets `atlasPopulationMode = AtlasPopulationMode.Static` and creates/saves the asset at the exact output path.

Set Regular as Bold's fallback and Bold as Regular's fallback only after both assets exist; do not add the old pixel font as a Vietnamese fallback.

- [ ] **Step 4: Run glyph tests and commit**

Expected: both font assets contain all corpus characters and `₫`.

```powershell
git add Assets/LangQueToi/Fonts Assets/Scripts/Editor/LQTFontBuilder.cs Assets/LangQueToi/Tests/EditMode/VietnameseGlyphTests.cs
git commit -m "feat: add Nunito Vietnamese TMP fonts"
```

---

### Task 4: Dialogue Presentation API and Bà Năm Runtime Wiring

**Files:**
- Create: `Assets/LangQueToi/Runtime/DialoguePresentation.cs`
- Modify: `Assets/Scripts/UI/DialoguePanelUI.cs:20-138`
- Modify: `Assets/Scripts/NPC/Base/NPCShop.cs:13-68`
- Create: `Assets/LangQueToi/Tests/EditMode/DialoguePresentationTests.cs`

**Interfaces:**
- Consumes: `LangQueToi.Loc`, Bà Năm portrait assigned in MainScene
- Produces: immutable `DialoguePresentation(string speakerName, Sprite portrait, string text)` and overload `DialoguePanelUI.Open(DialoguePresentation)` while preserving `Open()` and `PlayDialogue(string)`

- [ ] **Step 1: Write the presentation value-object test**

```csharp
[Test]
public void DialoguePresentation_PreservesSpeakerPortraitAndText()
{
    var sprite = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);
    var value = new DialoguePresentation("Bà Năm", sprite, "Chào cháu!");
    Assert.That(value.SpeakerName, Is.EqualTo("Bà Năm"));
    Assert.That(value.Portrait, Is.SameAs(sprite));
    Assert.That(value.Text, Is.EqualTo("Chào cháu!"));
}
```

- [ ] **Step 2: Implement the value object**

```csharp
using UnityEngine;

namespace LangQueToi
{
    public readonly struct DialoguePresentation
    {
        public DialoguePresentation(string speakerName, Sprite portrait, string text)
        {
            SpeakerName = speakerName ?? string.Empty;
            Portrait = portrait;
            Text = text ?? string.Empty;
        }

        public string SpeakerName { get; }
        public Sprite Portrait { get; }
        public string Text { get; }
    }
}
```

- [ ] **Step 3: Extend DialoguePanelUI without breaking old API**

Add serialized fields:

```csharp
[Header("Speaker")]
[SerializeField] private UnityEngine.UI.Image portraitImage;
[SerializeField] private TMP_Text speakerNameText;
[SerializeField] private Sprite defaultPortrait;
[SerializeField] private string defaultSpeakerName = "Bà Năm";
```

Keep parameterless `Open()` and implement:

```csharp
public void Open()
{
    int greeting = UnityEngine.Random.Range(0, 4);
    Open(new DialoguePresentation(
        defaultSpeakerName,
        defaultPortrait,
        Loc.Get($"dialogue.shop.greeting.{greeting}")));
}

public void Open(DialoguePresentation presentation)
{
    ApplyPresentation(presentation);
    _isOpen = true;
    Player.Instance.EnterDialogue();
    playerIndicator.Hide();
    _dialogueAnimator.Show(onComplete: () => PlayDialogue(presentation.Text));
}

private void ApplyPresentation(DialoguePresentation presentation)
{
    if (speakerNameText != null)
        speakerNameText.text = presentation.SpeakerName;
    if (portraitImage != null)
    {
        portraitImage.sprite = presentation.Portrait;
        portraitImage.enabled = presentation.Portrait != null;
    }
}
```

In `Close(Action)`, replace `StopAllCoroutines()` with a guarded stop of `_typingCoroutine` only. Reset portrait/speaker inside the animator hide callback after `_isOpen = false`. Keep `PlayDialogue(string)` signature and typewriter behavior unchanged.

- [ ] **Step 4: Wire NPCShop explicitly**

Add to `NPCShop`:

```csharp
[Header("Presentation")]
[SerializeField] private Sprite portrait;
[SerializeField] private string speakerName = "Bà Năm";
```

Replace the current open call with:

```csharp
int greeting = UnityEngine.Random.Range(0, 4);
DialoguePanelUI.Instance.Open(new DialoguePresentation(
    speakerName,
    portrait,
    Loc.Get($"dialogue.shop.greeting.{greeting}")));
```

Add `using LangQueToi;`. Do not add `OpenWithPortrait` because no such API exists in the audited project.

- [ ] **Step 5: Run tests/compile and commit**

Expected: old `Open()` and `PlayDialogue(string)` compile; value-object test passes; no `StopAllCoroutines` remains in `DialoguePanelUI`.

```powershell
git add Assets/LangQueToi/Runtime Assets/LangQueToi/Tests Assets/Scripts/UI/DialoguePanelUI.cs Assets/Scripts/NPC/Base/NPCShop.cs
git commit -m "feat: add Bà Năm dialogue presentation"
```

---

### Task 5: Targeted Scene Rebinding and Responsive Layout

**Files:**
- Create: `Assets/Scripts/Editor/LQTVisualSceneSetup.cs`
- Modify through Unity serialization: `Assets/Scenes/MenuScene.unity`
- Modify through Unity serialization: `Assets/Scenes/MainScene.unity`
- Modify through Unity serialization: relevant UI prefabs under `Assets/Prefabs/UI`
- Create on execution: `Artifacts/Visual/scene-setup.json`

**Interfaces:**
- Consumes: imported art, Nunito fonts, dialogue serialized fields
- Produces: idempotent `LQTVisualSceneSetup.ApplyFromCommandLine()` and exact visual reference report

- [ ] **Step 1: Implement hierarchy-safe scene lookup**

Use scene roots plus `Transform.Find`; never use global `GameObject.Find`. The method must throw for a missing/duplicate path:

```csharp
private static Transform RequirePath(Scene scene, string path)
{
    string[] segments = path.Split('/');
    GameObject[] roots = scene.GetRootGameObjects();
    Transform current = roots.Single(root => root.name == segments[0]).transform;
    for (int i = 1; i < segments.Length; i++)
    {
        current = current.Find(segments[i]);
        if (current == null)
            throw new InvalidOperationException($"Missing hierarchy path: {path}");
    }
    return current;
}
```

- [ ] **Step 2: Rebind exact visual targets**

Apply these bindings through `SerializedObject`:

```text
MenuScene | Canvas/Background | Image.sprite | menu-background.png
MenuScene | Canvas/LogoTitle | Image.sprite | title-mark.png
MenuScene | Canvas/CharacterPortrait | Image.sprite | menu-character.png
MainScene | Canvas/LogoTitle | Image.sprite | title-mark.png
MainScene | Canvas/DialoguePanel/DialogueBox | Image.sprite | dialogue-frame.png; Image.type=Sliced
MainScene | Canvas/ShopPanel/BookContainer/GoldContainer/GoldImage | Image.sprite | coin-dong.png
MainScene | Canvas/NotificationUI/Background | Image.sprite | notification-frame.png; Image.type=Sliced
```

Under `Canvas/ShopPanel/BookContainer/BuyPage`, create a child named `ShopHeaderFrame` only if absent. Give it an `Image` using `ba-nam-shop-header.png`, `Image.type=Sliced`, anchor min `(0.5,1)`, anchor max `(0.5,1)`, pivot `(0.5,1)`, anchored position `(0,-16)`, size `(512,128)`, and set it as the first sibling so the existing `ShopHeader` TMP text renders above it.

For `WeatherIconUI`, set its three serialized `iconEntries` sprites in enum order Sunny, Cloudy, Rainy. Do not add a state owner.

- [ ] **Step 3: Add the portrait Image and bind dialogue fields**

Under `Canvas/DialoguePanel/DialogueBox`, create `NPCPortrait` only if absent. Add `RectTransform` anchors `(0, 0.5)`, pivot `(0, 0.5)`, anchored position `(40, 0)`, size `(192, 192)` and an `Image` using `ba-nam-portrait.png` with preserve aspect enabled.

Reuse existing `Canvas/DialoguePanel/DialogueBox/NPCNameText`; do not rename it. Set serialized fields on `DialoguePanelUI`: `portraitImage`, `speakerNameText`, `defaultPortrait`. Assign the same portrait to `NPCShop.portrait` and speaker name `Bà Năm`.

Move `DialogueText` to leave a 232 px portrait column and retain total dialogue box size 960×256. Keep choices inside the right text column.

- [ ] **Step 4: Apply Nunito and expansion rules**

For every player-facing `TMP_Text` in both scenes and project-owned UI prefabs:

- Regular body: `LQT_Nunito_Regular_SDF`.
- Heading, button and speaker name: `LQT_Nunito_Bold_SDF`.
- Enable autosize only for buttons/headings, with min 18 and max equal to current point size.
- Body text keeps fixed size at least 20, wraps, and uses overflow mode `Overflow` only in audited scroll views; otherwise `Truncate` is forbidden.
- Padding follows 4 px increments.

Exclude third-party TMP example prefabs and debug-only objects.

- [ ] **Step 5: Apply twice and validate idempotency**

Run setup and save both scenes/prefabs. Record `$before = git diff --binary | git hash-object --stdin`, run setup a second time, then record `$after` the same way and fail when `$before -ne $after`.

Expected: second run creates no changes. Report confirms every exact sprite/font/serialized reference and dialogue rect 960×256.

- [ ] **Step 6: Manual visual matrix**

Open MenuScene and MainScene at 1280×720, 1920×1080 and 2560×1440. Capture screenshots into `Artifacts/Visual/<scene>-<resolution>.png`. Verify title legibility, no glyph squares, no overlap, Bà Năm portrait not stretched, choices clickable, coin/weather readable and existing animations still play.

- [ ] **Step 7: Commit the integrated UI**

```powershell
git add Assets/Scenes Assets/Prefabs Assets/Scripts/Editor/LQTVisualSceneSetup.cs Artifacts/Visual
git commit -m "feat: apply Làng Quê Tôi visual UI"
```

---

### Task 6: Visual and Dialogue Regression Gate

**Files:**
- Create: `Assets/Scripts/Editor/LQTVisualValidator.cs`
- Create: `Assets/LangQueToi/Tests/EditMode/VisualContractTests.cs`
- Create on execution: `Artifacts/Visual/visual-validation.json`

**Interfaces:**
- Consumes: final scene/prefab/font/art bindings
- Produces: fatal gate for missing visuals, glyphs, dialogue references and prohibited truncation

- [ ] **Step 1: Add contract tests**

Tests assert exact art dimensions, the three weather enum values, four greeting keys, both Nunito assets, and `DialoguePresentation` null normalization. Add a source test asserting `DialoguePanelUI` still declares `public void Open()` and `public void PlayDialogue(string text)`.

- [ ] **Step 2: Implement serialized visual validation**

The validator opens both scenes and checks every binding from Task 5. It also loads every relevant prefab, finds null fonts/sprites, validates Vietnamese glyph coverage from all Loc and translated display strings, and fails if any UI text contains `�`, `□`, `⟦`, or English values from the static manifest source column.

- [ ] **Step 3: Run complete EditMode and visual validation**

Expected: zero fatal findings. Warnings may include only explicitly excluded third-party sample content outside enabled scenes/prefabs.

- [ ] **Step 4: Commit and tag the visual phase**

```powershell
git add Assets/Scripts/Editor/LQTVisualValidator.cs Assets/LangQueToi/Tests Artifacts/Visual
git commit -m "test: gate Làng Quê Tôi visual integration"
git tag v0.5-visual-ui
```
