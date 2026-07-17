# Làng Quê Tôi — Trạng thái triển khai (2026-07-17)

> **Tóm tắt:** Phase 1 code hoàn tất; đã fix 1 mismatch trong AboutText. Phase 2 phần code (runtime + Editor tool) đã xong; **thiếu 11 file art PNG + 2 TTF Nunito** — em không sinh được, anh phải cung cấp. Phase 3 (audio) và 4 (Steam) chưa bắt đầu.

---

## Đã hoàn thành trong hôm nay

### Runtime localization (`Assets/LangQueToi/`)
- `LQT.Runtime.asmdef` + `Loc.cs` — catalog 18 khóa, formatter `Gold` (`1.250 ₫` theo vi-VN), `Day`, `Clock`, missing-key fallback `⟦key⟧`.
- `LQT.EditModeTests.asmdef` + `LocTests.cs` — 10 test bao phủ Gold, Clock, missing key, catalog, format contract, forbidden English literal audit.

### Runtime string replacement (`Assets/Scripts/`)
Đã đổi 12 file để dùng `Loc.Get`/`Loc.Format`/`Loc.Gold`/`Loc.Day`/`Loc.Clock`, không còn hard-code English:
- `Environment/BedInteractable.cs`, `ItemPickup.cs`
- `NPC/Chicken/ChickenNPC.cs`, `NPC/Cow/CowNPC.cs`
- `Economy/BuyPageUI.cs`, `SellPageUI.cs`, `MyItemsRowUI.cs`, `OrderItemRowUI.cs`, `ShopItemRowUI.cs`, `ToSellRowUI.cs`, `ShopTransactionManager.cs`
- `UI/FilledSaveSlotUI.cs`, `GoldUI.cs`, `SavingIndicatorUI.cs` (giá trị mặc định), `ClockUI.cs` (đồng hồ 24h qua `Loc.Clock`).

### Editor tooling (`Assets/Scripts/Editor/`)
- `LQTLocalizationManifest.cs` — bảng ánh xạ chính xác 68 item (36 fish + 8 seed + 22 world item + 2 tool) + 8 seedName + 8 cropName + 1 shopName.
- `LQTDataLocalizer.cs` — dry-run/apply cho ScriptableObject qua `SerializedObject`. Không đụng `name`/GUID. Ghi báo cáo JSON vào `Artifacts/Localization/data-{dry-run,applied}.json`. Có menu `LangQueToi/Localization/Data - Dry Run` và `Data - Apply`, và CLI method `DryRunFromCommandLine`/`ApplyFromCommandLine`.
- `LQTStaticTextLocalizer.cs` — dry-run/apply cho 41 mapping trong `MenuScene.unity`, `MainScene.unity`, `ExitGamePanel.prefab`, `EmptySaveSlot.prefab`, `FilledSaveSlot.prefab`, `MyItemsRow.prefab`, `OrderItemRow.prefab`, `ShopItemRow.prefab`, `ToSellItemRow.prefab`. So sánh full text sau `\r\n → \n` normalize, không dùng substring guard. Fatal nếu current text không khớp expected source lẫn target.
- `LQTLocalizationValidator.cs` — gate: catalog coverage (mọi `Loc.Get/Loc.Format` phải có key), unused-key warn, formatter placeholder contiguous, forbidden English literal scan, và verify serialized asset đã có giá trị tiếng Việt sau apply.

---

## Việc cần anh chạy trong Unity Editor để đóng Phase 1

### Bước A — Mở project và xác nhận biên dịch
1. Mở Unity Hub, chọn Unity **6000.4.6f1** (bản anh đã cài).
2. Add project `C:\Users\ADMIN\Desktop\TheSprouty\TheSprouty` → Open.
3. Chờ import xong, kiểm tra Console **không có error CS** đỏ.

