using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI;

namespace DealOrNoDeal
{
    /// <summary>
    /// Final step: keep your own case or swap it for the last remaining
    /// case. Purely Dock/percentage-based layout.
    /// </summary>
    public partial class ucFinalChoice : UserControl
    {
        // Both cases reuse the same (fairly low-res) images as the 30-case
        // grid - Dock=Fill previously stretched them to nearly half the
        // screen, way past what that source resolution can hold without
        // going blurry. A fixed size only a bit larger than the grid's own
        // case size keeps them sharp.
        private static readonly Size CaseSize = new Size(200, 200);
        private const int CaseGap = 200;

        private readonly PictureBox pboxMyCase;
        private readonly PictureBox pboxRemainingCase;
        private readonly Label labelPrompt;

        public Image MyCaseImage
        {
            get { return pboxMyCase.Image; }
            set { pboxMyCase.Image = value; }
        }

        /// <summary>
        /// Image of the last remaining, unopened case. Must be set by the
        /// caller every round - without it, this only ever showed a static
        /// placeholder image from the resx.
        /// </summary>
        public Image RemainingCaseImage
        {
            get { return pboxRemainingCase.Image; }
            set { pboxRemainingCase.Image = value; }
        }

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

            ComponentResourceManager resources = new ComponentResourceManager(typeof(ucFinalChoice));

            TableLayoutPanel main = UIStyles.TableLayoutPanels.CreateStandard(1, 2);
            main.Dock = DockStyle.Fill;
            main.BackColor = Color.Transparent;
            main.RowStyles.Clear();
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
            main.ColumnStyles.Clear();
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // FlowLayoutPanel (instead of two independent 50%-wide columns,
            // which each centered their case far apart on wide windows)
            // keeps both cases right next to each other, with the whole
            // pair then centered as one unit via Anchor=None.
            FlowLayoutPanel casePair = UIStyles.FlowLayoutPanels.CreateStandard();
            casePair.FlowDirection = FlowDirection.LeftToRight;
            casePair.Anchor = AnchorStyles.None;

            pboxMyCase = new PictureBox
            {
                Size = CaseSize,
                Margin = new Padding(0, 0, CaseGap, 0),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand
            };
            pboxMyCase.Click += PboxMyCase_Click;

            pboxRemainingCase = new PictureBox
            {
                Size = CaseSize,
                Margin = new Padding(0),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
                Image = (Image)resources.GetObject("pboxLetzterKoffer.Image")
            };
            pboxRemainingCase.Click += PboxRemainingCase_Click;

            casePair.Controls.Add(pboxMyCase);
            casePair.Controls.Add(pboxRemainingCase);

            Panel hintPanel = UIStyles.Panels.CreateTransparent();
            hintPanel.Dock = DockStyle.Bottom;
            hintPanel.Height = 75;

            labelPrompt = UIStyles.Labels.CreateNormal(AppLocalization.Get("FinalChoice.Prompt"));
            labelPrompt.Dock = DockStyle.Fill;
            labelPrompt.TextAlign = ContentAlignment.MiddleCenter;
            labelPrompt.Font = new Font(UIStyles.Fonts.Title.FontFamily, 15.75f);
            labelPrompt.ForeColor = UIStyles.Colors.YellowLighter;
            hintPanel.Controls.Add(labelPrompt);

            main.Controls.Add(casePair, 0, 0);
            main.Controls.Add(hintPanel, 0, 1);

            Controls.Add(main);
        }

        public void RefreshLanguage()
        {
            labelPrompt.Text = AppLocalization.Get("FinalChoice.Prompt");
        }

        private void PboxMyCase_Click(object sender, EventArgs e)
        {
            KeepMyCaseClicked?.Invoke(this, EventArgs.Empty);
        }

        private void PboxRemainingCase_Click(object sender, EventArgs e)
        {
            SwapCaseClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
