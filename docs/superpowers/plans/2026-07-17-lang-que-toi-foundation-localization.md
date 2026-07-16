# Làng Quê Tôi Foundation and Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Nâng project lên Unity đang cài đặt một cách kiểm soát, thêm localization runtime có kiểm thử, Việt hóa toàn bộ dữ liệu/chuỗi hiện hữu và chứng minh save key không đổi.

**Architecture:** Runtime string dùng assembly nhỏ `LQT.Runtime`; code gameplay hiện hữu trong `Assembly-CSharp` gọi API này. Dữ liệu tĩnh được migrate bằng Unity Editor API theo asset path và hierarchy path chính xác, có dry-run, apply idempotent và báo cáo cấu trúc.

**Tech Stack:** Unity 6000.4.6f1, C# 9, TextMesh Pro, Unity Test Framework 1.6.0, NUnit, UnityEditor SerializedObject/AssetDatabase.

## Global Constraints

- Baseline bất biến: commit `75d39e7`, tag `v0.0-original-baseline`.
- Không đổi `UnityEngine.Object.name`, GUID, fileID, tên file hiện hữu, enum, save key hoặc internal ID.
- Không thêm season, weather simulation, vật phẩm, cá, cây trồng, NPC hoặc gameplay mới.
- `Gold` tiếp tục là `int`; định dạng hiển thị chuẩn `1.250 ₫`.
- Dùng đúng 68 display items, trong đó 36 là fishing outcomes; không giả định `fishName`, `displayName` hoặc `toolName` runtime.
- `ToolSO` dùng `itemName` kế thừa từ `BaseItemSO`.
- Không bulk replace YAML; mọi scene/prefab/SO write đi qua Unity serialization.
- Mọi file text mới là UTF-8; Loc string phát hành không chứa emoji.
- Mỗi task kết thúc bằng test và commit độc lập.

---

### Task 1: Controlled Unity 6000.4 Upgrade

**Files:**
- Modify: `ProjectSettings/ProjectVersion.txt`
- Inspect only: every additional file changed automatically by Unity
- Create: `Artifacts/Logs/unity-upgrade.log`

**Interfaces:**
- Consumes: baseline Git commit `75d39e7`
- Produces: project serialized by installed Unity `6000.4.6f1` with an audited upgrade-only commit

- [ ] **Step 1: Confirm the tree and editor path**

Run:

```powershell
git status --short --branch
Test-Path 'C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe'
Get-Content ProjectSettings\ProjectVersion.txt
```

Expected: clean `main`, editor path returns `True`, project version reports `6000.3.12f1`.

- [ ] **Step 2: Open and close the project in batch mode**

Run:

```powershell
New-Item -ItemType Directory -Force Artifacts\Logs | Out-Null
& 'C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath (Get-Location).Path `
  -logFile (Join-Path (Get-Location).Path 'Artifacts\Logs\unity-upgrade.log')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

Expected: exit code 0 and no `Compilation failed` in the log.

- [ ] **Step 3: Audit the automatic diff**

Run:

```powershell
git status --short
git diff --stat
git diff -- ProjectSettings/ProjectVersion.txt Packages/manifest.json Packages/packages-lock.json
rg -n 'error CS|Compilation failed|MissingReferenceException' Artifacts/Logs/unity-upgrade.log
```

Expected: version changes are explainable; the final `rg` returns no matches. If Unity changes scenes or prefabs during this open, inspect every changed path and revert only the unexpected generated serialization by applying the baseline version with `git show 75d39e7:<path>` through `apply_patch`; do not use `git checkout --`.

- [ ] **Step 4: Commit the upgrade checkpoint**

```powershell
git add ProjectSettings Packages Artifacts/Logs/unity-upgrade.log
git commit -m "chore: upgrade project to Unity 6000.4.6f1"
```

Expected: clean tree after commit.

---

### Task 2: Runtime Localization Assembly and Contract Tests

**Files:**
- Create: `Assets/LangQueToi/Runtime/LQT.Runtime.asmdef`
- Create: `Assets/LangQueToi/Runtime/Loc.cs`
- Create: `Assets/LangQueToi/Tests/EditMode/LQT.EditModeTests.asmdef`
- Create: `Assets/LangQueToi/Tests/EditMode/LocTests.cs`

**Interfaces:**
- Consumes: no gameplay types
- Produces: `Loc.Get(string)`, `Loc.Format(string, params object[])`, `Loc.Gold(int)`, `Loc.Day(int)`, `Loc.Clock(float)`, `Loc.Contains(string)`

- [ ] **Step 1: Create the runtime and test assembly definitions**

`Assets/LangQueToi/Runtime/LQT.Runtime.asmdef`:

```json
{
  "name": "LQT.Runtime",
  "rootNamespace": "LangQueToi",
  "autoReferenced": true
}
```

`Assets/LangQueToi/Tests/EditMode/LQT.EditModeTests.asmdef`:

```json
{
  "name": "LQT.EditModeTests",
  "rootNamespace": "LangQueToi.Tests",
  "references": ["LQT.Runtime", "Unity.TextMeshPro"],
  "includePlatforms": ["Editor"],
  "optionalUnityReferences": ["TestAssemblies"],
  "autoReferenced": false
}
```

- [ ] **Step 2: Write failing formatter/catalog tests**

