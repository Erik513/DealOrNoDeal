using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Windows.Forms;
using CustomWFUI;
using CustomWFUI.Forms;
using CustomWFUI.Styles;

namespace DealOrNoDeal
{
    public partial class DealOrNoDeal : StyledForm
    {
        // Money amounts for the 30 cases, in case-1..30 order. Plain
        // numbers, not pre-formatted strings - display text (with the
        // currently selected currency's symbol/format) is generated on
        // demand via AppCurrencyFormatter, so the currency can be switched
        // without touching this list.
        private static readonly decimal[] CaseAmountValues =
        {
            0, 1, 5, 10, 20, 50, 75, 100, 200, 300,
            400, 500, 750, 1000, 2500, 5000, 7500, 10000, 15000, 20000,
            25000, 50000, 75000, 100000, 200000, 300000, 400000, 500000, 750000, 1000000
        };

        // How many cases are left to open once the info text is next shown,
        // keyed by the click count it applies from. Round thresholds match
        // BankerOfferCalculator's offer rounds (plus click 1, the very
        // first case pick, which has no banker offer of its own) - but the
        // remaining-count values themselves are a separate, hand-tuned
        // case-opening schedule with no mathematical link to the offer
        // percentages, so they can't be derived from that table.
        private static readonly Dictionary<int, int> CasesRemainingByRound = new Dictionary<int, int>
        {
            { 1, 7 },
            { 8, 6 },
            { 14, 5 },
            { 19, 4 },
            { 23, 2 },
            { 25, 2 },
            { 27, 1 },
            { 28, 1 },
            { 29, 0 }
        };

        // The two amount-display formats used throughout: whole amounts for
        // the 30 fixed case values, and cents-included for the banker's own
        // computed offer (a percentage of an average, so rarely a round
        // number).
        private const string WholeAmountFormat = "#,0";
        private const string OfferAmountFormat = "#,0.00";

        private readonly List<PictureBox> caseList = new List<PictureBox>();
        private readonly List<Button> buttonList = new List<Button>();

        // Single source of truth for which case holds which amount (index
        // into CaseAmounts/CaseAmountValues) - independent of display
        // properties such as Tag or BackColor.
        private readonly Dictionary<PictureBox, int> caseAmountIndex = new Dictionary<PictureBox, int>();

        // Each case's number (1-30, already printed on the case artwork
        // itself) - used to say which case was cashed in at the end of a
        // game, in the home screen's history table.
        private readonly Dictionary<PictureBox, int> caseNumbers = new Dictionary<PictureBox, int>();

        // Shown in the history table instead of a case number when the
        // game ended by accepting a banker offer - deliberately left
        // untranslated, same as "Deal Or No Deal ?" itself.
        private const string DealResultLabel = "Deal";

        // Indices of the amounts that haven't been revealed yet - basis for
        // the banker's offer average calculation.
        private readonly HashSet<int> remainingAmountIndices = new HashSet<int>();

        private readonly Random random = new Random();

        private static readonly Size OwnCaseSize = new Size(140, 140);

        private const string UpdateRepositoryOwner = "Erik513";
        private const string UpdateRepositoryName = "DealOrNoDeal";
        private static readonly TimeSpan UpdateCheckTimeout = TimeSpan.FromSeconds(5);
        private bool updateCheckStarted;

        private PictureBox selectedCase;
        private int casesClicked;
        private int casesRemaining;
        private Button lastOpenedButton;
        private bool IsGameOver;

        private readonly string videoPath = "C:/Users/uif42535/OneDrive - Continental AG/Dateien/Bilder/Visual Studio/DealOrNoDealKoffer/DealersOffer.mp4";

        // Core controls addressed by the game logic.
        private Panel panelMyCase;
        private TableLayoutPanel caseGridPanel;
        private Label labelMyCaseTitle;
        private Label labelOffersTitle;
        private Label labelInfoText;
        private RichTextBox txtOfferLog;
        private Button btnMainMenu;

        // Raw amounts, not pre-formatted strings - offer log entries and
        // the currently-shown banker offer get re-formatted on the fly
        // when the currency setting changes, instead of freezing whatever
        // format was active when they were first shown.
        private readonly List<decimal> offerHistoryValues = new List<decimal>();
        private decimal? currentOfferAmount;

