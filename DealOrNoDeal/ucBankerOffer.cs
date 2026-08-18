using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI;

namespace DealOrNoDeal
{
    /// <summary>
    /// Shows the banker's offer with an accept/decline choice.
    /// Layout is fully Dock/percentage-based so it scales correctly at any
    /// size of the parent area.
    /// </summary>
    public partial class ucBankerOffer : UserControl
    {
        private readonly PictureBox pboxOfferImage;
        private readonly Label labelOfferHeading;
        private readonly Label labelOfferAmount;
        private readonly Label labelCasesUntilNext;
        private readonly PictureBox pboxAcceptOffer;
        private readonly PictureBox pboxDeclineOffer;
        private readonly Panel panelCalculating;
        private readonly Label labelCalculating;
        private readonly Label labelCalculatingSkipHint;
        private readonly Timer revealTimer;

        // Whether labelOfferAmount currently shows a real offer (set from
        // outside via SetOfferAmountText) rather than the placeholder text -
        // RefreshLanguage must only touch the placeholder, never overwrite
        // an actual offer amount with translated placeholder text.
        private bool hasRealOffer;

        // Null means "this was the last offer, decline leads straight to
        // the final choice" - remembered (like hasRealOffer above) so
        // RefreshLanguage can rebuild the same hint in a new language.
        private int? casesUntilNextOffer;
        private bool hasCasesInfo;

        // Guards against both the timer and a click firing the reveal -
        // whichever happens first wins, the other must be a no-op.
        private bool revealed;

        public event Action OfferDeclined;
        public event Action<string> OfferAccepted;

        /// <summary>
        /// Fires once the accept/decline choice actually becomes usable -
        /// either the reveal delay ran out or the player clicked to skip
        /// it. The caller uses this to switch its own "banker is
        /// calculating" status text over to "accept or continue?", since
        /// before this fires the buttons underneath are still hidden.
        /// </summary>
        public event Action OfferRevealed;

        public ucBankerOffer()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = UIStyles.Colors.BackgroundMediumElevated;

            ComponentResourceManager resources = new ComponentResourceManager(typeof(ucBankerOffer));