Create `Assets/LangQueToi/Tests/EditMode/LocTests.cs`:

```csharp
using NUnit.Framework;

namespace LangQueToi.Tests
{
    public sealed class LocTests
    {
        [TestCase(0, "0 ₫")]
        [TestCase(1250, "1.250 ₫")]
        [TestCase(10000, "10.000 ₫")]
        [TestCase(999999, "999.999 ₫")]
        public void Gold_UsesVietnameseThousandsSeparator(int value, string expected)
            => Assert.That(Loc.Gold(value), Is.EqualTo(expected));

        [TestCase(0f, "00:00")]
        [TestCase(6.5f, "06:30")]
        [TestCase(17.25f, "17:15")]
        [TestCase(23.999f, "23:59")]
        public void Clock_UsesTwentyFourHourTime(float hour, string expected)
            => Assert.That(Loc.Clock(hour), Is.EqualTo(expected));

        [Test]
        public void MissingKey_IsVisibleToQa()
            => Assert.That(Loc.Get("missing.test.key"), Is.EqualTo("⟦missing.test.key⟧"));

        [Test]
        public void RequiredRuntimeKeys_AreRegistered()
        {
            string[] keys =
            {
                "save.saving", "save.saved", "calendar.day", "bed.too_early",
                "inventory.full", "animal.already_fed", "animal.need_feed",
                "shop.sell.success", "shop.buy.partial", "shop.buy.success",
                "shop.total", "shop.sell_unit", "shop.sell_stock", "shop.buy_unit",
                "dialogue.shop.greeting.0", "dialogue.shop.greeting.1",
                "dialogue.shop.greeting.2", "dialogue.shop.greeting.3"
            };

            foreach (string key in keys)
                Assert.That(Loc.Contains(key), Is.True, key);
        }

        [Test]
        public void Format_UsesLocalizedCurrencyArgument()
        {
            string value = Loc.Format("shop.sell.success", Loc.Gold(1250));
            Assert.That(value, Is.EqualTo("Bà Năm trả cháu 1.250 ₫."));
        }
    }
}
```

- [ ] **Step 3: Run tests and verify the expected compile failure**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe' `
  -batchmode -projectPath (Get-Location).Path `
  -runTests -testPlatform EditMode `
  -testResults (Join-Path (Get-Location).Path 'Artifacts\Logs\loc-tests-red.xml') `
  -logFile (Join-Path (Get-Location).Path 'Artifacts\Logs\loc-tests-red.log')
```

Expected: non-zero exit or test failure because `Loc` does not exist.

- [ ] **Step 4: Implement the minimal localization runtime**

Create `Assets/LangQueToi/Runtime/Loc.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace LangQueToi
{
    public static class Loc
    {
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
        private static readonly HashSet<string> MissingLogged = new();

        private static readonly IReadOnlyDictionary<string, string> Entries =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["save.saving"] = "Đang lưu…",
                ["save.saved"] = "Đã lưu",
                ["calendar.day"] = "Ngày {0}",
                ["bed.too_early"] = "Chưa tới giờ ngủ đâu!",
                ["inventory.full"] = "Túi đồ đã đầy.",
                ["animal.already_fed"] = "Hôm nay đã cho ăn rồi!",
                ["animal.need_feed"] = "Cần {0}!",
                ["shop.sell.success"] = "Bà Năm trả cháu {0}.",
                ["shop.buy.partial"] = "Đã chi {0}; {1} món chưa giao được.",
                ["shop.buy.success"] = "Đơn hàng đã giao, tổng cộng {0}.",
                ["shop.total"] = "TỔNG: {0}",
                ["shop.sell_unit"] = "Bán: {0}",
                ["shop.sell_stock"] = "Bán: {0} - x{1}",
                ["shop.buy_unit"] = "Giá: {0}",
                ["dialogue.shop.greeting.0"] = "Hôm nay trời đẹp quá ha, cháu! Hàng mới vừa lên kệ đó, coi thử có món nào ưng không nhen?",
                ["dialogue.shop.greeting.1"] = "Cháu ghé chơi đó hả? Bà mới sắp hàng xong, cứ coi thong thả nhen.",
                ["dialogue.shop.greeting.2"] = "Vô coi hàng đi cháu, hôm nay có mấy món tươi ngon lắm đó.",
                ["dialogue.shop.greeting.3"] = "Bà Năm chờ cháu nãy giờ. Cần mua bán gì thì nói bà nghe nhen."
            };

        public static bool Contains(string key) => Entries.ContainsKey(key);

        public static string Get(string key)
        {
            if (Entries.TryGetValue(key, out string value))
                return value;

            if (MissingLogged.Add(key))
                Debug.LogError($"[Loc] Missing key: {key}");
            return $"⟦{key}⟧";
        }

        public static string Format(string key, params object[] arguments)
        {
            string template = Get(key);
            if (template.Length > 0 && template[0] == '⟦')
                return template;

            try
            {
                return string.Format(Vi, template, arguments);
            }
            catch (FormatException exception)
            {
                Debug.LogError($"[Loc] Invalid placeholders for {key}: {exception.Message}");
                return $"⟦format:{key}⟧";
            }
        }

        public static string Gold(int amount) => $"{amount.ToString("N0", Vi)} ₫";
        public static string Day(int day) => Format("calendar.day", day);

        public static string Clock(float hour)
        {
            float wrapped = Mathf.Repeat(hour, 24f);
            int h = Mathf.FloorToInt(wrapped);
            int m = Mathf.Clamp(Mathf.FloorToInt((wrapped - h) * 60f), 0, 59);
            return $"{h:00}:{m:00}";
        }
    }
}
```