        private PictureBox pboxOfferCover;
        private AxWMPLib.AxWindowsMediaPlayer axBankerVideoPlayer;
        private ucBankerOffer bankerOfferView;
        private ucOpenCase caseOpeningView;
        private ucFinalChoice finalChoiceView;
        private ucGameOver gameOverView;
        private ucHomeScreen homeScreenView;
        private PictureBox lastRemainingCase;

        // Set only while the final (keep/swap) case reveal is playing, so
        // CaseOpeningView_CaseOpenedCompleted knows to end the game instead
        // of continuing the normal open-more-cases flow once it's dismissed.
        private string pendingFinalResultAmount;
        private decimal pendingFinalResultValue;
        private string pendingFinalResultLabel;

        // True only when the game ended by keeping the original case - in
        // that case its content IS the winnings already shown, so the
        // separate "your own case contained..." line on the game-over
        // screen would just repeat the same number. Stays false (and so
        // the line stays shown) for an accepted banker offer or a swap,
        // where it reveals something not shown anywhere else.
        private bool endedByKeepingOwnCase;

        // Raw winnings amount + the format it was shown with (case amounts
        // use "#,0", banker offers - computed percentages - use the default
        // "#,0.00"), remembered so RefreshCurrency can re-format the
        // game-over screen too instead of leaving it stuck in whatever
        // currency was active when the round ended.
        private decimal wonAmountValue;
        private string wonAmountFormat;
        private decimal? shownOwnCaseAmountValue;

        // Remembered so RefreshLanguage can rebuild whatever labelInfoText
        // is currently showing (it changes constantly during play) in the
        // new language, instead of leaving it stuck in the old one.
        private string currentInfoTextKey;
        private object[] currentInfoTextArgs;

        // Loaded for the custom title bar's icon - the taskbar/Alt-Tab icon
        // comes from the exe's own embedded ApplicationIcon instead
        // (StyledForm falls back to Icon.ExtractAssociatedIcon for that
        // automatically, no code needed here).
        private static Image LoadAppIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DealOrNoDeal.AppIconTitleBar.png"))
                return Image.FromStream(stream);
        }

        public DealOrNoDeal()
            : base(StyledFormOptions.CreateStandard("DealOrNoDeal", titleTextAlign: ContentAlignment.MiddleLeft, icon: LoadAppIcon()))
        {
            InitializeComponent();
            InitializeCaseAmounts();

            SetInfoText("Game.ChooseCase");
            selectedCase = null;

            bankerOfferView.OfferDeclined += HandleOfferDeclined;
            bankerOfferView.OfferAccepted += amount => EndGame(amount, currentOfferAmount ?? 0m, OfferAmountFormat, DealResultLabel);
            bankerOfferView.OfferRevealed += HandleOfferRevealed;
            caseOpeningView.AnimationCompleted += CaseOpeningView_AnimationCompleted;
            caseOpeningView.CaseOpenedCompleted += CaseOpeningView_CaseOpenedCompleted;
            finalChoiceView.KeepMyCaseClicked += (s, e) => RevealFinalCase(selectedCase);
            finalChoiceView.SwapCaseClicked += (s, e) => RevealFinalCase(lastRemainingCase ?? selectedCase);
            gameOverView.RestartClicked += (s, e) => RestartGame();
            gameOverView.MainMenuClicked += (s, e) => ReturnToMainMenu();

            // Live-refresh: both settings only affected newly-built text
            // before - now every already-built control that shows
            // translated/currency-formatted text listens for these and
            // re-applies itself immediately.
            AppLocalization.LanguageChanged += (s, e) => RefreshLanguage();
            AppCurrencyFormatter.CurrencyChanged += (s, e) => RefreshCurrency();
        }

        /// <summary>
        /// Sets labelInfoText and remembers which key/args produced it, so
        /// RefreshLanguage can rebuild the same message in a new language
        /// later - labelInfoText changes constantly during play, so this is
        /// simpler and less error-prone than tracking game phase separately.
        /// </summary>
        private void SetInfoText(string key, params object[] args)
        {
            currentInfoTextKey = key;
            currentInfoTextArgs = args;
            labelInfoText.Text = AppLocalization.Get(key, args);
        }

