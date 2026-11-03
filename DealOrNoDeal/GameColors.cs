using System.Drawing;

namespace DealOrNoDeal
{
    /// <summary>
    /// The game's own look - the fixed gold/yellow of the show - in one
    /// place. Deliberately plain colors and not the library's theme colors:
    /// the case artwork, the amount buttons and the gold frames are one
    /// design that must look the same in the light and the dark theme.
    /// </summary>
    internal static class GameColors
    {
        /// <summary>The gold frames around the cards and the stage, and gold text on dark.</summary>
        public static readonly Color Gold = Color.Gold;

        /// <summary>The dark gray inside the cards, the case grid and the offer log.</summary>
        public static readonly Color CardBackground = Color.FromArgb(64, 64, 64);

        /// <summary>
        /// An amount still in play, and the amount shown on an opened case -
        /// plain bright yellow, UIStyles.Colors.Yellow is a much darker
        /// amber/orange tone and looked out of place next to the case art.
        /// </summary>
        public static readonly Color AmountInPlay = Color.Yellow;

        /// <summary>An amount that is out of the game (its case was opened).</summary>
        public static readonly Color AmountOpened = Color.Olive;

        /// <summary>Text on an amount button.</summary>
        public static readonly Color AmountText = Color.Black;
    }
}