- [ ] **Step 5: Run tests and commit**

Run the same Unity command with `loc-tests-green.xml` and `loc-tests-green.log`.

Expected: all `LocTests` pass and no compiler errors.

```powershell
git add Assets/LangQueToi Artifacts/Logs/loc-tests-green.xml Artifacts/Logs/loc-tests-green.log
git commit -m "feat: add Vietnamese localization runtime"
```

---

### Task 3: Replace Runtime Player-Facing Strings

**Files:**
- Modify: `Assets/Scripts/Environment/BedInteractable.cs:61`
- Modify: `Assets/Scripts/ItemPickup.cs:40`
- Modify: `Assets/Scripts/NPC/Chicken/ChickenNPC.cs:182-187`
- Modify: `Assets/Scripts/NPC/Cow/CowNPC.cs:111-116`
- Modify: `Assets/Scripts/Economy/BuyPageUI.cs:78-88`
- Modify: `Assets/Scripts/Economy/SellPageUI.cs:86-96`
- Modify: `Assets/Scripts/Economy/MyItemsRowUI.cs:35-36`
- Modify: `Assets/Scripts/Economy/OrderItemRowUI.cs:38-39`
- Modify: `Assets/Scripts/Economy/ShopItemRowUI.cs:34-35`
- Modify: `Assets/Scripts/Economy/ToSellRowUI.cs:38-39`
- Modify: `Assets/Scripts/Economy/ShopTransactionManager.cs:67-85`
- Modify: `Assets/Scripts/UI/FilledSaveSlotUI.cs:80-81`
- Modify: `Assets/Scripts/UI/GoldUI.cs:36-39`
- Modify: `Assets/Scripts/UI/SavingIndicatorUI.cs:28-29`
- Modify: `Assets/Scripts/UI/ClockUI.cs:98-118`
- Test: `Assets/LangQueToi/Tests/EditMode/LocTests.cs`

**Interfaces:**
- Consumes: `LangQueToi.Loc`
- Produces: zero hardcoded English strings at known runtime call sites; all money uses `Loc.Gold(int)`

- [ ] **Step 1: Add a failing source coverage test**

Add to `LocTests.cs`:

```csharp
[Test]
public void KnownEnglishRuntimeLiterals_AreAbsent()
{
    string[] forbidden =
    {
        "Not time to sleep yet!", "Inventory Full", "Already fed today!",
        "Clove paid you", "Partial order:", "Order delivered!",
        "TOTAL:", "Price:", "Sell:", " Days", "Saving...", "Saved!"
    };

    string projectRoot = System.IO.Path.GetFullPath(
        System.IO.Path.Combine(UnityEngine.Application.dataPath, ".."));
    string scriptsRoot = System.IO.Path.Combine(projectRoot, "Assets", "Scripts");
    string source = string.Join("\n", System.IO.Directory.GetFiles(
            scriptsRoot, "*.cs", System.IO.SearchOption.AllDirectories)
        .Where(path => !path.Contains($"{System.IO.Path.DirectorySeparatorChar}Editor{System.IO.Path.DirectorySeparatorChar}"))
        .Select(System.IO.File.ReadAllText));

    foreach (string literal in forbidden)
        Assert.That(source, Does.Not.Contain(literal), literal);
}
```

Also add `using System.Linq;` to the test file.

- [ ] **Step 2: Run the single test and verify it fails**

Run the EditMode command with `-testFilter LangQueToi.Tests.LocTests.KnownEnglishRuntimeLiterals_AreAbsent`.

Expected: FAIL listing the first English literal still present.

- [ ] **Step 3: Replace notification and animal strings**

Add `using LangQueToi;` to each modified gameplay file and apply these exact expressions:

```csharp
NotificationManager.Instance?.ShowMessage(Loc.Get("bed.too_early"));
NotificationManager.Instance.ShowMessage(Loc.Get("inventory.full"));
NotificationManager.Instance?.ShowMessage(Loc.Get("animal.already_fed"));
NotificationManager.Instance?.ShowMessage(Loc.Format("animal.need_feed", itemName));
```

Use the last two expressions in both Chicken and Cow without changing feed logic.

- [ ] **Step 4: Replace shop formatting**

Use these exact assignments:

```csharp
totalText.text = Loc.Format("shop.total", Loc.Gold(total));
priceText.text = Loc.Format("shop.sell_stock", Loc.Gold(item.sellValue), quantity);
priceText.text = Loc.Format("shop.buy_unit", Loc.Gold(entry.buyPrice));
priceText.text = Loc.Format("shop.sell_unit", Loc.Gold(item.sellValue));
```

In `ShopTransactionManager.ShowNotificationsRoutine()` use:

```csharp
NotificationManager.Instance?.ShowMessage(
    Loc.Format("shop.sell.success", Loc.Gold(_pendingSellGold)));

string msg = _pendingBuySkipped > 0
    ? Loc.Format("shop.buy.partial", Loc.Gold(_pendingBuyGoldSpent), _pendingBuySkipped)
    : Loc.Format("shop.buy.success", Loc.Gold(_pendingBuyGoldSpent));
```