        /// <summary>
        /// Re-applies the current language to every already-built control
        /// that shows translated text - called once, whenever
        /// AppLocalization.Language changes.
        /// </summary>
        private void RefreshLanguage()
        {
            labelMyCaseTitle.Text = AppLocalization.Get("Game.MyCaseLabel");
            labelOffersTitle.Text = AppLocalization.Get("Game.OffersLabel");

            if (currentInfoTextKey != null)
                labelInfoText.Text = AppLocalization.Get(currentInfoTextKey, currentInfoTextArgs);

            UIStyles.Buttons.UpdateTooltip(btnMainMenu, AppLocalization.Get("Game.MainMenuButton"));

            bankerOfferView.RefreshLanguage();
            finalChoiceView.RefreshLanguage();
            gameOverView.RefreshLanguage();
            caseOpeningView.RefreshLanguage();
            homeScreenView.RefreshLanguage();
        }

        /// <summary>
        /// Re-formats every already-shown amount with the newly selected
        /// currency - called once, whenever AppCurrencyFormatter.Currency
        /// changes. Includes the game-over screen once the round is over,
        /// same as every other still-visible amount.
        /// </summary>
        private void RefreshCurrency()
        {
            // buttonList[i] always corresponds to CaseAmountValues[i] - set
            // that way in BuildAmountStrip and never reordered.
            for (int i = 0; i < buttonList.Count; i++)
                buttonList[i].Text = AppCurrencyFormatter.Format(CaseAmountValues[i], WholeAmountFormat);

            RenderOfferLog();

            if (currentOfferAmount.HasValue)
                bankerOfferView.SetOfferAmountText(AppCurrencyFormatter.Format(currentOfferAmount.Value));

            if (IsGameOver)
            {
                gameOverView.ShowResult(AppCurrencyFormatter.Format(wonAmountValue, wonAmountFormat));

                if (shownOwnCaseAmountValue.HasValue)
                    gameOverView.ShowOwnCaseAmount(AppCurrencyFormatter.Format(shownOwnCaseAmountValue.Value, WholeAmountFormat));
            }

            homeScreenView.RefreshRecordsDisplay();
        }

        private string AmountTextOf(PictureBox caseBox)
        {
            return AppCurrencyFormatter.Format(CaseAmountValues[caseAmountIndex[caseBox]], WholeAmountFormat);
        }

        /// <summary>
        /// The dramatic finale: slowly opens whichever case the player
        /// ends up with (own case kept, or swapped for the last remaining
        /// one) and only shows the game-over summary once that reveal has
        /// been dismissed - instead of jumping straight to a result text.
        /// </summary>
        private void RevealFinalCase(PictureBox caseBox)
        {
            finalChoiceView.Visible = false;
            SetInfoText("Game.YourCase");

            endedByKeepingOwnCase = caseBox == selectedCase;
            pendingFinalResultAmount = AmountTextOf(caseBox);
            pendingFinalResultValue = CaseAmountValues[caseAmountIndex[caseBox]];
            pendingFinalResultLabel = caseNumbers[caseBox].ToString(CultureInfo.InvariantCulture);
            caseOpeningView.SlowReveal = true;
            caseOpeningView.SetCaseAmount(pendingFinalResultAmount);
            caseOpeningView.Visible = true;
            caseOpeningView.BringToFront();
        }

