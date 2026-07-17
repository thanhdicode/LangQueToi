using System.IO;
using System.Linq;
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

        [Test]
        public void CalendarDay_FormatsWithVietnameseNumber()
            => Assert.That(Loc.Format("calendar.day", 5), Is.EqualTo("Ngày 5"));

        [Test]
        public void AnimalNeedFeed_InterpolatesItemName()
            => Assert.That(Loc.Format("animal.need_feed", "Bắp"), Is.EqualTo("Cần Bắp!"));

        [Test]
        public void ShopBuyPartial_ContainsGoldAndCount()
            => Assert.That(
                Loc.Format("shop.buy.partial", Loc.Gold(120), 2),
                Is.EqualTo("Đã chi 120 ₫; 2 món chưa giao được."));

        [Test]
        public void ShopBuySuccess_ContainsGoldTotal()
            => Assert.That(
                Loc.Format("shop.buy.success", Loc.Gold(120)),
                Is.EqualTo("Đơn hàng đã giao, tổng cộng 120 ₫."));

        [Test]
        public void KnownEnglishRuntimeLiterals_AreAbsent()
        {
            string[] forbidden =
            {
                "Not time to sleep yet!", "Inventory Full", "Already fed today!",
                "Clove paid you", "Partial order:", "Order delivered!",
                "TOTAL:", "Price:", "Sell:", " Days", "Saving...", "Saved!"
            };

            string projectRoot = Path.GetFullPath(
                Path.Combine(UnityEngine.Application.dataPath, ".."));
            string scriptsRoot = Path.Combine(projectRoot, "Assets", "Scripts");
            string source = string.Join("\n", Directory.GetFiles(
                    scriptsRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}"))
                .Select(File.ReadAllText));

            foreach (string literal in forbidden)
                Assert.That(source, Does.Not.Contain(literal), literal);
        }
    }
}
