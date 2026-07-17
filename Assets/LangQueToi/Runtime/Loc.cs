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
