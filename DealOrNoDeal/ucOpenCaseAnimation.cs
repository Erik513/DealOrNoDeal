using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI;

namespace DealOrNoDeal
{
    /// <summary>
    /// Shows the case-opening reveal animation: the closed case grows
    /// centered (coming toward the viewer), then swaps to a real opened-case
    /// image together with the amount. Reused for every opened case (not
    /// recreated each time).
    /// </summary>
    public partial class ucOpenCase : UserControl
    {
        // Target size and growth relative to the control's actual
        // size instead of hardcoded pixel values - stays correct at any
        // window size. Bigger than a plain "reveal" size so the case
        // clearly reads as coming closer to the viewer before it opens.
        private const float AnimationTargetFraction = 0.58f;
        private const float NormalScaleFactor = 1.025f;
        private const int NormalIntervalMs = 10;

        // Used for the dramatic final reveal (keep/swap decision) - much
        // slower growth so the moment gets to build suspense instead of
        // flashing by like an ordinary mid-game case.
        private const float SlowScaleFactor = 1.007f;
        private const int SlowIntervalMs = 20;

        // Once revealed, the opened case sits a bit lower than dead-center -
        // looked more natural than perfectly centered.
        private const float OpenCaseVerticalOffsetFraction = 0.10f;

        private static readonly Size AnimationStartSize = new Size(80, 80);

        private readonly PictureBox pictureBoxCase;
        private readonly Image caseClosedImage;
        private readonly Image caseOpenImage;
        private readonly Label labelCaseAmount;
        private readonly Label labelClickToClose;
        private readonly Timer animationTimer;

        private bool animationIsOver;
        private SizeF currentSize;

        public event EventHandler AnimationCompleted;
        public event EventHandler CaseOpenedCompleted;

        /// <summary>
        /// When set before making this control visible, the reveal grows
        /// much more slowly (used for the final keep/swap decision instead
        /// of the ordinary, quick mid-game case reveal).
        /// </summary>
        public bool SlowReveal { get; set; }

        public ucOpenCase()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            // Deliberately plain/gray, not the golden spotlight background -
            // that look belongs to ucFinalChoice/ucGameOver only. Applying it
            // here too made the ordinary case-opening reveal look wrong.
            BackColor = UIStyles.Colors.BackgroundMediumElevated;

            caseClosedImage = LoadEmbeddedImage("DealOrNoDeal.CaseClosed.png");
            caseOpenImage = LoadEmbeddedImage("DealOrNoDeal.CaseOpen.png");

            pictureBoxCase = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Image = caseClosedImage,
                Size = AnimationStartSize
            };
            pictureBoxCase.Click += UcOpenCase_Click;
            Click += UcOpenCase_Click;

            labelCaseAmount = UIStyles.Labels.CreateTitle("1.000.000€");
            labelCaseAmount.Dock = DockStyle.None;
            labelCaseAmount.AutoSize = true;
            labelCaseAmount.Font = new Font(UIStyles.Fonts.Title.FontFamily, 24f, FontStyle.Bold);
            // Plain bright yellow, matching the "still in play" amount
            // buttons in the side strips - UIStyles.Colors.Yellow is a much
            // darker amber/orange tone and looked out of place here.
            labelCaseAmount.BackColor = GameColors.AmountInPlay;
            labelCaseAmount.ForeColor = UIStyles.Colors.BackgroundBlack;
            labelCaseAmount.Padding = new Padding(16, 8, 16, 8);
            labelCaseAmount.Visible = false;
            labelCaseAmount.Click += UcOpenCase_Click;

            labelClickToClose = ClickHint.Create(AppLocalization.Get(TextKeys.OpenCase.ClickToSkip), UcOpenCase_Click);
            labelClickToClose.Visible = false;

            Controls.Add(pictureBoxCase);
            Controls.Add(labelClickToClose);
            Controls.Add(labelCaseAmount);

            animationTimer = new Timer { Interval = NormalIntervalMs };
            animationTimer.Tick += AnimationTimer_Tick;

