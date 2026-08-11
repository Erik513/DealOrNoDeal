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

            TableLayoutPanel casePair = UIStyles.TableLayoutPanels.CreateStandard(2, 1);
            casePair.Dock = DockStyle.Fill;
            casePair.BackColor = Color.Transparent;
            casePair.Padding = new Padding(140, 90, 140, 90);
            casePair.ColumnStyles.Clear();
            casePair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            casePair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            pboxMyCase = new PictureBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(70),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand
            };
            pboxMyCase.Click += PboxMyCase_Click;

            pboxRemainingCase = new PictureBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(70),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
                Image = (Image)resources.GetObject("pboxLetzterKoffer.Image")
            };
            pboxRemainingCase.Click += PboxRemainingCase_Click;

            casePair.Controls.Add(pboxMyCase, 0, 0);
            casePair.Controls.Add(pboxRemainingCase, 1, 0);

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
