using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI;

namespace DealOrNoDeal
{
    /// <summary>
    /// The plain gray "click to skip / close" footer shared by the
    /// case-opening reveal and the banker-offer screen: one label docked to
    /// the very bottom of its control.
    /// </summary>
    internal static class ClickHint
    {
        public static Label Create(string text, EventHandler onClick)
        {
            Label hint = UIStyles.Labels.CreateStandard(text);
            hint.Dock = DockStyle.Bottom;
            hint.Height = 46;
            hint.TextAlign = ContentAlignment.MiddleCenter;
            hint.BackColor = Color.Transparent;
            hint.ForeColor = UIStyles.Colors.TextMuted;
            hint.Font = new Font(UIStyles.Fonts.Normal.FontFamily, 12.75f);
            hint.Click += onClick;
            return hint;
        }
    }
}
