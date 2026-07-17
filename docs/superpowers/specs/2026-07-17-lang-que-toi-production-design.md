# Làng Quê Tôi — Đặc tả thiết kế sản xuất

Ngày duyệt: 2026-07-17

Trạng thái: Đã duyệt D1–D4, sẵn sàng lập kế hoạch triển khai

Project: TheSprouty / Unity 6

Phạm vi phát hành: Windows x86_64, bộ hình Steam-ready, chưa tích hợp Steamworks

## 1. Mục tiêu

Chuyển TheSprouty thành **Làng Quê Tôi**, một trải nghiệm nông trại pixel-art mang bản sắc miền Tây Nam Bộ, với giao diện tiếng Việt hoàn chỉnh, nhận diện hình ảnh nhất quán, Bà Năm làm nhân vật cửa hàng, âm thanh có nguồn gốc hợp lệ và bản build Windows có thể kiểm thử độc lập.

Thay đổi phải giữ nguyên gameplay hiện có, tương thích save và không làm hỏng reference Unity. Chất lượng mục tiêu là AAA indie có thể trình bày trên Steam, nhưng không tuyên bố production-ready nếu chưa vượt đủ build, regression, licensing và visual QA.

## 2. Sự thật đã audit từ project

- Project khai báo Unity `6000.3.12f1`.
- Build Settings có đúng hai scene: `Assets/Scenes/MenuScene.unity` và `Assets/Scenes/MainScene.unity`.
- Có 144 script C#.
- Có 68 ScriptableObject vật phẩm và 36 kết quả câu cá thực tế.
- Field runtime hiển thị đã xác nhận gồm `itemName`, `seedName`, `cropName` và `shopName`.
- `ToolSO` dùng `itemName` kế thừa từ `BaseItemSO`; `toolName` còn trong một số YAML cũ là serialized data mồ côi, không phải field runtime và không được hồi sinh.
- `FishSO` kế thừa `ItemSO`; không có field `fishName` riêng.
- Save dùng tên object/định danh nội bộ, không dùng bản dịch hiển thị làm khóa.
- Gold được lưu bằng `int`.
- Game không có hệ thống season.
- Weather chỉ có `Sunny`, `Cloudy`, `Rainy`.
- Project không có `WeatherManager` hoặc event/property sở hữu trạng thái weather; `WeatherIconUI.SetWeather(WeatherType)` hiện chỉ là API trình bày.
- `DialoguePanelUI` hiện có `Open()` và `PlayDialogue()`; chưa có portrait hoặc speaker name được serialize.
- Dialogue box thực tế là 960×256.
- Weather icon hiện có rect 90×90.
- Logo menu hiện dùng sprite 500×500 trong rect 600×600.
- Font hiện tại là pixel font/DePixelBreit SDF và chưa có fallback tiếng Việt được xác nhận.
- Audio hiện có ít nhất `On the Farm.wav` và `idoberg-cozy-lofi-beat-split-memmories-248205.mp3`; quyền phân phối phải được audit trước khi giữ trong release.
- Readme trong `UI/Sprout Lands - UI Pack - Basic pack`, `Sprout Lands - Sprites - Basic pack` và `Sprout Sorry pack` chỉ cho phép dự án phi thương mại. Static dependency audit từ hai scene hiện chạm ít nhất 29 asset trong ba vùng này. Ngày 2026-07-17, chủ dự án đã xác nhận có quyền sử dụng thương mại cho cả ba package; pipeline phải lưu xác nhận này trong compliance ledger và không được suy rộng sang asset ngoài ba package.
- Repo ban đầu không có lịch sử Git. Baseline nguyên trạng đã được tạo riêng trước mọi thay đổi sản phẩm.

## 3. Ranh giới bất biến

Không được thay đổi:

- `UnityEngine.Object.name` của asset hiện hữu.
- GUID, fileID hoặc nội dung `.meta` chỉ để đổi nhận diện.
- Tên file hiện hữu, tên class, namespace, enum value hoặc public API hiện hữu.
- Save key, internal ID hoặc kiểu dữ liệu gold.
- Logic farming, fishing, inventory, economy, thời gian, weather, movement và animation hiện có, trừ sửa lỗi cần thiết để tích hợp phần trình bày đã duyệt.
- Danh sách 68 vật phẩm và 36 kết quả câu cá.

Không được bổ sung season, cây trồng, cá, NPC, cơ chế gameplay, Steam App ID, achievement, cloud save hoặc Steamworks SDK trong phạm vi này.