Do not modify economy arithmetic.

- [ ] **Step 5: Replace save, HUD and clock formatting**

Use:

```csharp
dayText.text = Loc.Day(data.time.currentDay);
goldText.text = Loc.Gold(data.gold);
goldText.text = Loc.Gold(EconomyManager.Instance.Gold);
[SerializeField] private string savingText = "Đang lưu…";
[SerializeField] private string savedText = "Đã lưu";
timeText.text = Loc.Clock(currentHour);
```

Remove the AM/PM and 12-hour calculations from `ClockUI.UpdateTimeText`; retain hand rotation and day number behavior.

- [ ] **Step 6: Run tests, compile and commit**

Run all EditMode tests. Then run a batch `-quit` compile and search its log for `error CS`.

Expected: tests pass; zero compilation errors; coverage test passes.

```powershell
git add Assets/Scripts Assets/LangQueToi/Tests Artifacts/Logs
git commit -m "feat: localize runtime UI and notifications"
```

---

### Task 4: ScriptableObject Translation Manifest and Migrator

**Files:**
- Create: `Assets/Scripts/Editor/LQTLocalizationManifest.cs`
- Create: `Assets/Scripts/Editor/LQTDataLocalizer.cs`
- Create on execution: `Artifacts/Localization/data-dry-run.json`
- Create on execution: `Artifacts/Localization/data-applied.json`

**Interfaces:**
- Consumes: exact asset paths below
- Produces: `LQTDataLocalizer.DryRunFromCommandLine()`, `LQTDataLocalizer.ApplyFromCommandLine()`; structured counts `items=68`, `fish=36`, `seeds=8`, `crops=8`, `shops=1`, `unmapped=0`

- [ ] **Step 1: Create the exact translation manifest**

`LQTLocalizationManifest.ItemNames` must contain these path/value pairs:

```text
Assets/ScriptableObjects/ItemSO/FishSO/Anchovy.asset = Cá cơm
Assets/ScriptableObjects/ItemSO/FishSO/Axolotl.asset = Kỳ giông Mexico
Assets/ScriptableObjects/ItemSO/FishSO/Betta.asset = Cá lia thia
Assets/ScriptableObjects/ItemSO/FishSO/Clownfish.asset = Cá hề
Assets/ScriptableObjects/ItemSO/FishSO/Coral.asset = San hô
Assets/ScriptableObjects/ItemSO/FishSO/Crab.asset = Cua
Assets/ScriptableObjects/ItemSO/FishSO/EmptyCan.asset = Lon rỗng
Assets/ScriptableObjects/ItemSO/FishSO/Flounder.asset = Cá bơn
Assets/ScriptableObjects/ItemSO/FishSO/Frog.asset = Ếch
Assets/ScriptableObjects/ItemSO/FishSO/Goldfish.asset = Cá vàng
Assets/ScriptableObjects/ItemSO/FishSO/Hammerhead.asset = Cá mập đầu búa
Assets/ScriptableObjects/ItemSO/FishSO/Jellyfish.asset = Sứa
Assets/ScriptableObjects/ItemSO/FishSO/Lobster.asset = Tôm hùm
Assets/ScriptableObjects/ItemSO/FishSO/MorayEel.asset = Cá lịch biển
Assets/ScriptableObjects/ItemSO/FishSO/Nautilus.asset = Ốc anh vũ
Assets/ScriptableObjects/ItemSO/FishSO/Octopus.asset = Bạch tuộc
Assets/ScriptableObjects/ItemSO/FishSO/PearlShell.asset = Vỏ trai ngọc
Assets/ScriptableObjects/ItemSO/FishSO/PlasticBag.asset = Túi nhựa
Assets/ScriptableObjects/ItemSO/FishSO/PlasticBottle.asset = Chai nhựa
Assets/ScriptableObjects/ItemSO/FishSO/Pomfret.asset = Cá chim
Assets/ScriptableObjects/ItemSO/FishSO/Pufferfish.asset = Cá nóc
Assets/ScriptableObjects/ItemSO/FishSO/RedSnapper.asset = Cá hồng
Assets/ScriptableObjects/ItemSO/FishSO/SeaSerpent.asset = Hải xà
Assets/ScriptableObjects/ItemSO/FishSO/SeaSnake.asset = Rắn biển
Assets/ScriptableObjects/ItemSO/FishSO/SeaUrchin.asset = Cầu gai
Assets/ScriptableObjects/ItemSO/FishSO/Seahorse.asset = Cá ngựa
Assets/ScriptableObjects/ItemSO/FishSO/Seaweed.asset = Rong biển
Assets/ScriptableObjects/ItemSO/FishSO/Shark.asset = Cá mập
Assets/ScriptableObjects/ItemSO/FishSO/Shrimp.asset = Tôm
Assets/ScriptableObjects/ItemSO/FishSO/Squid.asset = Mực
Assets/ScriptableObjects/ItemSO/FishSO/Starfish.asset = Sao biển
Assets/ScriptableObjects/ItemSO/FishSO/Stingray.asset = Cá đuối
Assets/ScriptableObjects/ItemSO/FishSO/Sunfish.asset = Cá mặt trời
Assets/ScriptableObjects/ItemSO/FishSO/Swordfish.asset = Cá kiếm
Assets/ScriptableObjects/ItemSO/FishSO/Tuna.asset = Cá ngừ
Assets/ScriptableObjects/ItemSO/FishSO/Turtle.asset = Rùa biển
Assets/ScriptableObjects/ItemSO/SeedSO/CarrotSeedSO.asset = Hạt giống cà rốt
Assets/ScriptableObjects/ItemSO/SeedSO/CornSeedSO.asset = Hạt giống bắp
Assets/ScriptableObjects/ItemSO/SeedSO/EggplantSeedSO.asset = Hạt giống cà tím
Assets/ScriptableObjects/ItemSO/SeedSO/LettuceSeedSO.asset = Hạt giống xà lách
Assets/ScriptableObjects/ItemSO/SeedSO/PumpkinSeedSO.asset = Hạt giống bí đỏ
Assets/ScriptableObjects/ItemSO/SeedSO/TomatoSeedSO.asset = Hạt giống cà chua
Assets/ScriptableObjects/ItemSO/SeedSO/TurnipSeedSO.asset = Hạt giống củ cải
Assets/ScriptableObjects/ItemSO/SeedSO/WheatSeedSO.asset = Hạt giống lúa mì
Assets/ScriptableObjects/ItemSO/WorldItem/Apple.asset = Táo
Assets/ScriptableObjects/ItemSO/WorldItem/Beet.asset = Củ dền
Assets/ScriptableObjects/ItemSO/WorldItem/Carrot.asset = Cà rốt
Assets/ScriptableObjects/ItemSO/WorldItem/Corn.asset = Bắp
Assets/ScriptableObjects/ItemSO/WorldItem/EggDefault.asset = Trứng
Assets/ScriptableObjects/ItemSO/WorldItem/Eggplant.asset = Cà tím
Assets/ScriptableObjects/ItemSO/WorldItem/Grass.asset = Cỏ
Assets/ScriptableObjects/ItemSO/WorldItem/GreenBeen.asset = Đậu cô ve
Assets/ScriptableObjects/ItemSO/WorldItem/Lettuce.asset = Xà lách
Assets/ScriptableObjects/ItemSO/WorldItem/Log.asset = Gỗ
Assets/ScriptableObjects/ItemSO/WorldItem/Lotus.asset = Hoa sen
Assets/ScriptableObjects/ItemSO/WorldItem/Milk.asset = Sữa
Assets/ScriptableObjects/ItemSO/WorldItem/Orange.asset = Cam
Assets/ScriptableObjects/ItemSO/WorldItem/Peach.asset = Đào
Assets/ScriptableObjects/ItemSO/WorldItem/Pear.asset = Lê
Assets/ScriptableObjects/ItemSO/WorldItem/Pumpkin.asset = Bí đỏ
Assets/ScriptableObjects/ItemSO/WorldItem/StarFruit.asset = Khế
Assets/ScriptableObjects/ItemSO/WorldItem/Stone.asset = Đá
Assets/ScriptableObjects/ItemSO/WorldItem/Tomato.asset = Cà chua
Assets/ScriptableObjects/ItemSO/WorldItem/Tulip.asset = Hoa tulip
Assets/ScriptableObjects/ItemSO/WorldItem/Turnip.asset = Củ cải
Assets/ScriptableObjects/ItemSO/WorldItem/Wheat.asset = Lúa mì
Assets/ScriptableObjects/ToolSO/FishingRod.asset = Cần câu
Assets/ScriptableObjects/ToolSO/SeedBag.asset = Túi hạt giống
```

Add exact seed-name values identical to the eight translated seed item names. Add crop mappings:

```text
CarrotCrop.asset = Cà rốt
CornCrop.asset = Bắp
EggplantCrop.asset = Cà tím
LettuceCrop.asset = Xà lách
PumpkinCrop.asset = Bí đỏ
TomatoCrop.asset = Cà chua
TurnipCrop.asset = Củ cải
WheatCrop.asset = Lúa mì
```

Add `Assets/ScriptableObjects/ShopSO/Shop.asset = Tiệm Bà Năm`.

- [ ] **Step 2: Implement dry-run/apply with Unity serialization**

The core write helper in `LQTDataLocalizer.cs` must be:

```csharp
private static bool SetString(string assetPath, string propertyName, string value, bool apply, Report report)
{
    UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
    if (asset == null)
    {
        report.errors.Add($"Missing asset: {assetPath}");
        return false;
    }

    string objectNameBefore = asset.name;
    string guidBefore = AssetDatabase.AssetPathToGUID(assetPath);
    var serialized = new SerializedObject(asset);
    SerializedProperty property = serialized.FindProperty(propertyName);
    if (property == null || property.propertyType != SerializedPropertyType.String)
    {
        report.errors.Add($"Missing string property {propertyName}: {assetPath}");
        return false;
    }

    bool changed = property.stringValue != value;
    report.entries.Add(new Entry(assetPath, propertyName, property.stringValue, value, changed));
    if (apply && changed)
    {
        property.stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
    }

    if (asset.name != objectNameBefore || AssetDatabase.AssetPathToGUID(assetPath) != guidBefore)
        report.errors.Add($"Identity changed: {assetPath}");
    return true;
}
```