        /// <summary>
        /// Resets all game state and hands every case/amount control back
        /// to its starting look, in place - the window itself never closes
        /// or reopens, only the game state and the cases are restored.
        /// </summary>
        private void RestartGame()
        {
            casesClicked = 0;
            casesRemaining = 0;
            selectedCase = null;
            lastOpenedButton = null;
            lastRemainingCase = null;
            pendingFinalResultAmount = null;
            pendingFinalResultLabel = null;
            endedByKeepingOwnCase = false;
            IsGameOver = false;

            caseAmountIndex.Clear();
            remainingAmountIndices.Clear();
            InitializeCaseAmounts();

            offerHistoryValues.Clear();
            currentOfferAmount = null;
            txtOfferLog.Clear();

            for (int i = 0; i < caseList.Count; i++)
            {
                PictureBox caseBox = caseList[i];

                // Re-adding to the grid with an explicit cell also moves it
                // out of panelMyCase again, if it had been picked as the
                // player's own case last round - but Case_Click also
                // switches that one case to a fixed small Size with
                // Dock=None, which otherwise stuck around and left it
                // tiny and mispositioned back in the grid.
                caseBox.Dock = DockStyle.Fill;
                caseGridPanel.Controls.Add(caseBox, i % 5, i / 5);
                caseBox.Visible = true;
                caseBox.Enabled = true;
            }

            foreach (Button amountButton in buttonList)
            {
                SetAmountButtonColor(amountButton, Color.Yellow);
                amountButton.Enabled = true;
            }

            HideAllGameViews();
            gameOverView.Visible = false;

            SetInfoText("Game.ChooseCase");
        }

        /// <summary>
        /// Hides every overlapping game-phase view sharing the stage host
        /// (banker offer, case-opening reveal, final choice, banker video,
        /// its cover) - not gameOverView, which each caller controls
        /// separately depending on whether it should end up shown or hidden.
        /// </summary>
        private void HideAllGameViews()
        {
            bankerOfferView.Visible = false;
            caseOpeningView.Visible = false;
            finalChoiceView.Visible = false;
            axBankerVideoPlayer.Visible = false;
            pboxOfferCover.Visible = false;
        }

        private void DisableAllCases()
        {
            foreach (PictureBox caseBox in caseList)
                caseBox.Enabled = false;
        }

        /// <summary>
        /// Asks for confirmation before leaving an in-progress game - unlike
        /// the game-over screen's own main-menu button, this one can throw
        /// away real progress, so it needs a way back out of an accidental
        /// click.
        /// </summary>
        private void ConfirmReturnToMainMenu()
        {
            DialogResult result = CustomMessageBox.Show(
                AppLocalization.Get("Game.ConfirmMainMenuMessage"),
                AppLocalization.Get("Game.ConfirmMainMenuTitle"),
                CustomMessageBoxButtons.YesNo,
                CustomMessageBoxIcon.Warning,
                this,
                CustomMessageBoxSize.Small);

            if (result == DialogResult.Yes)
                ReturnToMainMenu();
        }

        private void ReturnToMainMenu()
        {
            RestartGame();
            homeScreenView.Visible = true;
            homeScreenView.BringToFront();
        }

        /// <summary>
        /// Opens the language/currency options dialog. Saving raises
        /// AppLocalization.LanguageChanged/AppCurrencyFormatter.CurrencyChanged,
        /// which RefreshLanguage/RefreshCurrency (wired up in the
        /// constructor) react to immediately - no restart needed.
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // OnShown can fire more than once (e.g. after a Hide()/Show()
            // cycle elsewhere), but the update check should only ever run
            // once per launch.
            if (updateCheckStarted)
                return;

            updateCheckStarted = true;