            VisibleChanged += UcOpenCase_VisibleChanged;
            Resize += UcOpenCase_Resize;
        }

        private static Image LoadEmbeddedImage(string logicalResourceName)
        {
            Assembly assembly = typeof(ucOpenCase).Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(logicalResourceName))
                return Image.FromStream(stream);
        }

        public void SetCaseAmount(string amountText)
        {
            labelCaseAmount.Text = amountText;
        }

        public void RefreshLanguage()
        {
            labelClickToClose.Text = AppLocalization.Get(
                animationIsOver ? TextKeys.OpenCase.ClickToClose : TextKeys.OpenCase.ClickToSkip);
        }

        private void UcOpenCase_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                ResetForNewAnimation();
                StartAnimation();
            }
            else
            {
                StopAnimation();
            }
        }

        private void UcOpenCase_Resize(object sender, EventArgs e)
        {
            CenterPictureBox();
            CenterAmountLabel();
        }

        private void ResetForNewAnimation()
        {
            // Cleared here (not just in StartAnimation right after) so the
            // CenterPictureBox() call below positions the small closed case
            // dead-center, not with the reveal's downward offset left over
            // from the end of the previous round.
            animationIsOver = false;

            currentSize = AnimationStartSize;
            pictureBoxCase.Size = AnimationStartSize;
            pictureBoxCase.Image = caseClosedImage;
            CenterPictureBox();
            labelCaseAmount.Visible = false;

            // Visible for the whole animation (not just after the reveal),
            // so the player knows from the start that clicking skips ahead.
            labelClickToClose.Text = AppLocalization.Get(TextKeys.OpenCase.ClickToSkip);
            labelClickToClose.Visible = true;
            labelClickToClose.BringToFront();
        }

        private void StopAnimation()
        {
            animationIsOver = true;
            animationTimer.Stop();
        }

        private void StartAnimation()
        {
            animationIsOver = false;
            animationTimer.Interval = SlowReveal ? SlowIntervalMs : NormalIntervalMs;
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            float target = Math.Min(Width, Height) * AnimationTargetFraction;
            float scaleFactor = SlowReveal ? SlowScaleFactor : NormalScaleFactor;

            if (currentSize.Width < target && target > 0)
            {
                currentSize = new SizeF(currentSize.Width * scaleFactor, currentSize.Height * scaleFactor);
                pictureBoxCase.Size = currentSize.ToSize();
                CenterPictureBox();
            }
            else
            {
                ShowRevealed();
            }
        }

        /// <summary>
        /// Jumps straight to the fully-grown, revealed state - used both
        /// when the animation finishes on its own and when the player
        /// clicks to skip it. Swaps to the real opened-case image together
        /// with the amount.
        /// </summary>
        private void ShowRevealed()
        {
            StopAnimation();
            pictureBoxCase.Image = caseOpenImage;
            // Re-center now that animationIsOver is true, so the downward
            // offset for the opened case actually takes effect.
            CenterPictureBox();
            labelClickToClose.Text = AppLocalization.Get(TextKeys.OpenCase.ClickToClose);
            labelClickToClose.Visible = true;
            labelClickToClose.BringToFront();
            labelCaseAmount.Visible = true;
            CenterAmountLabel();
            labelCaseAmount.BringToFront();
            AnimationCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void SkipAnimation()
        {
            float target = Math.Min(Width, Height) * AnimationTargetFraction;
            currentSize = new SizeF(target, target);
            pictureBoxCase.Size = currentSize.ToSize();
            CenterPictureBox();
            ShowRevealed();
        }

        private void CenterPictureBox()
        {
            int y = (Height - pictureBoxCase.Height) / 2;

            if (animationIsOver)
                y += (int)(Height * OpenCaseVerticalOffsetFraction);

            pictureBoxCase.Location = new Point((Width - pictureBoxCase.Width) / 2, y);
        }

        private void CenterAmountLabel()
        {
            // Centered on the case itself, not on the whole control - the
            // open case sits below dead-center (OpenCaseVerticalOffsetFraction),
            // so centering on the control would leave the amount floating
            // above it instead of on top of it.
            labelCaseAmount.Location = new Point(
                pictureBoxCase.Left + (pictureBoxCase.Width - labelCaseAmount.Width) / 2,
                pictureBoxCase.Top + (pictureBoxCase.Height - labelCaseAmount.Height) / 2);
        }

        private void CloseReveal()
        {
            if (!animationIsOver)
                return;

            Visible = false;
            CaseOpenedCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void UcOpenCase_Click(object sender, EventArgs e)
        {
            if (!animationIsOver)
            {
                SkipAnimation();
                return;
            }

            CloseReveal();
        }
    }
}