### Bước B — Commit upgrade separately (Task 1 của plan)
Sau khi Unity 6000.4.6 mở lần đầu, `ProjectSettings/ProjectVersion.txt` và các file config đã bị bump. Anh cần:
```powershell
git status --short
# Nếu có Assets/UniversalRenderPipelineGlobalSettings.asset, Packages/*, ProjectSettings/* thay đổi:
git add ProjectSettings Packages Assets/UniversalRenderPipelineGlobalSettings.asset
git commit -m "chore: upgrade project to Unity 6000.4.6f1"
```

### Bước C — Chạy migration data (68 item + 8 seed + 8 crop + 1 shop)
Trong Unity menu:
1. **LangQueToi → Localization → Data - Dry Run**
   - Mở `Artifacts/Localization/data-dry-run.json`. Kỳ vọng: `counts.items=68`, `fish=36`, `seeds=8`, `crops=8`, `shops=1`, `errors=0`.
2. Nếu dry-run sạch: **LangQueToi → Localization → Data - Apply**.
3. Chạy Dry Run lần 2 để chứng minh idempotency: `counts.changed=0`.
4. Commit:
```powershell
git add Assets/ScriptableObjects Assets/Scripts/Editor Artifacts/Localization
git commit -m "feat: localize item crop and shop display data"
```

### Bước D — Chạy migration static text (scene + prefab)
1. **LangQueToi → Localization → Static Text - Dry Run**
   - Kỳ vọng: `errors=0`, mỗi entry status = `matched` hoặc `already_target`.
   - **Nếu status = `mismatch`**: text hiện tại không khớp expected source đã hard-code trong `LQTStaticTextLocalizer.StaticMappings`. Anh cần mở scene, tìm object, so text thực tế với `expectedSource` trong log, rồi sửa mapping trong file cho khớp hoặc revert scene về baseline. Không tự sửa scene qua tay.
2. Nếu sạch: **LangQueToi → Localization → Static Text - Apply**.
3. Commit:
```powershell
git add Assets/Scenes Assets/Prefabs Artifacts/Localization
git commit -m "feat: localize static scene and prefab text"
```

### Bước E — Chạy Loc tests
Unity menu: **Window → General → Test Runner → EditMode → Run All**.
Kỳ vọng: 10/10 pass.

### Bước F — Chạy validator gate
Unity menu: **LangQueToi → Localization → Run Validator**.
Xem `Artifacts/Localization/coverage.json`. Kỳ vọng: `passed=true`, `fatalCount=0`.

### Bước G-old (thay bằng Phase 2 font builder — bỏ qua nếu chạy Phase 2)
Toàn bộ font hiện tại (`DePixelBreit SDF`, `pixelFont SDF`) **không có glyph tiếng Việt**. Tất cả text tiếng Việt sẽ hiện chữ vuông cho tới khi làm bước này:
1. Tải Nunito từ https://fonts.google.com/specimen/Nunito → giải nén vào `Assets/Fonts/Nunito/`.
2. Unity: **Window → TextMeshPro → Font Asset Creator**
   - Source Font: `Nunito-Regular.ttf`
   - Character Set: **Unicode Range (Hex)** với `0020-007E,00C0-024F,1E00-1EFF,20AB` (thêm `20AB` cho `₫`).
   - Atlas Resolution: 2048×2048, Render Mode: SDFAA.
   - **Generate Font Atlas** → **Save as** `Assets/Fonts/Nunito_Vietnamese_SDF.asset`.
3. Lặp lại với `Nunito-Bold.ttf` → `Nunito_Bold_Vietnamese_SDF.asset`.
4. Trong `Assets/Fonts/DePixelBreit SDF.asset` inspector → **Fallback Font Assets** → thêm `Nunito_Vietnamese_SDF`. Làm tương tự cho các font pixel còn lại. Đây là fallback chain — font gốc giữ style pixel cho ASCII, glyph Việt đến từ Nunito.
5. Test: mở MainScene, Play, kiểm tra chữ có dấu hiển thị đúng ở HUD, shop, dialogue.
6. Commit:
```powershell
git add Assets/Fonts
git commit -m "feat: add Nunito Vietnamese font asset and fallback"
```

---

---

## Phase 2 (visual-ui) — code xong, chờ asset