Use serializable `Report`/`Entry` classes, `JsonUtility.ToJson(report, true)`, and write under `Artifacts/Localization`. Dry-run must never call `ApplyModifiedPropertiesWithoutUndo` or `SaveAssetIfDirty`.

- [ ] **Step 3: Run dry-run and inspect exact counts**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe' `
  -batchmode -quit -projectPath (Get-Location).Path `
  -executeMethod LQTDataLocalizer.DryRunFromCommandLine `
  -logFile (Join-Path (Get-Location).Path 'Artifacts\Logs\data-dry-run.log')
```

Expected report: 68 item paths, 36 paths under `FishSO`, 8 `seedName`, 8 `cropName`, 1 `shopName`, zero errors. `git status --short Assets` must show only new scripts/meta, not modified data assets.

- [ ] **Step 4: Apply once, then prove idempotency**

Run `LQTDataLocalizer.ApplyFromCommandLine`, then dry-run again.

Expected: first apply reports changes; second dry-run reports zero pending changes and zero identity errors.

- [ ] **Step 5: Verify save identifiers remain internal**

Run:

```powershell
rg -n 'itemName = slot\.IsEmpty \? "" : slot\.GetItemSO\(\)\.name|itemName = item\.name' Assets/Scripts/Save/SaveManager.cs
git diff 75d39e7 -- Assets/ScriptableObjects/ItemRegistry.asset Assets/Scripts/Save/GameSaveData.cs Assets/Scripts/Save/SaveManager.cs
```

Expected: both save assignments still use `.name`; no save schema changes.

- [ ] **Step 6: Commit migrated data and reports**

```powershell
git add Assets/ScriptableObjects Assets/Scripts/Editor/LQTLocalizationManifest.cs Assets/Scripts/Editor/LQTDataLocalizer.cs Artifacts/Localization Artifacts/Logs
git commit -m "feat: localize item crop and shop display data"
```

---

### Task 5: Static Scene and Prefab Text Migration

**Files:**
- Create: `Assets/Scripts/Editor/LQTStaticTextLocalizer.cs`
- Modify through Unity serialization: `Assets/Scenes/MenuScene.unity`
- Modify through Unity serialization: `Assets/Scenes/MainScene.unity`
- Modify through Unity serialization: `Assets/Prefabs/ExitGamePanel.prefab`
- Modify through Unity serialization: `Assets/Prefabs/UI/EmptySaveSlot.prefab`
- Modify through Unity serialization: `Assets/Prefabs/UI/FilledSaveSlot.prefab`
- Modify through Unity serialization: row prefabs under `Assets/Prefabs/UI`
- Create on execution: `Artifacts/Localization/static-dry-run.json`
- Create on execution: `Artifacts/Localization/static-applied.json`

**Interfaces:**
- Consumes: exact `(assetPath, hierarchyPath, expectedSource, VietnameseTarget)` entries
- Produces: static UI localized without changing any GameObject name

- [ ] **Step 1: Encode the exact static mappings**

Include these scene targets:

```text
MenuScene.unity | Canvas/AboutPanel/HeaderText | ABOUT | GIỚI THIỆU
MenuScene.unity | Canvas/SettingsPanel/Rows/FullscreenRow/FullscreenText | Fullscreen | Toàn màn hình
MenuScene.unity | Canvas/SettingsPanel/Rows/AmbienceRow/AmbienceText | Ambience | Âm thanh môi trường
MenuScene.unity | Canvas/SettingsPanel/Rows/MusicRow/MusicText | Music | Nhạc
MenuScene.unity | Canvas/CreditsPanel/HeaderText | CREDITS | GHI CÔNG
MenuScene.unity | Canvas/ConfirmRemovePanel/ConfirmText | DO YOU WANT TO REMOVE THIS SAVE? | XÓA DỮ LIỆU LƯU NÀY?
MenuScene.unity | Canvas/AboutPanel/Scroll View/Viewport/Content/AboutText | The Sprouty is a cozy 2D pixel art farming game where you tend your land, | Làng Quê Tôi là trò chơi nông trại pixel 2D ấm áp. Chăm ruộng, câu cá, nuôi vật và tận hưởng nhịp sống yên bình bên miền sông nước.
MenuScene.unity | Canvas/SavePanel/HeaderText | PICK A SAVE | CHỌN Ô LƯU
MenuScene.unity | Canvas/SettingsPanel/Rows/SFXRow/SFXText | SFX | Hiệu ứng
MenuScene.unity | Canvas/SettingsPanel/Rows/TargetFPSRow/TargetFPSText | Target FPS | FPS mục tiêu
MenuScene.unity | Canvas/ExitGamePanel/ConfirmText | DO YOU WANT TO EXIT THE GAME? | BẠN MUỐN THOÁT TRÒ CHƠI?
MainScene.unity | Canvas/ShopPanel/BookContainer/SellPage/NoteText | * Your tems will be sold the next morning | * Hàng sẽ được bán vào sáng hôm sau
MainScene.unity | Canvas/PausePanel/ExitGameButton/ExitGameText | EXIT GAME | THOÁT TRÒ CHƠI
MainScene.unity | Canvas/ItemActionPanel/SellButton/SelllText | Sell | Bán
MainScene.unity | Canvas/PausePanel/OpenSettingsButton/OpenSettingsText | SETTINGS | CÀI ĐẶT
MainScene.unity | Canvas/ItemActionPanel/SellAllButton/SellAllText | Sell All | Bán tất cả
MainScene.unity | Canvas/ShopPanel/BookContainer/SellPage/MyItemsHeader | MY ITEMS | ĐỒ CỦA TÔI
MainScene.unity | Canvas/ShopPanel/BookContainer/BuyPage/NoteText | * Items will arrive the next morning | * Hàng sẽ được giao vào sáng hôm sau
MainScene.unity | Canvas/DialoguePanel/DialogueBox/ChoicesContainer/GoodbyeButton/GoodbyeText | Take care! | Bà giữ sức khỏe nhen!
MainScene.unity | Canvas/SettingsPanel/Rows/FullscreenRow/FullscreenText | Fullscreen | Toàn màn hình
MainScene.unity | Canvas/ShopPanel/BookContainer/SellPage/ToSellHeader | TO SELL | HÀNG SẼ BÁN
MainScene.unity | Canvas/ShopPanel/BookContainer/TabsContainer/BuyTabButton/BuyTabText | Buy | Mua
MainScene.unity | Canvas/PausePanel/BackToMenuButton/BackToMenuText | BACK TO MENU | VỀ MENU
MainScene.unity | Canvas/DialoguePanel/DialogueBox/NPCNameText | Clove | Bà Năm
MainScene.unity | Canvas/PausePanel/ResumeGameButton/ResumeGameButton | RESUME | TIẾP TỤC
MainScene.unity | Canvas/SettingsPanel/Rows/TargetFPSRow/TargetFPSText | Target FPS | FPS mục tiêu
MainScene.unity | Canvas/ShopPanel/BookContainer/TabsContainer/SellTabButton/SellTabText | Sell | Bán
MainScene.unity | Canvas/DialoguePanel/DialogueBox/ChoicesContainer/ShopButton/ShopText | Show what you've got! | Cho cháu xem hàng với!
MainScene.unity | Canvas/SettingsPanel/Rows/SFXRow/SFXText | SFX | Hiệu ứng
MainScene.unity | Canvas/ShopPanel/BookContainer/BuyPage/ShopHeader | SHOP | TIỆM BÀ NĂM
MainScene.unity | Canvas/ShopPanel/BookContainer/BuyPage/OrderHeader | ORDER | ĐƠN HÀNG
MainScene.unity | Canvas/SettingsPanel/Rows/AmbienceRow/AmbienceText | Ambience | Âm thanh môi trường
MainScene.unity | Canvas/ItemActionPanel/RemoveButton/RemoveText | Remove | Bỏ
MainScene.unity | Canvas/TimePanel/ClockGroup/Text/LabelText | DAY | NGÀY
MainScene.unity | Canvas/SettingsPanel/Rows/MusicRow/MusicText | Music | Nhạc
MainScene.unity | Canvas/SavingIndicator/SavingLabel | Saving... | Đang lưu…
MainScene.unity | Canvas/ExitGamePanel/ConfirmText | DO YOU WANT TO EXIT THE GAME? | BẠN MUỐN THOÁT TRÒ CHƠI?
```

Include prefab targets:

```text
ExitGamePanel.prefab | ExitGamePanel/ConfirmText | DO YOU WANT TO EXIT THE GAME? | BẠN MUỐN THOÁT TRÒ CHƠI?
EmptySaveSlot.prefab | EmptySaveSlot/NewGameText | START A NEW GAME | BẮT ĐẦU TRÒ CHƠI MỚI
FilledSaveSlot.prefab | FilledSaveSlot/SlotPanel/InfoGroup/IslandText | THE SPROUTY ISLAND | LÀNG QUÊ TÔI
FilledSaveSlot.prefab | FilledSaveSlot/SlotPanel/InfoGroup/DayText | x Days | Ngày x
MyItemsRow.prefab | MyItemsRow/NamePriceGroup/ItemNameText | Carrot Seed | Hạt giống cà rốt
MyItemsRow.prefab | MyItemsRow/NamePriceGroup/PriceText | Sell: 2 - x1 | Bán: 2 ₫ - x1
OrderItemRow.prefab | OrderItemRow/NamePriceGroup/ItemNameText | Carrot Seed | Hạt giống cà rốt
OrderItemRow.prefab | OrderItemRow/NamePriceGroup/PriceText | Price: 2 | Giá: 2 ₫
ShopItemRow.prefab | ShopItemRow/NamePriceGroup/ItemNameText | Carrot Seed | Hạt giống cà rốt
ShopItemRow.prefab | ShopItemRow/NamePriceGroup/PriceText | Price: 2 | Giá: 2 ₫
ToSellItemRow.prefab | ToSellItemRow/NamePriceGroup/ItemNameText | Carrot Seed | Hạt giống cà rốt
ToSellItemRow.prefab | ToSellItemRow/NamePriceGroup/PriceText | Sell: 2 | Bán: 2 ₫
```

The credits body target is set to:

```text
PHÁT TRIỂN
TheSprouty Team

