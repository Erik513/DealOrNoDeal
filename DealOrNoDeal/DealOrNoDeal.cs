using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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

        // Percentage of the average of still-unrevealed amounts the banker
        // offers in each round, keyed by the click count that triggers that
        // round's offer. Modeled on the real show: the offer starts well
        // below the fair average and climbs toward it as fewer cases (and
        // more information) remain, but stays just under 100% - the bank
        // always keeps a small edge. Round 29 is the last offer of all,
        // made right before the final keep-or-swap decision between the
        // player's own case and the one remaining case.
        private static readonly Dictionary<int, decimal> BankerOfferPercentageByRound = new Dictionary<int, decimal>
        {
            { 8, 0.10m },
            { 14, 0.15m },
            { 19, 0.25m },
            { 23, 0.40m },
            { 25, 0.55m },
            { 27, 0.75m },
            { 28, 0.90m },
            { 29, 0.95m }
        };

        /// <summary>
        /// Suspense delay (indexed by offer round, in the same order as
        /// BankerOfferPercentageByRound) before the accept/decline choice
        /// becomes usable - short for the first offer, longer for later
        /// ones, since by then there's more at stake and the moment
        /// deserves to breathe more. Skippable by clicking (see
        /// ucBankerOffer.BeginRevealDelay), so this is a ceiling on the
        /// wait, not a forced one.
        /// </summary>
        private static readonly int[] BankerOfferRevealDelaysMs = { 1500, 2250, 3000, 3750, 4500, 5250, 6000, 6000 };

        private readonly List<PictureBox> caseList = new List<PictureBox>();
        private readonly List<Button> buttonList = new List<Button>();

        // Single source of truth for which case holds which amount (index
        // into CaseAmounts/CaseAmountValues) - independent of display
        // properties such as Tag or BackColor.
        private readonly Dictionary<PictureBox, int> caseAmountIndex = new Dictionary<PictureBox, int>();

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
        public bool IsGameOver;

        private readonly string videoPath = "C:/Users/uif42535/OneDrive - Continental AG/Dateien/Bilder/Visual Studio/DealOrNoDealKoffer/DealersOffer.mp4";

        // Core controls addressed by the game logic.
        private Panel panelMyCase;
        private Panel panelBankerOffer;
        private Panel stageHost;
        private TableLayoutPanel caseGridPanel;
        private Label labelMyCaseTitle;
        private Label labelOffersTitle;
        private Label labelInfoText;
        private RichTextBox txtOfferLog;
        private Button btnOptions;
        private Button btnReset;

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
        private PictureBox lastRemainingCase;

        // Set only while the final (keep/swap) case reveal is playing, so
        // CaseOpeningView_CaseOpenedCompleted knows to end the game instead
        // of continuing the normal open-more-cases flow once it's dismissed.
        private string pendingFinalResultAmount;
        private decimal pendingFinalResultValue;

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
            bankerOfferView.OfferAccepted += amount => EndGame(amount, currentOfferAmount ?? 0m, "#,0.00");
            bankerOfferView.OfferRevealed += () => SetInfoText("Game.AcceptOrContinue");
            caseOpeningView.AnimationCompleted += CaseOpeningView_AnimationCompleted;
            caseOpeningView.CaseOpenedCompleted += CaseOpeningView_CaseOpenedCompleted;
            finalChoiceView.KeepMyCaseClicked += (s, e) => RevealFinalCase(selectedCase);
            finalChoiceView.SwapCaseClicked += (s, e) => RevealFinalCase(lastRemainingCase ?? selectedCase);
            gameOverView.RestartClicked += (s, e) => RestartGame();

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

            UIStyles.Buttons.UpdateTooltip(btnOptions, AppLocalization.Get("Options.MenuButton"));
            UIStyles.Buttons.UpdateTooltip(btnReset, AppLocalization.Get("Game.ResetButton"));

            bankerOfferView.RefreshLanguage();
            finalChoiceView.RefreshLanguage();
            gameOverView.RefreshLanguage();
            caseOpeningView.RefreshLanguage();
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
                buttonList[i].Text = AppCurrencyFormatter.Format(CaseAmountValues[i], "#,0");

            RenderOfferLog();

            if (currentOfferAmount.HasValue)
                bankerOfferView.SetOfferAmountText(AppCurrencyFormatter.Format(currentOfferAmount.Value));

            if (IsGameOver)
            {
                gameOverView.ShowResult(AppCurrencyFormatter.Format(wonAmountValue, wonAmountFormat));

                if (shownOwnCaseAmountValue.HasValue)
                    gameOverView.ShowOwnCaseAmount(AppCurrencyFormatter.Format(shownOwnCaseAmountValue.Value, "#,0"));
            }
        }

        private string AmountTextOf(PictureBox caseBox)
        {
            return AppCurrencyFormatter.Format(CaseAmountValues[caseAmountIndex[caseBox]], "#,0");
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

            bankerOfferView.Visible = false;
            caseOpeningView.Visible = false;
            finalChoiceView.Visible = false;
            gameOverView.Visible = false;
            axBankerVideoPlayer.Visible = false;
            pboxOfferCover.Visible = false;

            SetInfoText("Game.ChooseCase");
        }

        /// <summary>
        /// Opens the language/currency options dialog. Saving raises
        /// AppLocalization.LanguageChanged/AppCurrencyFormatter.CurrencyChanged,
        /// which RefreshLanguage/RefreshCurrency (wired up in the
        /// constructor) react to immediately - no restart needed.
        /// </summary>
        private void OpenOptions()
        {
            using (StyledOptionsForm optionsForm = new StyledOptionsForm(AppLocalization.Get("Options.Title")))
            {
                ComboBox languageCombo = UIStyles.ComboBoxes.CreateStandard();
                languageCombo.Items.Add("English");
                languageCombo.Items.Add("Deutsch");
                languageCombo.SelectedIndex = AppLocalization.Language == AppLanguage.German ? 1 : 0;

                // Dollar first - matches English being the default language.
                ComboBox currencyCombo = UIStyles.ComboBoxes.CreateStandard();
                currencyCombo.Items.Add("Dollar ($)");
                currencyCombo.Items.Add("Euro (€)");
                currencyCombo.SelectedIndex = AppCurrencyFormatter.Currency == AppCurrency.Euro ? 1 : 0;

                optionsForm.PropertyTable.AddRow(AppLocalization.Get("Options.Language"), languageCombo);
                optionsForm.PropertyTable.AddRow(AppLocalization.Get("Options.Currency"), currencyCombo);

                optionsForm.FitToContent();

                optionsForm.SaveClicked += (s, e) =>
                {
                    AppLanguage selectedLanguage = languageCombo.SelectedIndex == 1 ? AppLanguage.German : AppLanguage.English;
                    AppLocalization.Language = selectedLanguage;
                    // Also drives CustomWFUI's own built-in text (e.g. the
                    // title bar's minimize/maximize/close tooltips), which
                    // is a separate setting from AppLocalization.
                    UIStyles.Language = selectedLanguage == AppLanguage.German
                        ? UILanguage.German
                        : UILanguage.English;
                    AppCurrencyFormatter.Currency = currencyCombo.SelectedIndex == 1 ? AppCurrency.Euro : AppCurrency.Dollar;
                    GameSettings.Save();
                };

                optionsForm.ShowDialog(this);
            }
        }

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

        private void InitializeComponent()
        {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(DealOrNoDeal));

            AutoScaleMode = AutoScaleMode.Dpi;
            // Tall enough that the 15-row amount strips (each needs ~30px
            // to show their text without clipping) still fit underneath
            // the now-260px-tall "My Case"/"Offers" cards - 700 was only
            // ever enough before those cards were made taller to stop the
            // offer log itself from clipping.
            //
            // Wide enough that the middle column's own minimum content
            // width doesn't squeeze the right column's card out of its
            // gold border - confirmed by testing a range of widths that
            // the border only reliably renders from ~1350px up; 1200 left
            // no slack at all. 1450 keeps a safety margin beyond that
            // measured threshold.
            MinimumSize = new Size(1450, 800);
            ClientSize = new Size(1659, 900);
            StartPosition = FormStartPosition.CenterScreen;

            TableLayoutPanel root = UIStyles.TableLayoutPanels.CreateDark(3, 1);
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(10);
            root.ColumnStyles.Clear();
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16f));

            root.Controls.Add(BuildLeftColumn(), 0, 0);
            root.Controls.Add(BuildMiddleColumn(resources), 1, 0);
            root.Controls.Add(BuildRightColumn(resources), 2, 0);

            ContentPanel.Controls.Add(root);
        }

        private Control BuildLeftColumn()
        {
            TableLayoutPanel column = UIStyles.TableLayoutPanels.CreateDark(1, 2);
            column.Dock = DockStyle.Fill;
            column.Margin = new Padding(0, 0, 6, 0);
            column.RowStyles.Clear();
            // Matches the offers card's height on the right for visual
            // symmetry - see BuildRightColumn for why it needs to be this
            // tall.
            column.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
            column.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Panel myCaseCard = BuildCard(AppLocalization.Get("Game.MyCaseLabel"), out Panel myCaseContent, out labelMyCaseTitle);
            panelMyCase = myCaseContent;
            // selectedCase is a fixed size, not Dock=Fill, so it doesn't
            // automatically stay centered when the window (and so
            // panelMyCase) is resized - re-center it manually whenever
            // that happens.
            panelMyCase.Resize += (s, e) => CenterOwnCaseDisplay();
            TableLayoutPanel lowAmountStrip = BuildAmountStrip(0, 15);

            column.Controls.Add(myCaseCard, 0, 0);
            column.Controls.Add(lowAmountStrip, 0, 1);

            return column;
        }

        private Control BuildRightColumn(ComponentResourceManager resources)
        {
            TableLayoutPanel column = UIStyles.TableLayoutPanels.CreateDark(1, 2);
            column.Dock = DockStyle.Fill;
            column.Margin = new Padding(6, 0, 0, 0);
            column.RowStyles.Clear();
            // Fixed (not percent-based) so this always has enough room for
            // up to 8 accumulated banker offers regardless of window size -
            // txtOfferLog has no scrollbar, so anything taller than this
            // card would previously just get clipped off. 190 was only
            // ever enough for about 5 lines; 230 still comfortably fits all
            // 8 with margin to spare.
            column.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
            column.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Panel offerCard = BuildCard(AppLocalization.Get("Game.OffersLabel"), out Panel offerContent, out labelOffersTitle);
            panelBankerOffer = offerCard;

            // Plain TextBox can't format individual lines - a RichTextBox
            // lets the newest offer stand out (larger) from the older ones
            // below it, all in the same gold tone.
            // A plain "Cursor = Cursors.Default" (or forcing it on
            // MouseMove) doesn't work: the native RichEdit control re-sets
            // its own I-beam cursor on every WM_SETCURSOR message, which
            // fires far more often than MouseMove - the two kept
            // overwriting each other, which is exactly what caused the
            // flicker. ArrowCursorRichTextBox intercepts WM_SETCURSOR
            // itself and never lets the native control handle it at all.
            txtOfferLog = new ArrowCursorRichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                TabStop = false,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.Gold,
                Font = UIStyles.Fonts.Normal,
                ScrollBars = RichTextBoxScrollBars.None,
                Cursor = Cursors.Default
            };
            // ReadOnly still lets it take focus on click and shows a
            // blinking text caret at the selection - it's just a display,
            // not an input field, so hand focus straight back whenever it
            // tries to take it.
            txtOfferLog.GotFocus += (s, e) => ActiveControl = null;

            pboxOfferCover = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Visible = false,
                Image = (Image)resources.GetObject("pboxAngebotÜberdecken.Image")
            };

            offerContent.Controls.Add(txtOfferLog);
            offerContent.Controls.Add(pboxOfferCover);
            // Both are Dock=Fill and intentionally overlap - the cover is
            // meant to hide the offer log while the video is playing.
            pboxOfferCover.BringToFront();

            TableLayoutPanel highAmountStrip = BuildAmountStrip(15, 15);

            column.Controls.Add(offerCard, 0, 0);
            column.Controls.Add(highAmountStrip, 0, 1);

            return column;
        }

        private Control BuildMiddleColumn(ComponentResourceManager resources)
        {
            TableLayoutPanel column = UIStyles.TableLayoutPanels.CreateDark(1, 2);
            column.Dock = DockStyle.Fill;
            column.Margin = new Padding(6, 0, 6, 0);
            column.RowStyles.Clear();
            column.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            column.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

            stageHost = BuildStageHost(resources);

            Panel infoBar = UIStyles.Panels.CreatePrimary();
            infoBar.Margin = new Padding(0, 6, 0, 0);
            labelInfoText = UIStyles.Labels.CreateTitle("Text");
            labelInfoText.Dock = DockStyle.Fill;
            labelInfoText.TextAlign = ContentAlignment.MiddleCenter;
            labelInfoText.Font = new Font(UIStyles.Fonts.Title.FontFamily, 16f, FontStyle.Bold);

            btnOptions = UIStyles.Buttons.CreateStandard("⚙", AppLocalization.Get("Options.MenuButton"));
            btnOptions.Dock = DockStyle.Right;
            btnOptions.Width = 56;
            btnOptions.Font = UIStyles.Fonts.Icon;
            btnOptions.Click += (s, e) => OpenOptions();

            // Mirrors the options button on the opposite side.
            btnReset = UIStyles.Buttons.CreateStandard("↺", AppLocalization.Get("Game.ResetButton"));
            btnReset.Dock = DockStyle.Left;
            btnReset.Width = 56;
            btnReset.Font = UIStyles.Fonts.Icon;
            btnReset.Click += (s, e) => RestartGame();

            // Fill added first, edge-docked buttons after - same reasoning
            // as BuildCard: keeps their bands reliably reserved.
            infoBar.Controls.Add(labelInfoText);
            infoBar.Controls.Add(btnOptions);
            infoBar.Controls.Add(btnReset);

            column.Controls.Add(stageHost, 0, 0);
            column.Controls.Add(infoBar, 0, 1);

            return column;
        }

        /// <summary>
        /// A shared Dock=Fill container for all overlapping game phases
        /// (case grid, banker video, offer, final choice). In the original
        /// layout these had different fixed pixel sizes and Anchor
        /// combinations and drifted apart when scaling - now they share
        /// exactly the same area.
        /// </summary>
        private Panel BuildStageHost(ComponentResourceManager resources)
        {
            Panel host = UIStyles.Panels.CreatePrimary();
            host.BackColor = Color.Gold;
            host.Padding = new Padding(4);

            TableLayoutPanel caseGrid = BuildCaseGrid(resources);

            axBankerVideoPlayer = new AxWMPLib.AxWindowsMediaPlayer
            {
                Dock = DockStyle.Fill,
                Enabled = true,
                Visible = false
            };

            bankerOfferView = new ucBankerOffer { Visible = false };
            caseOpeningView = new ucOpenCase { Visible = false };
            finalChoiceView = new ucFinalChoice { Visible = false };
            gameOverView = new ucGameOver { Visible = false };

            // Order only matters for the initial build - which layer is
            // visible is controlled purely via Visible/BringToFront, not
            // via the add order.
            host.Controls.Add(caseGrid);
            host.Controls.Add(axBankerVideoPlayer);
            host.Controls.Add(bankerOfferView);
            host.Controls.Add(caseOpeningView);
            host.Controls.Add(finalChoiceView);
            host.Controls.Add(gameOverView);

            return host;
        }

        private TableLayoutPanel BuildCaseGrid(ComponentResourceManager resources)
        {
            TableLayoutPanel grid = UIStyles.TableLayoutPanels.CreateDark(5, 6);
            grid.Dock = DockStyle.Fill;
            grid.BackColor = Color.FromArgb(64, 64, 64);
            grid.ColumnStyles.Clear();
            grid.RowStyles.Clear();

            for (int col = 0; col < 5; col++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            for (int row = 0; row < 6; row++)
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6f));

            for (int i = 1; i <= 30; i++)
            {
                PictureBox caseBox = new PictureBox
                {
                    Name = "B" + i,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = (Image)resources.GetObject("B" + i + ".Image"),
                    Cursor = Cursors.Hand
                };
                caseBox.Click += Case_Click;

                int index = i - 1;
                int col = index % 5;
                int row = index / 5;
                grid.Controls.Add(caseBox, col, row);

                caseList.Add(caseBox);
            }

            caseGridPanel = grid;
            return grid;
        }

        private TableLayoutPanel BuildAmountStrip(int startIndex, int count)
        {
            TableLayoutPanel strip = UIStyles.TableLayoutPanels.CreateDark(1, count);
            strip.Dock = DockStyle.Fill;
            strip.RowStyles.Clear();
            strip.ColumnStyles.Clear();
            strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            for (int i = 0; i < count; i++)
            {
                strip.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / count));

                int caseNumber = startIndex + i + 1;
                Button amountButton = UIStyles.Buttons.CreateStandard(
                    AppCurrencyFormatter.Format(CaseAmountValues[caseNumber - 1], "#,0"));
                amountButton.Name = "button" + caseNumber;
                amountButton.Dock = DockStyle.Fill;
                // 4, not 2 - the right strip's column consistently renders
                // 1-2px narrower than the left's (percent-column rounding),
                // which was exactly enough to swallow a 2px margin whole,
                // leaving no visible gap at all on that side. A bigger
                // margin leaves a visible gap on both sides even after
                // that deficit, just not pixel-identical.
                amountButton.Margin = new Padding(4);
                amountButton.TabStop = false;
                amountButton.Cursor = Cursors.Default;
                amountButton.ForeColor = Color.Black;
                SetAmountButtonColor(amountButton, Color.Yellow);

                strip.Controls.Add(amountButton, 0, i);
                buttonList.Add(amountButton);
            }

            return strip;
        }

        /// <summary>
        /// Sets an amount button's color and keeps its hover/mouse-down
        /// colors identical to it - these buttons are purely informational
        /// (TabStop=false, Cursor=Default), so hovering one must never
        /// visibly change it. Without this, a button's hover color stayed
        /// fixed at whatever it was set to when the button was first built
        /// (yellow), so an already-opened, olive-colored amount would flash
        /// back to yellow on mouseover.
        /// </summary>
        private static void SetAmountButtonColor(Button amountButton, Color color)
        {
            amountButton.BackColor = color;
            amountButton.FlatAppearance.MouseOverBackColor = color;
            amountButton.FlatAppearance.MouseDownBackColor = color;
        }

        /// <summary>
        /// Golden frame with a title bar (Dock=Top) and its own indented
        /// content area (Dock=Fill + Padding). The fill area is added first,
        /// the title bar after - exactly the docking pattern StyledForm
        /// itself uses for title bar + content, and it prevents the title
        /// from overlapping the content.
        /// </summary>
        private Panel BuildCard(string title, out Panel content, out Label titleLabel)
        {
            Panel card = UIStyles.Panels.CreatePrimary();
            card.BackColor = Color.Gold;
            card.Padding = new Padding(2);

            Panel innerContent = UIStyles.Panels.CreateDark();
            innerContent.Dock = DockStyle.Fill;
            innerContent.BackColor = Color.FromArgb(64, 64, 64);
            innerContent.Padding = new Padding(6);

            titleLabel = UIStyles.Labels.CreateTitle(title);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 30;
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.BackColor = Color.FromArgb(64, 64, 64);
            titleLabel.ForeColor = Color.Gold;

            card.Controls.Add(innerContent);
            card.Controls.Add(titleLabel);

            content = innerContent;
            return card;
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

        public void ShowBankerOffer(decimal offerValue)
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

            bankerOfferView.SetOfferAmountText(AppCurrencyFormatter.Format(offerValue));
            bankerOfferView.SetCasesUntilNextOffer(CalculateCasesUntilNextOffer());
            bankerOfferView.BeginRevealDelay(CalculateOfferRevealDelayMs());
            offerHistoryValues.Insert(0, offerValue);
            RenderOfferLog();
        }

        private int CalculateOfferRevealDelayMs()
        {
            List<int> sortedRounds = BankerOfferPercentageByRound.Keys.OrderBy(round => round).ToList();
            int roundIndex = sortedRounds.IndexOf(casesClicked);
            return BankerOfferRevealDelaysMs[Math.Max(0, roundIndex)];
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

        /// <summary>
        /// How many more cases need to be opened before the next banker
        /// offer - null on the very last offer (round 29), where declining
        /// leads straight to the final keep-or-swap decision instead of
        /// another round of case-opening.
        /// </summary>
        private int? CalculateCasesUntilNextOffer()
        {
            int nextRoundThreshold = BankerOfferPercentageByRound.Keys
                .Where(round => round > casesClicked)
                .DefaultIfEmpty(-1)
                .Min();

            return nextRoundThreshold == -1 ? (int?)null : nextRoundThreshold - casesClicked;
        }

        private void CenterOwnCaseDisplay()
        {
            if (selectedCase == null || selectedCase.Parent != panelMyCase)
                return;

            selectedCase.Location = new Point(
                (panelMyCase.Width - selectedCase.Width) / 2,
                (panelMyCase.Height - selectedCase.Height) / 2);
        }

        private void Case_Click(object sender, EventArgs e)
        {
            PictureBox clickedCase = (PictureBox)sender;
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
                pendingFinalResultAmount = null;
                EndGame(resultAmount, resultValue, "#,0");
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

        public void UpdateInfoText()
        {
            if (casesClicked == 1) casesRemaining = 7;
            if (casesClicked == 8) casesRemaining = 6;
            if (casesClicked == 14) casesRemaining = 5;
            if (casesClicked == 19) casesRemaining = 4;
            if (casesClicked == 23) casesRemaining = 2;
            if (casesClicked == 25) casesRemaining = 2;
            if (casesClicked == 27) casesRemaining = 1;
            if (casesClicked == 28) casesRemaining = 1;
            if (casesClicked == 29) casesRemaining = 0;

            SetInfoText("Game.OpenMoreCases", casesRemaining);
            casesRemaining--;
        }

        private void DealerMakesOffer()
        {
            if (BankerOfferPercentageByRound.TryGetValue(casesClicked, out decimal baseOfferPercentage))
            {
                decimal bankerOffer = CalculateBankerOffer(baseOfferPercentage);
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
            if (casesClicked == 29)
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
            foreach (PictureBox caseBox in caseList)
                caseBox.Enabled = false;

            finalChoiceView.Visible = true;
            finalChoiceView.BringToFront();
            SetInfoText("Game.MakeDecision");
        }

        /// <summary>
        /// Ends the game unambiguously - whether by accepting a banker's
        /// offer, or by keeping/swapping the case at the very end.
        /// </summary>
        private void EndGame(string wonAmount, decimal wonAmountValue, string wonAmountFormat)
        {
            if (IsGameOver)
                return;

            IsGameOver = true;
            this.wonAmountValue = wonAmountValue;
            this.wonAmountFormat = wonAmountFormat;

            foreach (PictureBox caseBox in caseList)
                caseBox.Enabled = false;

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

            bankerOfferView.Visible = false;
            caseOpeningView.Visible = false;
            finalChoiceView.Visible = false;
            axBankerVideoPlayer.Visible = false;
            pboxOfferCover.Visible = false;
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

        /// <summary>
        /// The banker's offer for this round: the round's base percentage
        /// of the average remaining amount, dampened when those amounts are
        /// spread far apart (e.g. a very small and a very large one both
        /// still in play). A flat percentage-of-average would otherwise be
        /// unrealistically generous whenever one huge amount happens to
        /// still be live and everyone would just take the deal - in the
        /// real show the banker holds back more the riskier/more spread
        /// out the remaining pool is, and only approaches fair value once
        /// the remaining amounts are close together.
        /// </summary>
        private decimal CalculateBankerOffer(decimal baseOfferPercentage)
        {
            if (remainingAmountIndices.Count == 0)
                return 0;

            decimal[] remainingValues = remainingAmountIndices.Select(index => CaseAmountValues[index]).ToArray();
            decimal average = remainingValues.Average();

            if (average == 0)
                return 0;

            decimal variance = remainingValues.Average(value => (value - average) * (value - average));
            decimal coefficientOfVariation = (decimal)Math.Sqrt((double)variance) / average;

            // Normalize the spread into [0, 1] (capped at a CV of 2, which
            // is already an extreme spread) and dampen the base percentage
            // by at most half of it - so a high-risk round pays noticeably
            // less than the round's usual percentage, but never turns into
            // an obvious "always decline" trap either.
            decimal riskFactor = Math.Min(coefficientOfVariation, 2m) / 2m;
            decimal offerPercentage = baseOfferPercentage * (1m - riskFactor * 0.5m);
            decimal offer = average * offerPercentage;

            // Hard rule, enforced regardless of how the percentage table
            // above is tuned: the banker never offers more than the fair
            // average of what's still in play.
            return Math.Min(offer, average);
        }
    }

    /// <summary>
    /// A RichTextBox that always shows the normal arrow cursor. The native
    /// RichEdit control handles WM_SETCURSOR itself and sets its own I-beam
    /// cursor over text - setting Control.Cursor (or re-forcing it on
    /// MouseMove) doesn't stop that and just fights it, message by message,
    /// which is what caused the cursor to flicker. Intercepting
    /// WM_SETCURSOR here and never forwarding it to the native control is
    /// the only way to actually prevent it from setting a cursor at all.
    /// </summary>
    internal sealed class ArrowCursorRichTextBox : RichTextBox
    {
        private const int WM_SETCURSOR = 0x0020;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SETCURSOR)
            {
                Cursor.Current = Cursors.Default;
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);
        }
    }
}