### Đã hoàn thành
- [DialoguePresentation.cs](Assets/LangQueToi/Runtime/DialoguePresentation.cs) — readonly struct (SpeakerName, Portrait, Text), null-normalized.
- [DialoguePanelUI.cs](Assets/Scripts/UI/DialoguePanelUI.cs) — thêm `Open(DialoguePresentation)` overload, `portraitImage`/`speakerNameText`/`defaultPortrait`/`defaultSpeakerName` serialized fields. Giữ nguyên signature `Open()` và `PlayDialogue(string)`. Thay `StopAllCoroutines()` bằng guarded stop chỉ cancel `_typingCoroutine`. Reset portrait/speaker khi `Close`. Loại bỏ hard-code English "What a lovely day...".
- [NPCShop.cs](Assets/Scripts/NPC/Base/NPCShop.cs) — thêm `portrait` + `speakerName` serialized. Random 1 trong 4 greeting Bà Năm từ Loc catalog.
- [LQTPalette.cs](Assets/LangQueToi/Runtime/LQTPalette.cs) — 25 color token bất biến theo spec Section 5.2 (`Color32`, dùng hex chính xác). `RedLantern` restricted-use.
- [LQTArtImporter.cs](Assets/Scripts/Editor/LQTArtImporter.cs) — 11 spec (path, width×height, sliced flag, maxSize). Idempotent (chỉ set khi cần). Menu `LangQueToi/Visual/Art - Configure && Validate`.
- [LQTFontBuilder.cs](Assets/Scripts/Editor/LQTFontBuilder.cs) — build 2 TMP SDF từ Nunito TTF, corpus tiếng Việt đầy đủ + `₫`, fallback đối xứng Regular↔Bold. Menu `LangQueToi/Visual/Font - Build Nunito SDF`.

### Anh phải làm (Phase 2 asset)
1. **Vẽ / generate 11 file art** vào các path chính xác trong `LQTArtImporter.Specs`. Spec Section 5.4 CẤM dùng Pillow / code sinh làm primary art. Anh có 2 lựa chọn:
   - **Vẽ pixel-art tay** bằng Aseprite theo palette `LQTPalette` và prompt trong [visual-ui plan](docs/superpowers/plans/2026-07-17-lang-que-toi-visual-ui.md) Task 1.
   - **Dùng imagegen skill** (Midjourney / Leonardo / DALL-E) — cách này em có thể chạy nếu anh cấp API access, nhưng em không có sẵn.
   
   Kích thước bắt buộc:
   - `Assets/LangQueToi/Art/Menu/menu-background.png` 1920×1080
   - `Assets/LangQueToi/Art/Menu/title-mark.png` 500×500 (transparent)
   - `Assets/LangQueToi/Art/Menu/menu-character.png` 400×400 (transparent)
   - `Assets/LangQueToi/Art/Dialogue/dialogue-frame.png` 960×256 (transparent, 9-slice border 24)
   - `Assets/LangQueToi/Art/Characters/ba-nam-portrait.png` 256×256 (transparent)
   - `Assets/LangQueToi/Art/HUD/coin-dong.png` 64×64
   - `Assets/LangQueToi/Art/HUD/weather-{sunny,cloudy,rainy}.png` 90×90 (×3)
   - `Assets/LangQueToi/Art/HUD/notification-frame.png` 640×128 (9-slice)
   - `Assets/LangQueToi/Art/Shop/ba-nam-shop-header.png` 512×128 (9-slice)

2. **Tải Nunito TTF** từ https://fonts.google.com/specimen/Nunito → giải nén `Nunito-Regular.ttf` + `Nunito-Bold.ttf` + `OFL.txt` vào `Assets/LangQueToi/Fonts/`.