TÀI SẢN ĐỒ HỌA
Sprout Lands — Cup Nooble

VIỆT HÓA & GIAO DIỆN
Làng Quê Tôi
```

- [ ] **Step 2: Implement exact hierarchy lookup and source guards**

Use `EditorSceneManager.OpenScene` for scenes and `PrefabUtility.LoadPrefabContents` for prefabs. Resolve each path by starting at the named root and calling `Transform.Find` on the remaining path. Require `TMP_Text`; if current text equals neither `expectedSource` nor `target`, record a fatal mismatch. Preserve every `GameObject.name` and GUID before/after.

The write operation is exactly:

```csharp
var serialized = new SerializedObject(tmp);
SerializedProperty text = serialized.FindProperty("m_text");
if (apply && text.stringValue != entry.target)
{
    text.stringValue = entry.target;
    serialized.ApplyModifiedPropertiesWithoutUndo();
    EditorUtility.SetDirty(tmp);
}
```

- [ ] **Step 3: Dry-run, apply, dry-run again**

Expose command-line methods `LQTStaticTextLocalizer.DryRunFromCommandLine` and `ApplyFromCommandLine`. Execute them with Unity batch mode.

Expected: first dry-run reports every target resolved and zero writes; apply reports all intended changes; second dry-run reports zero pending changes and zero mismatches.

- [ ] **Step 4: Run a serialized English audit**

The audit loads both scenes and all prefabs under `Assets/Prefabs`, enumerates every `TMP_Text`, and fails only player-facing English not present in a documented allowlist. Allowlist exact values: `E`, numeric-only strings, dotted spacer strings, zero-width placeholder, empty strings, `SFX`, and debug-only labels not included in enabled scenes.

Expected: zero player-facing English findings.

- [ ] **Step 5: Commit static localization**

```powershell
git add Assets/Scenes Assets/Prefabs Assets/Scripts/Editor/LQTStaticTextLocalizer.cs Artifacts/Localization Artifacts/Logs
git commit -m "feat: localize static scene and prefab text"
```

---

### Task 6: Localization Coverage and Save Compatibility Gate

**Files:**
- Create: `Assets/Scripts/Editor/LQTLocalizationValidator.cs`
- Create: `Assets/LangQueToi/Tests/EditMode/LocalizationContractTests.cs`
- Create on execution: `Artifacts/Localization/coverage.json`
- Create manually before code migration: `Artifacts/SaveCompatibility/baseline-slot.json`
- Create on execution: `Artifacts/SaveCompatibility/migrated-slot.json`

**Interfaces:**
- Consumes: Loc catalog, data/static manifests, serialized assets, baseline save
- Produces: `LQTLocalizationValidator.RunFromCommandLine()` returning process exit 0 only when coverage is complete

- [ ] **Step 1: Add contract tests for all format placeholders**

Create tests asserting:

```csharp
Assert.That(Loc.Format("calendar.day", 5), Is.EqualTo("Ngày 5"));
Assert.That(Loc.Format("animal.need_feed", "Bắp"), Is.EqualTo("Cần Bắp!"));
Assert.That(Loc.Format("shop.buy.partial", Loc.Gold(120), 2),
    Is.EqualTo("Đã chi 120 ₫; 2 món chưa giao được."));
