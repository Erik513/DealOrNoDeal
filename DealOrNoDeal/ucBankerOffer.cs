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
        private readonly PictureBox pboxAcceptOffer;
        private readonly PictureBox pboxDeclineOffer;

        // Whether labelOfferAmount currently shows a real offer (set from
        // outside via SetOfferAmountText) rather than the placeholder text -
        // RefreshLanguage must only touch the placeholder, never overwrite
        // an actual offer amount with translated placeholder text.
        private bool hasRealOffer;

        public event Action OfferDeclined;
        public event Action<string> OfferAccepted;

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

            labelOfferAmount = UIStyles.Labels.CreateTitle(AppLocalization.Get("BankerOffer.Placeholder"));
            labelOfferAmount.Dock = DockStyle.Fill;
            labelOfferAmount.TextAlign = ContentAlignment.MiddleCenter;
            labelOfferAmount.BackColor = Color.Transparent;
            labelOfferAmount.Font = new Font(UIStyles.Fonts.Title.FontFamily, 26f, FontStyle.Bold);

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
            actionBar.Controls.Add(labelOfferAmount, 1, 0);
            actionBar.Controls.Add(pboxDeclineOffer, 2, 0);

            main.Controls.Add(pboxOfferImage, 0, 0);
            main.Controls.Add(actionBar, 0, 1);

            Controls.Add(main);
        }

        public void SetOfferAmountText(string offer)
        {
            // Already fully formatted (symbol included) by
            // AppCurrencyFormatter on the caller's side.
            labelOfferAmount.Text = offer;
            hasRealOffer = true;
        }

        /// <summary>
        /// Re-applies the current language's text - only the "Angebot"/
        /// "Offer" placeholder, never a real offer amount already showing.
        /// </summary>
        public void RefreshLanguage()
        {
            if (!hasRealOffer)
                labelOfferAmount.Text = AppLocalization.Get("BankerOffer.Placeholder");
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
