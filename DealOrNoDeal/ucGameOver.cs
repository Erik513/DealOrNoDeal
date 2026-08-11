using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI;

namespace DealOrNoDeal
{
    /// <summary>
    /// Final screen: shows the winnings and offers a restart. Uses the same
    /// plain gray background as the other game-phase controls. The amount
    /// is the visual focus - "Du hast" / AMOUNT (large, gold) / "gewonnen!"
    /// instead of one plain sentence.
    /// </summary>
    public partial class ucGameOver : UserControl
    {
        private readonly Label labelPrefix;
        private readonly Label labelAmount;
        private readonly Label labelSuffix;
        private readonly Label labelOwnCase;
        private readonly Button btnRestart;

        // Remembered so RefreshLanguage can rebuild the "...contained: {0}"
        // sentence in the new language without losing the amount.
        private string ownCaseAmountText;

        public event EventHandler RestartClicked;

        public ucGameOver()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = UIStyles.Colors.BackgroundMediumElevated;

            // Prefix/suffix get small, fixed-height rows right against the
            // amount (Dock=Bottom/Top so they hug its edges), instead of
            // large percentage rows that centered them with lots of empty
            // space around them - the amount is the focus, "Du hast" and
            // "gewonnen!" are just its immediate frame.
            TableLayoutPanel main = UIStyles.TableLayoutPanels.CreateStandard(1, 7);
            main.Dock = DockStyle.Fill;
            main.BackColor = Color.Transparent;
            main.RowStyles.Clear();
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
            main.ColumnStyles.Clear();
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // AutoSize rows + AutoSize labels (Anchor=None centers a
            // AutoSize control in its TableLayoutPanel cell) size each row
            // to exactly fit its text, regardless of font/DPI - a fixed
            // pixel row height clipped these at some sizes.
            labelPrefix = UIStyles.Labels.CreateTitle(AppLocalization.Get("GameOver.Prefix"));
            labelPrefix.AutoSize = true;
            labelPrefix.Anchor = AnchorStyles.None;
            labelPrefix.BackColor = Color.Transparent;
            labelPrefix.Font = new Font(UIStyles.Fonts.Title.FontFamily, 26f, FontStyle.Regular);

            labelAmount = UIStyles.Labels.CreateTitle("");
            labelAmount.AutoSize = true;
            labelAmount.Anchor = AnchorStyles.None;
            labelAmount.BackColor = Color.Transparent;
            labelAmount.ForeColor = Color.Gold;
            labelAmount.AutoEllipsis = false;
            labelAmount.Font = new Font(UIStyles.Fonts.Title.FontFamily, 66f, FontStyle.Bold);

            labelSuffix = UIStyles.Labels.CreateTitle(AppLocalization.Get("GameOver.Suffix"));
            labelSuffix.AutoSize = true;
            labelSuffix.Anchor = AnchorStyles.None;
            labelSuffix.BackColor = Color.Transparent;
            labelSuffix.Font = new Font(UIStyles.Fonts.Title.FontFamily, 26f, FontStyle.Regular);

            // Secondary line: what was actually in the player's own case -
            // shown no matter how the game ended (kept, swapped, or a
            // banker offer accepted), since accepting an offer never
            // otherwise reveals it.
            labelOwnCase = UIStyles.Labels.CreateMuted("");
            labelOwnCase.AutoSize = true;
            labelOwnCase.Anchor = AnchorStyles.None;
            labelOwnCase.BackColor = Color.Transparent;
            labelOwnCase.Font = new Font(UIStyles.Fonts.Normal.FontFamily, 15f);
            labelOwnCase.Margin = new Padding(0, 20, 0, 0);

            btnRestart = UIStyles.Buttons.CreateGreen(AppLocalization.Get("GameOver.Restart"));
            btnRestart.Anchor = AnchorStyles.None;
            btnRestart.Size = new Size(280, 64);
            btnRestart.Font = new Font(UIStyles.Fonts.Normal.FontFamily, 16f, FontStyle.Bold);
            btnRestart.Click += (s, e) => RestartClicked?.Invoke(this, EventArgs.Empty);

            main.Controls.Add(labelPrefix, 0, 1);
            main.Controls.Add(labelAmount, 0, 2);
            main.Controls.Add(labelSuffix, 0, 3);
            main.Controls.Add(labelOwnCase, 0, 4);
            main.Controls.Add(btnRestart, 0, 6);

            Controls.Add(main);
        }

        public void ShowResult(string amountText)
        {
            labelAmount.Text = amountText;
        }

        public void ShowOwnCaseAmount(string amountText)
        {
            ownCaseAmountText = amountText;
            labelOwnCase.Text = AppLocalization.Get("GameOver.OwnCaseContained", amountText);
            labelOwnCase.Visible = true;
        }

        public void HideOwnCaseAmount()
        {
            ownCaseAmountText = null;
            labelOwnCase.Visible = false;
        }

        public void RefreshLanguage()
        {
            labelPrefix.Text = AppLocalization.Get("GameOver.Prefix");
            labelSuffix.Text = AppLocalization.Get("GameOver.Suffix");
            btnRestart.Text = AppLocalization.Get("GameOver.Restart");

            if (ownCaseAmountText != null)
                labelOwnCase.Text = AppLocalization.Get("GameOver.OwnCaseContained", ownCaseAmountText);
        }
    }
}