Assert.That(Loc.Format("shop.buy.success", Loc.Gold(120)),
    Is.EqualTo("Đơn hàng đã giao, tổng cộng 120 ₫."));
```

- [ ] **Step 2: Implement structured validation**

`LQTLocalizationValidator` must combine:

- every Loc literal called by `Loc.Get`/`Loc.Format` exists;
- no registered key is unused except four randomized greetings;
- 68 item paths equal manifest targets;
- 36 fish paths are present within those 68;
- 8 seed, 8 crop and 1 shop fields equal manifest targets;
- static scene/prefab targets equal manifest targets;
- no `⟦` marker is serialized;
- all formatter placeholders are contiguous from `{0}` and match each known call contract;
- known English runtime literals are absent.

Serialize a report with `passed`, `fatalCount`, `warningCount`, `checks[]` and `findings[]`. Throw `BuildFailedException` from the command-line entry point when `fatalCount > 0`.

- [ ] **Step 3: Capture and compare save compatibility evidence**

Before applying localization in a clean baseline worktree, create slot 0 with non-zero gold, at least three inventory items, and day greater than 1; copy `sprouty_save_slot0.json` to `Artifacts/SaveCompatibility/baseline-slot.json`.

After localization, load that same save, save again, and copy it to `migrated-slot.json`. Compare these JSON paths exactly: `gold`, `time.currentDay`, every `slots[*].itemName`, every `slots[*].quantity`, and player position. Display fields are absent from the save and therefore must not appear in the diff.

- [ ] **Step 4: Run complete localization validation**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe' `
  -batchmode -quit -projectPath (Get-Location).Path `
  -executeMethod LQTLocalizationValidator.RunFromCommandLine `
  -logFile (Join-Path (Get-Location).Path 'Artifacts\Logs\localization-validation.log')
```

Expected: exit 0, `fatalCount=0`, expected mapping counts, and no missing keys.

- [ ] **Step 5: Commit and tag the completed localization phase**

```powershell
git add Assets Artifacts/Localization Artifacts/SaveCompatibility Artifacts/Logs
git commit -m "test: gate Vietnamese localization coverage"
git tag v0.3-localization
```