            CheckForUpdatesAsync();
        }

        private async void CheckForUpdatesAsync()
        {
            using (HttpClient checkHttpClient = new HttpClient())
            using (HttpClient downloadHttpClient = new HttpClient())
            {
                AppUpdater updater = new AppUpdater(
                    UpdateRepositoryOwner,
                    UpdateRepositoryName,
                    checkHttpClient,
                    downloadHttpClient);

                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                await updater.CheckForUpdateAsync(currentVersion, UpdateCheckTimeout, this);
            }
        }

        // Dragging the window edge makes Windows send a lot of WM_SIZE
        // messages in quick succession. Without this guard, all ~90 controls
        // (case grid, amount strips, nested TableLayoutPanels, the ActiveX
        // video element) would be re-laid-out on EVERY single one of them -
        // that was the cause of the heavy stutter. Now the layout is only
        // rebuilt once, at the end of the resize.
        protected override void OnResizeBegin(EventArgs e)
        {
            base.OnResizeBegin(e);
            SetLayoutSuspended(this, true);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            SetLayoutSuspended(this, false);
            PerformLayout();
            Invalidate(true);
        }

        private static void SetLayoutSuspended(Control control, bool suspended)
        {
            if (suspended)
            {
                control.SuspendLayout();
            }

            foreach (Control child in control.Controls)
                SetLayoutSuspended(child, suspended);

            if (!suspended)
                control.ResumeLayout(false);
        }

        /// <summary>
        /// Distributes the 30 fixed money amounts randomly across the 30
        /// cases - once per round. From here on this is the only place that
        /// rolls the dice; the rest of the game logic only ever reads
        /// caseAmountIndex/remainingAmountIndices.
        /// </summary>
        private void InitializeCaseAmounts()
        {
            List<int> availableIndices = Enumerable.Range(0, CaseAmountValues.Length).ToList();

            foreach (PictureBox caseBox in caseList)
            {
                int randomPosition = random.Next(availableIndices.Count);
                int amountIndex = availableIndices[randomPosition];
                availableIndices.RemoveAt(randomPosition);

                caseAmountIndex[caseBox] = amountIndex;
                remainingAmountIndices.Add(amountIndex);
            }
        }

        private void RefreshHomeScreenRecords()
        {
            homeScreenView.SetRecords(
                GameSettings.HighestAmount,
                GameSettings.HighestAmountDate,
                GameSettings.HighestAmountResultLabel);
            homeScreenView.SetHistory(GameHistory.Entries);
        }

        private void ShowBankerOffer(decimal offerValue)
        {
            currentOfferAmount = offerValue;
            SetInfoText("Game.YourOffer");

            bool videoExists = File.Exists(videoPath);

            if (videoExists)
            {
                pboxOfferCover.Visible = true;
                axBankerVideoPlayer.Visible = true;
                axBankerVideoPlayer.BringToFront();
                axBankerVideoPlayer.settings.autoStart = true;
                axBankerVideoPlayer.uiMode = "none";
                axBankerVideoPlayer.URL = videoPath;
                axBankerVideoPlayer.PlayStateChange += BankerVideoPlayer_PlayStateChange;
                axBankerVideoPlayer.MouseDownEvent += BankerVideoPlayer_MouseDownEvent;
            }
            else
            {
                // No banker video found on this machine - jump straight to
                // the offer instead of waiting on a video event that never
                // fires. labelInfoText stays on "Game.YourOffer" (set
                // above) until OfferRevealed actually fires.
                bankerOfferView.Visible = true;
                bankerOfferView.BringToFront();
            }

            // Set behind the still-covering "calculating" overlay - not a
            // spoiler on its own. offerHistoryValues/RenderOfferLog stay
            // untouched until HandleOfferRevealed, though: that log lives
            // in the always-visible Offers sidebar, with nothing covering
            // it, so adding the offer there immediately would spoil it
            // well before the suspense delay/reveal actually happens.
            bankerOfferView.SetOfferAmountText(AppCurrencyFormatter.Format(offerValue));
            bankerOfferView.SetCasesUntilNextOffer(BankerOfferCalculator.CalculateCasesUntilNextOffer(casesClicked));
            bankerOfferView.BeginRevealDelay(BankerOfferCalculator.CalculateRevealDelayMs(casesClicked));
        }

        /// <summary>
        /// Fires once the banker's offer is actually revealed (suspense
        /// delay ran out, or the player clicked to skip it) - only now does
        /// the offer get logged to the Offers sidebar, which has nothing
        /// covering it the way ucBankerOffer's own amount does.
        /// </summary>
        private void HandleOfferRevealed()
        {
            SetInfoText("Game.AcceptOrContinue");

            if (currentOfferAmount.HasValue)
            {
                offerHistoryValues.Insert(0, currentOfferAmount.Value);
                RenderOfferLog();
            }
        }

        /// <summary>
        /// Re-renders the whole offer log from offerHistoryValues, newest
        /// first, formatted with the currently selected currency - the
        /// newest entry is visually highlighted (just larger) while all
        /// entries share the same gold tone. Called both when a new offer
        /// is added and when the currency changes.
        /// </summary>
        private void RenderOfferLog()
        {
            txtOfferLog.Clear();

            for (int i = 0; i < offerHistoryValues.Count; i++)
            {
                bool isLatest = i == 0;

                txtOfferLog.SelectionStart = txtOfferLog.TextLength;
                txtOfferLog.SelectionLength = 0;
                txtOfferLog.SelectionAlignment = HorizontalAlignment.Right;
                txtOfferLog.SelectionFont = new Font(
                    UIStyles.Fonts.Normal.FontFamily,
                    isLatest ? 13f : UIStyles.Fonts.Normal.Size);
                txtOfferLog.SelectionColor = Color.Gold;
                txtOfferLog.AppendText(AppCurrencyFormatter.Format(offerHistoryValues[i]) + Environment.NewLine);
            }

            // AppendText leaves the caret (and therefore the scroll
            // position) at the end of the text, i.e. the oldest offer -
            // once there are enough lines to overflow the box, that pushed
            // the newest one (which is first) out of view. Scroll back to
            // the top so the newest offer always stays visible.
            txtOfferLog.SelectionStart = 0;
            txtOfferLog.SelectionLength = 0;
            txtOfferLog.ScrollToCaret();
        }

        private void BankerVideoPlayer_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if (e.newState == (int)WMPLib.WMPPlayState.wmppsStopped || e.newState == (int)WMPLib.WMPPlayState.wmppsMediaEnded)
            {
                ShowOfferAfterVideo();
                axBankerVideoPlayer.PlayStateChange -= BankerVideoPlayer_PlayStateChange;
            }
        }

        private void BankerVideoPlayer_MouseDownEvent(object sender, AxWMPLib._WMPOCXEvents_MouseDownEvent e)
        {
            if (e.nButton == 1)
            {
                axBankerVideoPlayer.Ctlcontrols.stop();
                ShowOfferAfterVideo();
                axBankerVideoPlayer.MouseDownEvent -= BankerVideoPlayer_MouseDownEvent;
            }
        }

        private void ShowOfferAfterVideo()
        {
            // labelInfoText stays on "Game.YourOffer" (set at the top of
            // ShowBankerOffer) until OfferRevealed actually fires.
            bankerOfferView.Visible = true;
            bankerOfferView.BringToFront();
            axBankerVideoPlayer.Visible = false;
            pboxOfferCover.Visible = false;
        }

        private void Case_Click(object sender, EventArgs e)
        {
            HandleCaseClicked((PictureBox)sender);
        }

        private void HandleCaseClicked(PictureBox clickedCase)
        {
            casesClicked += 1;

            if (selectedCase == null)
            {
                selectedCase = clickedCase;
                // Reparent first, while still Dock=Fill (matching how this
                // always worked) - changing Dock/Size/Location while it's
                // still sitting in caseGridPanel's TableLayoutPanel cell
                // left it invisible after the move.
                panelMyCase.Controls.Add(selectedCase);
                // Fixed, smaller size instead of Dock=Fill - filling the
                // whole "My Case" card made it look oversized next to
                // everything else.
                selectedCase.Dock = DockStyle.None;
                selectedCase.Size = OwnCaseSize;
                CenterOwnCaseDisplay();
                selectedCase.Enabled = false;
                UpdateInfoText();
            }
            else
            {
                clickedCase.Visible = false;
                int amountIndex = caseAmountIndex[clickedCase];
                Button matchedButton = buttonList[amountIndex];
                lastOpenedButton = matchedButton;
                remainingAmountIndices.Remove(amountIndex);
                OpenCases();
                caseOpeningView.SetCaseAmount(matchedButton.Text);
            }
        }

        private void CaseOpeningView_AnimationCompleted(object sender, EventArgs e)
        {
            SetAmountButtonColor(lastOpenedButton, Color.Olive);
        }

        private void CaseOpeningView_CaseOpenedCompleted(object sender, EventArgs e)
        {
            if (pendingFinalResultAmount != null)
            {
                string resultAmount = pendingFinalResultAmount;
                decimal resultValue = pendingFinalResultValue;
                string resultLabel = pendingFinalResultLabel;
                pendingFinalResultAmount = null;
                pendingFinalResultLabel = null;
                EndGame(resultAmount, resultValue, WholeAmountFormat, resultLabel);
                return;
            }

            UpdateInfoText();
            DealerMakesOffer();
        }

        private void OpenCases()
        {
            caseOpeningView.SlowReveal = false;
            caseOpeningView.Visible = true;
            caseOpeningView.BringToFront();
        }

        private void UpdateInfoText()
        {
            if (CasesRemainingByRound.TryGetValue(casesClicked, out int newCasesRemaining))
                casesRemaining = newCasesRemaining;

            string key = casesRemaining == 1 ? "Game.OpenMoreCaseSingular" : "Game.OpenMoreCases";
            SetInfoText(key, casesRemaining);
            casesRemaining--;
        }

        private void DealerMakesOffer()
        {
            if (BankerOfferCalculator.TryGetOfferPercentage(casesClicked, out decimal baseOfferPercentage))
            {
                decimal[] remainingValues = remainingAmountIndices.Select(index => CaseAmountValues[index]).ToArray();
                decimal bankerOffer = BankerOfferCalculator.CalculateOffer(baseOfferPercentage, remainingValues, random);
                ShowBankerOffer(bankerOffer);
            }
        }

        /// <summary>
        /// Called whenever a banker offer is turned down. During the normal
        /// game this just continues the "open more cases" flow - but after
        /// round 29 (the last offer, made with only the player's own case
        /// and one other case left) there's nothing left to open, so this
        /// is where the final keep-or-swap decision actually gets shown.
        /// </summary>
        private void HandleOfferDeclined()
        {
            if (casesClicked == BankerOfferCalculator.LastRound)
            {
                ShowFinalChoice();
                return;
            }

            UpdateInfoText();
        }

        private void ShowFinalChoice()
        {
            lastRemainingCase = caseList.FirstOrDefault(c => c.Visible && c != selectedCase);

            // Once the final choice is showing, nothing in the case grid
            // may be clickable anymore - otherwise a click on the last
            // remaining case could invalidate the click-counter logic (used
            // to cause "Öffne -1 Koffer...").
            DisableAllCases();

            finalChoiceView.Visible = true;
            finalChoiceView.BringToFront();
            SetInfoText("Game.MakeDecision");
        }

        /// <summary>
        /// Ends the game unambiguously - whether by accepting a banker's
        /// offer, or by keeping/swapping the case at the very end.
        /// </summary>
        private void EndGame(string wonAmount, decimal wonAmountValue, string wonAmountFormat, string resultLabel)
        {
            if (IsGameOver)
                return;

            IsGameOver = true;
            this.wonAmountValue = wonAmountValue;
            this.wonAmountFormat = wonAmountFormat;

            if (!GameSettings.HighestAmount.HasValue || wonAmountValue > GameSettings.HighestAmount.Value)
            {
                GameSettings.HighestAmount = wonAmountValue;
                GameSettings.HighestAmountDate = DateTime.Now;
                GameSettings.HighestAmountResultLabel = resultLabel;
            }

            GameSettings.Save();
            GameHistory.Record(wonAmountValue, resultLabel);
            RefreshHomeScreenRecords();

            DisableAllCases();

            // Mark every amount as "done" (same olive color already used
            // for amounts revealed during play) except the one actually
            // won. A banker offer is a computed value that never matches
            // one of the 30 fixed amounts exactly, so this naturally
            // recolors all of them when a deal was accepted, and leaves
            // exactly one still gold when the game ended by keeping or
            // swapping a case.
            foreach (Button amountButton in buttonList)
            {
                if (amountButton.Text != wonAmount)
                    SetAmountButtonColor(amountButton, Color.Olive);
            }

            HideAllGameViews();
            currentOfferAmount = null;

            SetInfoText("Game.Over");

            gameOverView.ShowResult(wonAmount);

            // Only shown when it reveals something not already obvious from
            // the headline number above: an accepted banker offer or a
            // swap. If the player kept their own case, its content already
            // IS the winnings, so the line would just repeat that number.
            if (endedByKeepingOwnCase)
            {
                shownOwnCaseAmountValue = null;
                gameOverView.HideOwnCaseAmount();
            }
            else
            {
                shownOwnCaseAmountValue = CaseAmountValues[caseAmountIndex[selectedCase]];
                gameOverView.ShowOwnCaseAmount(AmountTextOf(selectedCase));
            }

            gameOverView.Visible = true;
            gameOverView.BringToFront();
        }
    }
}
