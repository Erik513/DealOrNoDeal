using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI;

namespace DealOrNoDeal
{
    /// <summary>
    /// Final step: keep your own case or swap it for the last remaining
    /// case. Two big color-coded buttons instead of the case artwork -
    /// the case images are low-res to begin with, and blow up badly at
    /// any size large enough to read as a clear "click me" target here.
    /// </summary>
    public partial class ucFinalChoice : UserControl
    {
        private static readonly Size ButtonSize = new Size(320, 160);
        private const int ButtonGap = 100;

        private readonly Button btnKeep;
        private readonly Button btnSwap;
        private readonly Label labelPrompt;

        public event EventHandler KeepMyCaseClicked;
        public event EventHandler SwapCaseClicked;

        public ucFinalChoice()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            // Plain gray, not the golden resx background image - a fully
            // gold screen looked bad, so all game-phase controls now share
            // the same neutral background.
            BackColor = UIStyles.Colors.BackgroundMediumElevated;

            TableLayoutPanel main = UIStyles.TableLayoutPanels.CreateStandard(1, 2);
            main.Dock = DockStyle.Fill;
            main.BackColor = Color.Transparent;
            main.RowStyles.Clear();
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
            main.ColumnStyles.Clear();
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // FlowLayoutPanel keeps both buttons right next to each other,
            // with the whole pair then centered as one unit via
            // Anchor=None - same approach the case artwork used before.
            FlowLayoutPanel buttonPair = UIStyles.FlowLayoutPanels.CreateStandard();
            buttonPair.FlowDirection = FlowDirection.LeftToRight;
            buttonPair.Anchor = AnchorStyles.None;

            btnKeep = UIStyles.Buttons.CreateGreen(AppLocalization.Get("FinalChoice.Keep"), size: ButtonSize);
            btnKeep.Margin = new Padding(0, 0, ButtonGap, 0);
            btnKeep.Font = new Font(UIStyles.Fonts.Normal.FontFamily, 22f, FontStyle.Bold);
            btnKeep.Click += BtnKeep_Click;

            btnSwap = UIStyles.Buttons.CreateDanger(AppLocalization.Get("FinalChoice.Swap"), size: ButtonSize);
            btnSwap.Margin = new Padding(0);
            btnSwap.Font = new Font(UIStyles.Fonts.Normal.FontFamily, 22f, FontStyle.Bold);
            btnSwap.Click += BtnSwap_Click;

            buttonPair.Controls.Add(btnKeep);
            buttonPair.Controls.Add(btnSwap);

            Panel hintPanel = UIStyles.Panels.CreateTransparent();
            hintPanel.Dock = DockStyle.Bottom;
            hintPanel.Height = 75;

            labelPrompt = UIStyles.Labels.CreateNormal(AppLocalization.Get("FinalChoice.Prompt"));
            labelPrompt.Dock = DockStyle.Fill;
            labelPrompt.TextAlign = ContentAlignment.MiddleCenter;
            labelPrompt.Font = new Font(UIStyles.Fonts.Title.FontFamily, 15.75f);
            labelPrompt.ForeColor = UIStyles.Colors.YellowLighter;
            hintPanel.Controls.Add(labelPrompt);

            main.Controls.Add(buttonPair, 0, 0);
            main.Controls.Add(hintPanel, 0, 1);

            Controls.Add(main);
        }

        public void RefreshLanguage()
        {
            labelPrompt.Text = AppLocalization.Get("FinalChoice.Prompt");
            btnKeep.Text = AppLocalization.Get("FinalChoice.Keep");
            btnSwap.Text = AppLocalization.Get("FinalChoice.Swap");
        }

        private void BtnKeep_Click(object sender, EventArgs e)
        {
            KeepMyCaseClicked?.Invoke(this, EventArgs.Empty);
        }

        private void BtnSwap_Click(object sender, EventArgs e)
        {
            SwapCaseClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