Không bulk replace YAML. Không ghi đè asset gốc của bên thứ ba. Không đưa asset âm thanh chưa xác minh quyền thương mại vào bản build phát hành.

Mục tiêu “giữ world sprites” chỉ áp dụng cho asset có quyền sử dụng phù hợp. Quyền phát hành luôn ưu tiên hơn bảo toàn asset. Với ba package đã được chủ dự án xác nhận quyền thương mại, asset đang reference được phép giữ lại và phải xuất hiện trong compliance ledger; mọi asset ngoài phạm vi xác nhận vẫn phải có bằng chứng riêng hoặc bị loại khỏi build phát hành.

## 4. Kiến trúc — Audited Hybrid Pipeline

Quy trình gồm bốn lớp độc lập nhưng được kiểm toán chung:

1. **Runtime localization** xử lý chuỗi phát sinh động, formatter và hội thoại.
2. **Editor migration** xử lý text tĩnh trong scene, prefab và display field trong ScriptableObject bằng SerializedObject/SerializedProperty.
3. **Targeted visual rebinding** thêm asset mới và gán vào đúng reference đã audit, không sửa asset nguồn.
4. **Validation/build gates** kiểm tra catalog, serialized assets, reference, save, license, UI, build và artifact.

Mỗi migration phải có dry-run, báo cáo mapping chính xác, số lượng expected/actual, danh sách unmapped và chế độ apply idempotent. Asset chỉ được lưu khi giá trị đích thực sự thay đổi.

Baseline và commit theo phase là một phần của kiến trúc kiểm soát regression. Mốc tag D3/D4 chỉ được tạo sau khi phase tương ứng đạt nghiệm thu thực tế.

## 5. Nhận diện hình ảnh

### 5.1 Chủ đề

Menu và key art diễn tả đồng bằng sông Cửu Long lúc nắng vàng: ruộng lúa, kênh nước, dừa, nhà gỗ, xuồng ba lá và đèn lồng đỏ dùng tiết chế. Art phải hòa hợp với pixel-art hiện hữu và được tinh chỉnh theo pixel grid.

Title mark trong game là PNG vuông 500×500, nền trong suốt, tương thích rect 600×600. Steam dùng logo master ngang riêng; không kéo giãn title mark vuông.

### 5.2 Bảng màu bất biến

| Token | Màu |
|---|---|
| Bamboo Dark | `#163E16` |
| Bamboo Mid | `#2D6423` |
| Bamboo Light | `#4E9440` |
| Lacquer Brown | `#1C0C04` |
| Lacquer Border | `#C89B26` |
| Aged Wood Dark | `#482C12` |
| Aged Wood Mid | `#8C5A28` |
| Aged Wood Light | `#B9823E` |
| Muted Gold | `#DAA520` |
| Gold Highlight | `#FFDC50` |
| Gold Shadow | `#8C640A` |
| Cream | `#FFF5C3` |
| Sky High | `#64AAE6` |
| Sky Low | `#AFE09B` |
| Paddy | `#205220` |
| Canal | `#3A6E8C` |
| Canal Shine | `#7AB8D4` |
| Skin | `#D0A573` |
| Áo bà ba Indigo | `#2A2858` |
| Nón lá | `#D2B96E` |
| Red Lantern | `#C41E1C`, chỉ dùng cho đèn lồng |
| Coconut Palm | `#4A7A1E` |
| Shadow | `#12230A` |

### 5.3 UI

- Grid thiết kế 4 px, border nhiều lớp, góc hoa sen tiết chế.
- Panel dùng lacquer brown/aged wood/cream/muted gold.
- Tái sử dụng animator hiện có.
- Dialogue box 960×256 dùng sprite 9-slice lacquer/gold.
- Bổ sung an toàn `Image portraitImage` và `TMP_Text speakerNameText` vào `DialoguePanelUI`.
- Portrait Bà Năm và tên người nói không làm thay đổi signature của `Open()` hoặc `PlayDialogue()`.
- Coin dùng ký hiệu đồng và formatter `1.250 ₫`.
- Chỉ tạo ba weather icon tương ứng ba trạng thái thực tế.
- Thay riêng logo, menu art, dialogue, portrait Bà Năm, coin, weather, notification và shop header; giữ world tiles, animal sprites và player controller hiện hữu.

### 5.4 Typography

