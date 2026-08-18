using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI;
using CustomWFUI.Styles;

namespace DealOrNoDeal
{
    // Pure UI construction - InitializeComponent and everything it calls to
    // build the game screen. Kept separate from DealOrNoDeal.cs (game
    // state, flow, event handlers) so "how it's built" and "how it behaves"
    // aren't interleaved in one file - same type, same fields, just split
    // by file, the standard way to shrink a WinForms code-behind file.
    public partial class DealOrNoDeal
    {
        // BuildLeftColumn/BuildRightColumn's fixed top-row height - the two
        // must stay equal for the left ("My Case") and right ("Offers")
        // cards to line up visually; a shared constant means an edit to one
        // can't silently desync the other.
        private const int TopCardRowHeight = 230;

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
            //
            // Tall enough for ucHomeScreen too, not just the in-game
            // layout - its fixed-height title row (130) + 20-row history
            // table (~658) need ~788px before the title bar, which 800 no
            // longer covered once the home screen was added; 850 restores
            // a real margin instead of clipping the Play button at the
            // bottom.
            MinimumSize = new Size(1450, 850);
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

            // Both Dock=Fill and intentionally overlap, same as
            // pboxOfferCover/txtOfferLog - the home screen covers the
            // entire game layout (not just stageHost) until Play is
            // clicked, so it needs to sit directly on ContentPanel.
            homeScreenView = new ucHomeScreen();
            RefreshHomeScreenRecords();
            homeScreenView.PlayClicked += (s, e) => homeScreenView.Visible = false;

            ContentPanel.Controls.Add(root);
            ContentPanel.Controls.Add(homeScreenView);
            homeScreenView.BringToFront();
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
            column.RowStyles.Add(new RowStyle(SizeType.Absolute, TopCardRowHeight));
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
            column.RowStyles.Add(new RowStyle(SizeType.Absolute, TopCardRowHeight));
            column.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Panel offerCard = BuildCard(AppLocalization.Get("Game.OffersLabel"), out Panel offerContent, out labelOffersTitle);

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

            Panel stageHost = BuildStageHost(resources);

            Panel infoBar = UIStyles.Panels.CreatePrimary();
            infoBar.Margin = new Padding(0, 6, 0, 0);
            labelInfoText = UIStyles.Labels.CreateTitle("Text");
            labelInfoText.Dock = DockStyle.Fill;
            labelInfoText.TextAlign = ContentAlignment.MiddleLeft;
            labelInfoText.Padding = new Padding(16, 0, 0, 0);
            labelInfoText.Font = new Font(UIStyles.Fonts.Title.FontFamily, 16f, FontStyle.Bold);

            // Options moved to the home screen - this button now leaves an
            // in-progress game entirely, so it needs a confirmation instead
            // of acting immediately like the old "restart in place" button
            // did.
            btnMainMenu = UIStyles.Buttons.CreateStandard("⌂", AppLocalization.Get("Game.MainMenuButton"));
            btnMainMenu.Dock = DockStyle.Left;
            btnMainMenu.Width = 56;
            btnMainMenu.Font = UIStyles.Fonts.Icon;
            btnMainMenu.Click += (s, e) => ConfirmReturnToMainMenu();

            // Fill added first, edge-docked buttons after - same reasoning
            // as BuildCard: keeps their bands reliably reserved.
            infoBar.Controls.Add(labelInfoText);
            infoBar.Controls.Add(btnMainMenu);

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

                // The case artwork itself already prints a large number on
                // every case - no need for a separate visible badge, just
                // the internal lookup for the history table.
                caseNumbers[caseBox] = i;

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
                    AppCurrencyFormatter.Format(CaseAmountValues[caseNumber - 1], WholeAmountFormat));
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

        private void CenterOwnCaseDisplay()
        {
            if (selectedCase == null || selectedCase.Parent != panelMyCase)
                return;

            selectedCase.Location = new Point(
                (panelMyCase.Width - selectedCase.Width) / 2,
                (panelMyCase.Height - selectedCase.Height) / 2);
        }
    }
}