            TableLayoutPanel main = UIStyles.TableLayoutPanels.CreateStandard(1, 2);
            main.Dock = DockStyle.Fill;
            main.BackColor = Color.Transparent;
            main.RowStyles.Clear();
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 80));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            main.ColumnStyles.Clear();
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            pboxOfferImage = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = (Image)resources.GetObject("pboxOfferBild.Image")
            };

            labelOfferHeading = UIStyles.Labels.CreateTitle("Deal Or No Deal ?");
            labelOfferHeading.Dock = DockStyle.Top;
            labelOfferHeading.Height = 80;
            labelOfferHeading.TextAlign = ContentAlignment.MiddleCenter;
            labelOfferHeading.BackColor = Color.Transparent;
            labelOfferHeading.Font = new Font(UIStyles.Fonts.Title.FontFamily, 26f, FontStyle.Bold);
            pboxOfferImage.Controls.Add(labelOfferHeading);

            TableLayoutPanel actionBar = UIStyles.TableLayoutPanels.CreateStandard(3, 1);
            actionBar.Dock = DockStyle.Fill;
            actionBar.ColumnStyles.Clear();
            actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            pboxAcceptOffer = new PictureBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = UIStyles.Colors.Green,
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
                Image = (Image)resources.GetObject("pboxAngebotAnnehmen.Image")
            };
            pboxAcceptOffer.Click += PboxAcceptOffer_Click;

            // AutoSize rows + AutoSize labels with Anchor=None (same
            // vertical-centering idiom as ucGameOver) instead of Dock=Fill -
            // that stretched the hint down to the whole cell's bottom edge,
            // far below the amount instead of sitting right under it.
            TableLayoutPanel amountCell = UIStyles.TableLayoutPanels.CreateStandard(1, 4);
            amountCell.Dock = DockStyle.Fill;
            amountCell.BackColor = Color.Transparent;
            amountCell.RowStyles.Clear();
            amountCell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            amountCell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            amountCell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            amountCell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            amountCell.ColumnStyles.Clear();
            amountCell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            labelOfferAmount = UIStyles.Labels.CreateTitle(AppLocalization.Get("BankerOffer.Placeholder"));
            labelOfferAmount.AutoSize = true;
            labelOfferAmount.Anchor = AnchorStyles.None;
            labelOfferAmount.TextAlign = ContentAlignment.MiddleCenter;
            labelOfferAmount.BackColor = Color.Transparent;
            labelOfferAmount.Font = new Font(UIStyles.Fonts.Title.FontFamily, 26f, FontStyle.Bold);

            labelCasesUntilNext = UIStyles.Labels.CreateMuted("");
            labelCasesUntilNext.AutoSize = true;
            labelCasesUntilNext.Anchor = AnchorStyles.None;
            labelCasesUntilNext.Margin = new Padding(0, 2, 0, 0);
            labelCasesUntilNext.TextAlign = ContentAlignment.MiddleCenter;
            labelCasesUntilNext.BackColor = Color.Transparent;
            labelCasesUntilNext.Font = new Font(UIStyles.Fonts.Normal.FontFamily, 11f);

            amountCell.Controls.Add(labelOfferAmount, 0, 1);
            amountCell.Controls.Add(labelCasesUntilNext, 0, 2);

            pboxDeclineOffer = new PictureBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = UIStyles.Colors.Red,
                SizeMode = PictureBoxSizeMode.Zoom,
                Cursor = Cursors.Hand,
                Image = (Image)resources.GetObject("pboxAngebotAblehnen.Image")
            };
            pboxDeclineOffer.Click += PboxDeclineOffer_Click;

            actionBar.Controls.Add(pboxAcceptOffer, 0, 0);
            actionBar.Controls.Add(amountCell, 1, 0);
            actionBar.Controls.Add(pboxDeclineOffer, 2, 0);

            // Covers actionBar (both Dock=Fill, stacked in the same plain
            // Panel instead of a shared TableLayoutPanel cell - the same
            // "intentionally overlap" trick DealOrNoDeal.cs already uses
            // for pboxOfferCover/txtOfferLog) so the accept/decline choice
            // stays hidden - and unclickable - until the reveal delay is
            // over or the player clicks to skip it.
            labelCalculating = UIStyles.Labels.CreateTitle(AppLocalization.Get("Game.BankerCalculating"));
            labelCalculating.AutoSize = true;
            labelCalculating.Anchor = AnchorStyles.None;
            labelCalculating.TextAlign = ContentAlignment.MiddleCenter;
            labelCalculating.BackColor = Color.Transparent;
            labelCalculating.Font = new Font(UIStyles.Fonts.Title.FontFamily, 20f, FontStyle.Bold);

            TableLayoutPanel calculatingLayout = UIStyles.TableLayoutPanels.CreateStandard(1, 3);
            calculatingLayout.Dock = DockStyle.Fill;
            calculatingLayout.BackColor = Color.Transparent;
            calculatingLayout.RowStyles.Clear();
            calculatingLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            calculatingLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            calculatingLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            calculatingLayout.ColumnStyles.Clear();
            calculatingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            calculatingLayout.Controls.Add(labelCalculating, 0, 1);

            // Same footer style/position as ucOpenCase's "click to
            // skip"/"click to close" prompt - plain gray, docked to the
            // very bottom of the control - instead of sitting right under
            // the calculating text like a caption.
            labelCalculatingSkipHint = UIStyles.Labels.CreateNormal(AppLocalization.Get("BankerOffer.ClickToSkip"));
            labelCalculatingSkipHint.Dock = DockStyle.Bottom;
            labelCalculatingSkipHint.Height = 32;
            labelCalculatingSkipHint.TextAlign = ContentAlignment.MiddleCenter;
            labelCalculatingSkipHint.BackColor = Color.Transparent;
            labelCalculatingSkipHint.ForeColor = UIStyles.Colors.TextMuted;
            labelCalculatingSkipHint.Font = new Font(UIStyles.Fonts.Normal.FontFamily, 11f);

            panelCalculating = UIStyles.Panels.CreateElevated();
            panelCalculating.Dock = DockStyle.Fill;
            // Same plain gray as ucOpenCase's own background - olive made
            // the gray "click to skip" hint hard to read (too little
            // contrast between the two).
            panelCalculating.BackColor = UIStyles.Colors.BackgroundMediumElevated;
            panelCalculating.Cursor = Cursors.Hand;
            panelCalculating.Controls.Add(calculatingLayout);
            panelCalculating.Controls.Add(labelCalculatingSkipHint);
            panelCalculating.Click += (s, e) => RevealOffer();
            calculatingLayout.Click += (s, e) => RevealOffer();
            labelCalculating.Click += (s, e) => RevealOffer();
            labelCalculatingSkipHint.Click += (s, e) => RevealOffer();

            Panel actionBarHost = UIStyles.Panels.CreateTransparent();
            actionBarHost.Dock = DockStyle.Fill;
            actionBarHost.Controls.Add(actionBar);
            actionBarHost.Controls.Add(panelCalculating);
            panelCalculating.BringToFront();

            revealTimer = new Timer();
            revealTimer.Tick += (s, e) => RevealOffer();

            main.Controls.Add(pboxOfferImage, 0, 0);
            main.Controls.Add(actionBarHost, 0, 1);

            Controls.Add(main);
        }

        /// <summary>
        /// Starts (or restarts) the suspense delay before the accept/decline
        /// choice becomes visible and usable - call after SetOfferAmountText
        /// and SetCasesUntilNextOffer so they're already showing the moment
        /// the reveal happens, whether that's from the timer or a skip click.
        /// </summary>
        public void BeginRevealDelay(int delayMs)
        {
            revealed = false;
            panelCalculating.Visible = true;
            panelCalculating.BringToFront();

            revealTimer.Stop();
            revealTimer.Interval = Math.Max(1, delayMs);
            revealTimer.Start();
        }

        private void RevealOffer()
        {
            if (revealed)
                return;

            revealed = true;
            revealTimer.Stop();
            panelCalculating.Visible = false;
            OfferRevealed?.Invoke();
        }

        public void SetOfferAmountText(string offer)
        {
            // Already fully formatted (symbol included) by
            // AppCurrencyFormatter on the caller's side.
            labelOfferAmount.Text = offer;
            hasRealOffer = true;
        }

        /// <summary>
        /// Small hint under the offer amount - either how many more cases
        /// remain until the next offer, or (passing null) that this was the
        /// final offer, since declining it leads straight to the final
        /// keep-or-swap choice instead of another round of case-opening.
        /// </summary>
        public void SetCasesUntilNextOffer(int? casesUntilNext)
        {
            casesUntilNextOffer = casesUntilNext;
            hasCasesInfo = true;
            UpdateCasesUntilNextLabel();
        }

        /// <summary>
        /// Re-applies the current language's text - only the "Angebot"/
        /// "Offer" placeholder, never a real offer amount already showing.
        /// </summary>
        public void RefreshLanguage()
        {
            if (!hasRealOffer)
                labelOfferAmount.Text = AppLocalization.Get("BankerOffer.Placeholder");

            if (hasCasesInfo)
                UpdateCasesUntilNextLabel();

            labelCalculating.Text = AppLocalization.Get("Game.BankerCalculating");
            labelCalculatingSkipHint.Text = AppLocalization.Get("BankerOffer.ClickToSkip");
        }

        private void UpdateCasesUntilNextLabel()
        {
            if (!casesUntilNextOffer.HasValue)
            {
                labelCasesUntilNext.Text = AppLocalization.Get("BankerOffer.FinalOffer");
                return;
            }

            string key = casesUntilNextOffer.Value == 1
                ? "BankerOffer.CaseUntilNextSingular"
                : "BankerOffer.CasesUntilNext";
            labelCasesUntilNext.Text = AppLocalization.Get(key, casesUntilNextOffer.Value);
        }

        private void PboxAcceptOffer_Click(object sender, EventArgs e)
        {
            Visible = false;
            OfferAccepted?.Invoke(labelOfferAmount.Text);
        }

        private void PboxDeclineOffer_Click(object sender, EventArgs e)
        {
            Visible = false;
            OfferDeclined?.Invoke();
        }
    }
}