- Nunito Regular cho body và Nunito Bold cho heading/button.
- Tạo TMP SDF asset với đầy đủ glyph tiếng Việt, chữ số, punctuation và ký hiệu `₫`.
- Fallback phải được cấu hình và kiểm thử.
- Autosize có min/max rõ ràng; ưu tiên biên tập câu và tăng layout trước khi thu chữ xuống mức khó đọc.

Primary artwork phải là asset sản xuất riêng. Pillow hoặc script chỉ dùng cho resize, composition, slicing và validation xác định; không dùng hình vẽ thô sinh bằng code làm art chính.

## 6. Việt hóa và luồng dữ liệu

### 6.1 Phân tách định danh và hiển thị

Tên object và save ID giữ tiếng Anh/nội bộ. Các field hiển thị được Việt hóa trực tiếp:

- `itemName`
- `seedName`
- `cropName`
- `shopName`

Mapping phải bao phủ đúng 68 vật phẩm và 36 kết quả câu cá đã audit. Seed/item/crop liên quan phải nhất quán. Tool hiện hữu được điền vào `itemName`; migration bằng SerializedObject sẽ loại dữ liệu `toolName` mồ côi khi Unity lưu lại đúng schema. Không ánh xạ theo field giả định như `fishName`, `displayName` hoặc `toolName` nếu type thực tế không có field đó.

### 6.2 Runtime catalog

Catalog chỉ chứa chuỗi runtime thực sự được gọi. Nhóm khóa dự kiến:

- `dialogue.shop.greeting.*`
- `shop.sell.success`
- `bed.too_early`
- `save.saving`
- `save.saved`
- `calendar.day`
- `currency.amount`

Không đăng ký khóa season hoặc feature không tồn tại.

Formatter tiền dùng `CultureInfo("vi-VN")` và `N0`, chỉ nhận kiểu `int` theo data model hiện hữu. Kết quả chuẩn gồm `0 ₫`, `1.250 ₫`, `10.000 ₫`, `999.999 ₫`.

Missing key phải log một lần và hiện `⟦key⟧` trong QA. Production build gate dựa trên phân tích catalog/call site tĩnh và asset coverage, không phụ thuộc vào missing key đã tình cờ chạy trong session.

Placeholder phải được xác minh số lượng và index. Lỗi format phải có thông báo chứa key để truy vết.

### 6.3 Static content migration

Editor migration ánh xạ theo asset path và hierarchy path chính xác. Tool phải:

- Mở và kiểm tra cả scene, không chỉ object đang load.
- Load prefab/SO trực tiếp qua AssetDatabase.
- Có dry-run không làm bẩn asset.
- Phát hiện path trùng, path không tồn tại, giá trị nguồn không khớp và field không tồn tại.
- Apply idempotent và sinh báo cáo UTF-8 ngoài luồng gameplay.
- Không đổi GameObject `m_Name`.

### 6.4 Giọng văn

Tiếng Việt phải tự nhiên, súc tích, không dịch word-by-word. Bà Năm dùng giọng miền Nam nhẹ, gọi người chơi là “cháu”, dùng “nhen” tiết chế. Không dùng emoji trong Loc string phát hành.

Ví dụ lời chào đã duyệt:

> Hôm nay trời đẹp quá ha, cháu! Hàng mới vừa lên kệ đó, coi thử có món nào ưng không nhen?

Mỗi lần mở shop có thể chọn trong tập câu chào đã duyệt, nhưng lựa chọn này chỉ thay trình bày, không đổi logic shop.

### 6.5 Dialogue presentation

Phần mở rộng hội thoại truyền presentation data gồm speaker name, portrait và lines. API cũ vẫn hoạt động. Không dùng cách gọi lồng khiến panel mở hai lần, không dùng `StopAllCoroutines()` làm gián đoạn coroutine không liên quan, và phải reset portrait/speaker khi đóng hoặc mở dialogue không có người nói.

## 7. Âm thanh và licensing

Tạo inventory tất cả `.wav`, `.mp3`, `.ogg`, sau đó ledger có tối thiểu:

- filename
- loại BGM/SFX/Ambience
- source URL
- license
- author
- ngày thu thập
- attribution required
- commercial use allowed
- ghi chú/chỉnh sửa

`commercial_ok = UNKNOWN` được coi là không được phép phát hành. Clip đó phải bị loại khỏi reference build cho đến khi có bằng chứng.

Nếu license yêu cầu attribution, tạo credits đi kèm build. Không coi tên website nguồn là bằng chứng đủ; phải lưu điều khoản/license áp dụng cho đúng asset tại thời điểm thu thập.