3. **Trong Unity chạy theo thứ tự:**
   - `LangQueToi/Visual/Art - Configure && Validate` → verify `Artifacts/Visual/art-validation.json` errors=0.
   - `LangQueToi/Visual/Font - Build Nunito SDF` → tạo 2 SDF asset ở `Assets/LangQueToi/Fonts/`.
   - Mở MainScene inspector, gán:
     - `DialoguePanelUI.portraitImage` = child Image mới thêm `Canvas/DialoguePanel/DialogueBox/NPCPortrait`
     - `DialoguePanelUI.speakerNameText` = existing `NPCNameText`
     - `DialoguePanelUI.defaultPortrait` = ba-nam-portrait sprite
     - `NPCShop.portrait` = ba-nam-portrait sprite
     - Gán `LQT_Nunito_Regular_SDF` / `LQT_Nunito_Bold_SDF` cho mọi TMP Text player-facing (heading dùng Bold, body dùng Regular)
   - `WeatherIconUI` inspector: gán 3 sprite theo thứ tự enum Sunny/Cloudy/Rainy
   - Rebind `Canvas/Background` (menu), `Canvas/LogoTitle`, dialogue frame, coin, notification, shop header

### Phase 2 CHƯA có
- `LQTVisualSceneSetup` (Editor tool tự động rebind — em chưa viết vì cần biết chính xác hierarchy path của scene, và không có art thì tool rebind cũng bind vào sprite null).
- `LQTVisualValidator` gate.
- Test glyph coverage (`VietnameseGlyphTests.cs`).
- Test dialogue presentation (`DialoguePresentationTests.cs`).
- Manual visual matrix ở 1280×720 / 1920×1080 / 2560×1440.

---

## Việc CHƯA làm (nằm ngoài code hôm nay)

### Phase 1 còn thiếu
- **Task 1**: Chưa commit tách bạch upgrade Unity 6000.3 → 6000.4 (project đã ở 6000.4 sẵn, nhưng chưa audit và commit theo đúng thứ tự spec yêu cầu).
- **Task 6 Step 3**: Chưa capture save baseline + prove save compatibility. Cần:
  1. Ở baseline (`git checkout 75d39e7`), chạy game, tạo save slot 0 với gold > 0, ≥ 3 item, day > 1, copy `sprouty_save_slot0.json` → `Artifacts/SaveCompatibility/baseline-slot.json`.
  2. Về HEAD, load save đó, save lại, copy → `migrated-slot.json`.
  3. Diff `gold`, `time.currentDay`, `slots[*].itemName`, `slots[*].quantity`, position phải giống hệt.
- **Credits panel body**: source text chưa được nắm exact; static localizer bỏ qua. Cần thêm 1 entry vào `StaticMappings.All` khi có nguồn.

### Phase 3 (audio-release) — chưa bắt đầu
- Chưa có audio inventory ledger (filename/type/source URL/license/author/commercial_ok).
- Chưa xác thực quyền của `On the Farm.wav`, `idoberg-cozy-lofi-beat-split-memmories-248205.mp3` và các clip khác. **Cho tới khi có bằng chứng license, không được đưa vào release build.**
- Chưa có Windows IL2CPP build.

### Phase 4 (steam-assets) — chưa bắt đầu
- 11 asset kích thước Steam (Header/Small/Main/Vertical/Library capsule, Library Header/Hero/Logo, Shortcut/App Icon, gameplay screenshot). Phải chờ release candidate rồi mới chụp screenshot thật.

---

## Rủi ro / cần chú ý
- **Font**: cho tới khi hoàn tất bước G, mọi text tiếng Việt sẽ là chữ vuông. Đây không phải bug code — đúng như spec Section 5.4 cảnh báo.
- **Static text mismatch**: nếu ai đó đã sửa text trong scene/prefab mà không qua migrator, dry-run sẽ báo mismatch. Đừng auto-fix — verify baseline trước.
- **License CupNooble**: chủ dự án đã xác nhận quyền thương mại (spec Section 2), nhưng receipt/grant phải lưu ngoài repo. Không extrapolate ra asset ngoài 3 package đã xác nhận.
- **Save compatibility**: chưa được prove. Nếu save schema thay đổi ngầm do refactor, save từ baseline có thể không load được — phải chạy Bước B của "Task 6 Step 3" trước khi tuyên bố Phase 1 hoàn tất.
