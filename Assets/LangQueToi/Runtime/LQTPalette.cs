using UnityEngine;

namespace LangQueToi
{
    /// <summary>
    /// Invariant color palette for Làng Quê Tôi visual identity (spec Section 5.2).
    /// Do not mutate; every UI/art layer must reference these exact tokens.
    /// Red lantern is reserved — use only for lantern art, not general accents.
    /// </summary>
    public static class LQTPalette
    {
        // Bamboo (primary green family)
        public static readonly Color BambooDark  = Hex(0x16, 0x3E, 0x16);
        public static readonly Color BambooMid   = Hex(0x2D, 0x64, 0x23);
        public static readonly Color BambooLight = Hex(0x4E, 0x94, 0x40);

        // Lacquer (panel body)
        public static readonly Color LacquerBrown  = Hex(0x1C, 0x0C, 0x04);
        public static readonly Color LacquerBorder = Hex(0xC8, 0x9B, 0x26);

        // Aged wood
        public static readonly Color AgedWoodDark  = Hex(0x48, 0x2C, 0x12);
        public static readonly Color AgedWoodMid   = Hex(0x8C, 0x5A, 0x28);
        public static readonly Color AgedWoodLight = Hex(0xB9, 0x82, 0x3E);

        // Gold family
        public static readonly Color MutedGold     = Hex(0xDA, 0xA5, 0x20);
        public static readonly Color GoldHighlight = Hex(0xFF, 0xDC, 0x50);
        public static readonly Color GoldShadow    = Hex(0x8C, 0x64, 0x0A);

        // Cream / text
        public static readonly Color Cream = Hex(0xFF, 0xF5, 0xC3);

        // Sky / paddy / canal
        public static readonly Color SkyHigh    = Hex(0x64, 0xAA, 0xE6);
        public static readonly Color SkyLow     = Hex(0xAF, 0xE0, 0x9B);
        public static readonly Color Paddy      = Hex(0x20, 0x52, 0x20);
        public static readonly Color Canal      = Hex(0x3A, 0x6E, 0x8C);
        public static readonly Color CanalShine = Hex(0x7A, 0xB8, 0xD4);

        // Character
        public static readonly Color Skin        = Hex(0xD0, 0xA5, 0x73);
        public static readonly Color AoBaBaIndigo = Hex(0x2A, 0x28, 0x58);
        public static readonly Color NonLa       = Hex(0xD2, 0xB9, 0x6E);

        // Reserved / restricted
        /// <summary>ONLY use for lantern art (spec 5.2). Do not use as general accent.</summary>
        public static readonly Color RedLantern = Hex(0xC4, 0x1E, 0x1C);

        public static readonly Color CoconutPalm = Hex(0x4A, 0x7A, 0x1E);
        public static readonly Color Shadow      = Hex(0x12, 0x23, 0x0A);

        private static Color Hex(byte r, byte g, byte b) => new Color32(r, g, b, 255);
    }
}