Không gian âm thanh mục tiêu:

- Ban ngày: acoustic nhẹ, âm sắc Việt Nam tiết chế.
- Chiều/tối: phối dịu hơn.
- Ambience: kênh nước, gió ruộng, chim, côn trùng và mưa.
- Crossfade tự động dựa trên `DayCycleManager.OnHourChanged` và `CurrentTimeOfDay` hiện hữu.
- Không tạo weather bridge trả về giá trị giả và không thêm weather gameplay/controller ngoài scope. Rain ambience chỉ được chuẩn bị trong ledger/import; chưa tự động phát cho đến khi project có nguồn trạng thái weather thật.
- Chuyển track phải xử lý yêu cầu liên tiếp mà không nhảy volume; ambience cũng cần fade hoặc transition không bật cứng.
- Loop seam, peak, import setting và mixer routing phải được nghe/đo kiểm.

Nếu kiến trúc audio hiện có đã đáp ứng yêu cầu, mở rộng nó thay vì tạo controller cạnh tranh hoặc nhiều AudioSource trùng trách nhiệm.

## 8. Error handling và build gates

Lỗi migration hoặc validation phải phân loại thành fatal và warning:

- Fatal: thiếu mapping bắt buộc, missing script/reference, missing Loc key, thiếu glyph, license không hợp lệ trên clip đang được reference, build scene sai, compile error, save regression.
- Warning: asset không dùng, chuỗi debug tiếng Anh nằm trong allowlist, optional Steam asset chưa tạo trước phase marketing.

Build chỉ chạy khi preflight trả về kết quả cấu trúc thành công; không được gọi regression suite kiểu `void` rồi tiếp tục build bất kể fail.

Không xác nhận compile chỉ vì menu script đang chạy. Compile status lấy từ Unity compilation API/build report. Không xác nhận GUID integrity chỉ vì có `.meta`; phải so diff với baseline và phát hiện duplicate/missing GUID.

CSV phải được parse đúng chuẩn có quote/escaped comma; không dùng `Split(',')` cho ledger sản xuất.

Final report lấy dữ liệu có cấu trúc hoặc exit code/result object, không suy luận pass bằng cách tìm substring như `"6/6"` hoặc `"Succeeded"` trong text.

## 9. Kiểm thử

### 9.1 Tự động

- Unity compile không error.
- Catalog/call-site placeholder coverage.
- 68/68 item và 36/36 fishing outcome được map, unmapped bằng 0.
- Static text coverage trên mọi scene/prefab liên quan.
- TMP font/glyph coverage tiếng Việt và `₫`.
- Missing script và missing object reference.
- Build Settings có đúng hai scene theo thứ tự hiện hữu.
- Currency formatter test.
- Audio ledger/reference validation.
- Diff guard cho GUID, `.meta`, internal name và save schema.

### 9.2 Play Mode và manual smoke

Luồng bắt buộc:

1. Menu mới hiển thị đúng ở 1280×720, 1920×1080, 2560×1440.
2. New Game và Load Game.
3. Movement, farming, harvest, inventory.
4. Fishing và toàn bộ UI bắt cá.
5. Mở shop Bà Năm, portrait/name/greeting, mua/bán và tiền tệ.
6. Gọi `WeatherIconUI.SetWeather(...)` cho Sunny, Cloudy và Rainy để xác nhận ba icon; không tuyên bố có weather simulation.
7. Ngủ, autosave, thoát, mở lại executable và load save.
8. Dialogue cũ qua `Open()`/`PlayDialogue()`.
9. Không text overflow, chữ vuông, English player-facing hoặc console error.
10. Profiler smoke: không có allocation lặp đáng kể do formatter/catalog trong refresh loop.

Save compatibility phải được chứng minh bằng ít nhất một save tạo từ baseline, sau đó load bằng build mới và so sánh gold, inventory, day và vị trí.

## 10. Windows build

- Nếu chỉ có Unity `6000.4.6f1`, mọi auto-upgrade phải diễn ra sau baseline và nằm trong commit riêng để review.
- Target Windows x86_64.
- IL2CPP.
- LZ4HC.
- Development Build tắt.
- Build output nằm ngoài `Assets`.
- Build script không tự xóa output chưa xác minh; nếu cần clean phải kiểm tra absolute path nằm trong thư mục output dành riêng.
- Lưu BuildReport, manifest, Unity version, scene list, backend, compression, size, warnings và errors.
- Chạy `.exe` độc lập và thực hiện smoke test release.

## 11. Steam graphical assets

