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
    /// is the visual focus - "Your winnings:" / AMOUNT (large, gold) -
    /// instead of one plain sentence (a literal "You have X won!"
    /// word-for-word translation doesn't read as valid English).
    /// </summary>
    public partial class ucGameOver : UserControl
    {
        private readonly Label labelPrefix;
        private readonly Label labelAmount;
        private readonly Label labelOwnCase;
        private readonly Button btnRestart;
        private readonly Button btnMainMenu;

        // Remembered so RefreshLanguage can rebuild the "...contained: {0}"
        // sentence in the new language without losing the amount.
        private string ownCaseAmountText;

        public event EventHandler RestartClicked;
        public event EventHandler MainMenuClicked;

        public ucGameOver()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = UIStyles.Colors.BackgroundMediumElevated;

            // Prefix gets a small, fixed-height row right against the
            // amount (instead of a large percentage row that centered it
            // with lots of empty space around it) - the amount is the
            // focus, "Your winnings:" is just its immediate frame.
            TableLayoutPanel main = UIStyles.TableLayoutPanels.CreateStandard(1, 6);
            main.Dock = DockStyle.Fill;
            main.BackColor = Color.Transparent;
            main.RowStyles.Clear();
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
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
            // Plain gold, not a UIStyles.Colors tone - deliberate deviation
            // matching the game's own gold/yellow case artwork and amount
            // buttons, same reasoning as ucOpenCase's Color.Yellow choice.
            labelAmount.ForeColor = Color.Gold;
            labelAmount.AutoEllipsis = false;
            labelAmount.Font = new Font(UIStyles.Fonts.Title.FontFamily, 66f, FontStyle.Bold);

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

            // FlowLayoutPanel + Anchor=None (AutoSize=true by default, see
            // UIFlowLayoutPanelFactory) centers the whole button pair as
            // one unit - same technique ucFinalChoice/ucHomeScreen use for
            // their own button pairs.
            FlowLayoutPanel buttonRow = UIStyles.FlowLayoutPanels.CreateStandard();
            buttonRow.FlowDirection = FlowDirection.LeftToRight;
            buttonRow.Anchor = AnchorStyles.None;

            btnMainMenu = UIStyles.Buttons.CreateStandard(AppLocalization.Get("GameOver.MainMenu"), size: new Size(200, 64));
            btnMainMenu.Margin = new Padding(0, 0, 20, 0);
            btnMainMenu.Font = new Font(UIStyles.Fonts.Normal.FontFamily, 16f, FontStyle.Bold);
            btnMainMenu.Click += (s, e) => MainMenuClicked?.Invoke(this, EventArgs.Empty);

            btnRestart = UIStyles.Buttons.CreatePrimary(AppLocalization.Get("GameOver.Restart"), size: new Size(200, 64));
            btnRestart.Margin = new Padding(0);
            btnRestart.Font = new Font(UIStyles.Fonts.Normal.FontFamily, 16f, FontStyle.Bold);
            btnRestart.Click += (s, e) => RestartClicked?.Invoke(this, EventArgs.Empty);

            buttonRow.Controls.Add(btnMainMenu);
            buttonRow.Controls.Add(btnRestart);

            main.Controls.Add(labelPrefix, 0, 1);
            main.Controls.Add(labelAmount, 0, 2);
            main.Controls.Add(labelOwnCase, 0, 3);
            main.Controls.Add(buttonRow, 0, 5);

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
            btnRestart.Text = AppLocalization.Get("GameOver.Restart");
            btnMainMenu.Text = AppLocalization.Get("GameOver.MainMenu");

            if (ownCaseAmountText != null)
                labelOwnCase.Text = AppLocalization.Get("GameOver.OwnCaseContained", ownCaseAmountText);
        }
    }
}