Theo Steamworks hiện hành:

| Asset | Kích thước |
|---|---:|
| Header Capsule | 920×430 |
| Small Capsule | 462×174 |
| Main Capsule | 1232×706 |
| Vertical Capsule | 748×896 |
| Library Capsule | 600×900 |
| Library Header | 920×430 |
| Library Hero | 3840×1240, không có chữ |
| Library Logo | rộng 1280 hoặc cao 720, PNG trong suốt |
| Shortcut Icon | 256×256 hoặc 512×512 |
| App Icon | 184×184 JPG |
| Gameplay screenshot | tối thiểu 1920×1080, 16:9 |

Capsule chỉ chứa artwork, tên game và subtitle chính thức. Không có review, giải thưởng, giá, phần trăm giảm giá hoặc marketing copy. Screenshot phải lấy từ gameplay release build, không dùng concept art hoặc mockup.

Key art master: nông dân nhỏ giữa ruộng, mặt trời sớm, chòi và dụng cụ nông trại, kênh/cần câu; phong cách pixel-art ấm. Mỗi tỉ lệ có composition riêng từ master/layered source; không kéo giãn một file đầu ra.

Bộ screenshot mục tiêu bao phủ farming, fishing, shop Bà Năm, inventory/HUD và world view. HUD được giữ khi nó phản ánh gameplay thật.

## 12. Trình tự triển khai

1. Baseline và inventory/audit manifest.
2. Runtime localization, formatter và automated coverage.
3. SO/static migration dry-run, review, apply và save compatibility.
4. Font và layout tiếng Việt.
5. Visual assets, menu, UI, Bà Năm và dialogue presentation.
6. Weather/coin/notification/shop polish.
7. Audio inventory, licensing, integration và listening QA.
8. Regression suite và manual smoke.
9. Windows release build và executable smoke.
10. Steam assets từ approved key art và gameplay screenshot thật.
11. Final structured production report.

Mỗi phase có commit riêng và không bắt đầu phase sau khi còn fatal gate.

## 13. Điều kiện hoàn tất

Chỉ được báo “production-ready” khi đồng thời đạt:

- Localization coverage hoàn chỉnh, không unmapped/missing/glyph lỗi.
- Tất cả visual reference đã kiểm tra, không missing script/reference.
- Save baseline tải đúng trong build mới.
- Ba weather icon hiển thị đúng qua API trình bày hiện hữu; farming, fishing, shop và dialogue hoạt động.
- Audio đang được reference đều có quyền thương mại đã xác minh.
- Windows IL2CPP build thành công và executable smoke pass.
- Steam asset đủ kích thước, nội dung đúng quy định và screenshot là gameplay thật.
- Console/build report không có error; warning còn lại được liệt kê và chấp nhận rõ ràng.
- Final report liên kết tới artifact, test evidence, license ledger và commit/tag tương ứng.

## 14. Rủi ro đã biết

- Nâng Unity từ 6000.3 sang 6000.4 có thể tạo serialization diff lớn; phải tách commit.
- `MainScene.unity` lớn, nên mọi thay đổi scene cần Editor serialization và diff review có mục tiêu.
- Font atlas thiếu glyph có thể chỉ lộ khi chạy câu hiếm; coverage lấy từ toàn bộ catalog và display data.
- Tên cá/vật phẩm có thể đúng ngôn ngữ nhưng không khớp sprite; mapping cần visual review.
- Audio hiện hữu có thể không đủ bằng chứng license; lịch phát hành không được phụ thuộc vào việc giữ chúng.
- Ít nhất 29 dependency hiện chạm asset có readme nội bộ giới hạn ở non-commercial; blocker đã được giải quyết bằng xác nhận quyền thương mại của chủ dự án ngày 2026-07-17, nhưng receipt/license grant gốc vẫn phải được chủ dự án lưu ngoài repository.
- Steam artwork tạo trước khi gameplay hoàn thiện dễ lệch hình ảnh thật; screenshot và final capsules chỉ khóa sau release candidate.

## 15. Tài liệu tham chiếu

- Steamworks — Graphical Assets Overview: <https://partner.steamgames.com/doc/store/assets>
- Steamworks — Graphical Asset Rules: <https://partner.steamgames.com/doc/store/assets/rules?language=english>
- Steamworks — Store Graphical Assets: <https://partner.steamgames.com/doc/store/assets/standard>
- Steamworks — Library Assets: <https://partner.steamgames.com/doc/store/assets/libraryassets>
