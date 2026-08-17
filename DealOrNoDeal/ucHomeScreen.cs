using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CustomWFUI;
using CustomWFUI.Controls;
using CustomWFUI.Styles;

namespace DealOrNoDeal
{
    /// <summary>
    /// The very first thing shown when the app starts: title on top, then
    /// this player's game history on the left and, on the right, a short
    /// how-to-play blurb, the app's settings (language, currency) shown
    /// inline as a property table instead of behind a separate Options
    /// window, and Play - all packed together and centered as one block.
    /// Added directly to ContentPanel as a Dock=Fill overlay (not inside
    /// stageHost) since it replaces the whole game layout - side columns
    /// and info bar included - not just one phase of it.
    /// </summary>
    public partial class ucHomeScreen : UserControl
    {
        // Every item stacked on the right - the instructions blurb, the
        // settings property table, and Play - shares this exact width, so
        // the whole column reads as one uniform block instead of a ragged
        // stack of differently-sized pieces.
        private const int RightColumnWidth = 330;

        private static readonly Size PlayButtonSize = new Size(RightColumnWidth, 70);

        private const int HistoryDateColumnWidth = 170;
        private const int HistoryCaseColumnWidth = 100;
        private const int HistoryResultColumnWidth = 140;

        // Fixed gap between the history block and the right-hand block so
        // the two read as one tightly-packed, centered unit instead of
        // being pulled apart across the full width.
        private const int TableGroupGap = 60;

        // Gap between the stacked items on the right (instructions,
        // settings).
        private const int RightColumnItemGap = 24;

        // Shared by every table so their header/row bands line up at the
        // same height even though history has far more rows than the
        // others - same font sizes too, so no table looks mismatched.
        // Kept small enough that 20 fixed history rows plus a header still
        // fit the content band at the app's default window height.
        private const int TableHeaderHeight = 28;
        private const int TableRowHeight = 28;
        private const float TableRowFontSize = 12f;
        private const float TableHeaderFontSize = 13f;

        // Fixed instead of AutoSize so every title row is pixel-identical
        // regardless of the text's own font metrics (e.g. a descender like
        // the "y" in "History") - otherwise two tables' headers can end up
        // a couple pixels apart even though they look the same height. Tall
        // enough to clear descenders in every language (e.g. the "g" in
        // "Angebot"/"Betrag"), not just English text.
        private const int TableTitleRowHeight = 42;

        // Fixed row count - the table always shows exactly this many rows,
        // blank ones included, so its height never changes as games get
        // added and it never needs to scroll.
        private const int HistoryDisplayRowCount = 20;

        // Shown for the all-time-best row before any game has finished, or
        // right after a reset - a dash there read oddly on an otherwise
        // empty row, so it's just left blank instead.
        private const string NoRecordYetPlaceholder = "";

        private readonly Label labelTitle;
        private readonly Label labelHistoryTitle;
        private readonly Label labelInstructionsTitle;
        private readonly Label labelInstructionsBody;
        private readonly Label labelSettingsTitle;
        private readonly Label labelAllTimeBestName;
        private readonly Label labelAllTimeBestCase;
        private readonly Label labelAllTimeBestAmount;
        private readonly StyledDataTable historyTable;
        private readonly StyledPropertyTable settingsTable;
        private readonly ComboBox languageCombo;
        private readonly ComboBox currencyCombo;
        private readonly Button btnPlay;
        private readonly ToolTip allTimeBestToolTip;

        private decimal? highestAmount;
        private DateTime? highestAmountDate;
        private string highestAmountResultLabel;
        private List<(DateTime Date, decimal Amount, string ResultLabel)> historyEntries =
            new List<(DateTime, decimal, string)>();

        public event EventHandler PlayClicked;

        public ucHomeScreen()
        {
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = UIStyles.Colors.BackgroundMediumElevated;

            TableLayoutPanel main = UIStyles.TableLayoutPanels.CreateStandard(1, 2);
            main.Dock = DockStyle.Fill;
            main.BackColor = Color.Transparent;
            main.RowStyles.Clear();
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.ColumnStyles.Clear();
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // "Deal Or No Deal ?" left untranslated everywhere else in the
            // app too - it's the show's own name.
            labelTitle = UIStyles.Labels.CreateTitle("Deal Or No Deal ?");
            labelTitle.Dock = DockStyle.Fill;
            labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            labelTitle.BackColor = Color.Transparent;
            labelTitle.ForeColor = UIStyles.Colors.YellowLighter;
            labelTitle.Font = new Font(UIStyles.Fonts.Title.FontFamily, 52f, FontStyle.Bold);

            // History (left) and offer/amount/settings (right) sit together
            // as one tightly-packed block, centered as a whole in the
            // content area. The right column is a 3-row table (stacked
            // content / spacer / Play) that stretches to match history's
            // height, so Play ends up pinned to the bottom-right corner of
            // the whole block. The data tables use StyledDataTable (not the
            // full StyledListView - no clipboard copy/resizable columns
            // needed for read-only data); history is capped at 20 rows so
            // it never needs to scroll.
            TableLayoutPanel content = UIStyles.TableLayoutPanels.CreateStandard(2, 1);
            content.AutoSize = true;
            content.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            content.Anchor = AnchorStyles.None;
            content.BackColor = Color.Transparent;
            content.ColumnStyles.Clear();
            content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            content.RowStyles.Clear();
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            historyTable = UIStyles.DataTables.Create();
            historyTable.HeaderHeight = TableHeaderHeight;
            historyTable.RowHeight = TableRowHeight;
            historyTable.RowFont = new Font(UIStyles.Fonts.Normal.FontFamily, TableRowFontSize);
            historyTable.HeaderFont = new Font(UIStyles.Fonts.Normal.FontFamily, TableHeaderFontSize, FontStyle.Bold);
            historyTable.SetColumns(
                new[] { AppLocalization.Get("Home.DateColumn"), AppLocalization.Get("Home.CaseColumn"), AppLocalization.Get("Home.ResultColumn") },
                new[] { HistoryDateColumnWidth, HistoryCaseColumnWidth, HistoryResultColumnWidth });

            labelHistoryTitle = CreateTableTitleLabel(AppLocalization.Get("Home.HistoryTitle"));

            (Control historyWithAllTimeBest, Label allTimeBestName, Label allTimeBestCase, Label allTimeBestAmount) =
                CreateHistoryTableWithAllTimeBest();
            labelAllTimeBestName = allTimeBestName;
            labelAllTimeBestCase = allTimeBestCase;
            labelAllTimeBestAmount = allTimeBestAmount;

            labelAllTimeBestName.Text = AppLocalization.Get("Home.AllTimeBestRow");
            allTimeBestToolTip = UIStyles.ToolTips.CreateToolTip();

            Control historyBlock = CreateTitledTable(labelHistoryTitle, historyWithAllTimeBest);
            // Every block needs its Margin zeroed explicitly - Control's
            // default Margin is Padding(3,3,3,3), and a stray 3px top
            // margin on just one of these inside its FlowLayoutPanel is
            // exactly what was throwing history's header out of alignment
            // with the others.
            historyBlock.Margin = new Padding(0);

            (Control instructionsBlock, Label instructionsTitle, Label instructionsBody) =
                CreateInstructionsBlock();
            labelInstructionsTitle = instructionsTitle;
            labelInstructionsBody = instructionsBody;

            // Language and currency - shown right here instead of behind a
            // separate Options window.
            languageCombo = UIStyles.ComboBoxes.CreateStandard();
            languageCombo.Items.Add("English");
            languageCombo.Items.Add("Deutsch");
            languageCombo.SelectedIndex = AppLocalization.Language == AppLanguage.German ? 1 : 0;
            languageCombo.SelectedIndexChanged += (s, e) =>
            {
                AppLanguage selectedLanguage = languageCombo.SelectedIndex == 1 ? AppLanguage.German : AppLanguage.English;
                AppLocalization.Language = selectedLanguage;
                // Also drives CustomWFUI's own built-in text (e.g. the
                // title bar's minimize/maximize/close tooltips), which is a
                // separate setting from AppLocalization.
                UIStyles.Language = selectedLanguage == AppLanguage.German
                    ? UILanguage.German
                    : UILanguage.English;
                GameSettings.Save();
            };

            // Dollar first - matches English being the default language.
            currencyCombo = UIStyles.ComboBoxes.CreateStandard();
            currencyCombo.Items.Add("Dollar ($)");
            currencyCombo.Items.Add("Euro (€)");
            currencyCombo.SelectedIndex = AppCurrencyFormatter.Currency == AppCurrency.Euro ? 1 : 0;
            currencyCombo.SelectedIndexChanged += (s, e) =>
            {
                AppCurrencyFormatter.Currency = currencyCombo.SelectedIndex == 1 ? AppCurrency.Euro : AppCurrency.Dollar;
                GameSettings.Save();
            };

            settingsTable = UIStyles.PropertyTables.Create();
            settingsTable.Dock = DockStyle.None;
            settingsTable.Anchor = AnchorStyles.Left;
            settingsTable.Margin = new Padding(0);
            // Only Language/Currency now (no reset row) - just Combos, both
            // Dock=Fill inside the property table, don't reliably drive its
            // own AutoSize width up to RightColumnWidth on their own, so
            // give it a matching floor instead.
            settingsTable.MinimumSize = new Size(RightColumnWidth, 0);
            RebuildSettingsRows();

            labelSettingsTitle = CreateTableTitleLabel(AppLocalization.Get("Home.SettingsTitle"));
            Control settingsBlock = CreateTitledTable(labelSettingsTitle, settingsTable);
            settingsBlock.Margin = new Padding(0, 0, 0, RightColumnItemGap);

            btnPlay = UIStyles.Buttons.CreatePrimary(AppLocalization.Get("Home.PlayButton"), size: PlayButtonSize);
            btnPlay.Margin = new Padding(0);
            btnPlay.Font = new Font(UIStyles.Fonts.Normal.FontFamily, 15f, FontStyle.Bold);
            btnPlay.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPlay.Click += (s, e) => PlayClicked?.Invoke(this, EventArgs.Empty);

            // Both columns go through an identical Dock=Fill TableLayoutPanel
            // wrapping an identical FlowLayoutPanel (even though the left
            // one only ever holds a single control) so the two sides are
            // laid out through the exact same nesting depth - otherwise the
            // two header rows can end up a couple pixels apart even with
            // identical row heights, since an asymmetric AutoSize/Dock=Fill
            // nesting depth on only one side introduces its own small
            // rounding difference.
            FlowLayoutPanel leftStack = UIStyles.FlowLayoutPanels.CreateStandard();
            leftStack.FlowDirection = FlowDirection.TopDown;
            leftStack.WrapContents = false;
            leftStack.BackColor = Color.Transparent;
            leftStack.Controls.Add(historyBlock);

            TableLayoutPanel leftColumn = UIStyles.TableLayoutPanels.CreateStandard(1, 3);
            leftColumn.Dock = DockStyle.Fill;
            leftColumn.AutoSize = true;
            leftColumn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            leftColumn.BackColor = Color.Transparent;
            leftColumn.ColumnStyles.Clear();
            leftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            leftColumn.RowStyles.Clear();
            leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftColumn.Controls.Add(leftStack, 0, 0);

            // Instructions grow to soak up whatever extra height this
            // column gets once it's stretched (by content's AutoSize row)
            // to match history's height, so Settings ends up sitting
            // directly above Play instead of leaving a gap between them.
            TableLayoutPanel rightColumn = UIStyles.TableLayoutPanels.CreateStandard(1, 3);
            rightColumn.Dock = DockStyle.Fill;
            rightColumn.AutoSize = true;
            rightColumn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            rightColumn.BackColor = Color.Transparent;
            rightColumn.ColumnStyles.Clear();
            rightColumn.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            rightColumn.RowStyles.Clear();
            rightColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rightColumn.Controls.Add(instructionsBlock, 0, 0);
            rightColumn.Controls.Add(settingsBlock, 0, 1);
            rightColumn.Controls.Add(btnPlay, 0, 2);

            leftColumn.Margin = new Padding(0, 0, TableGroupGap, 0);
            content.Controls.Add(leftColumn, 0, 0);
            content.Controls.Add(rightColumn, 1, 0);

            main.Controls.Add(labelTitle, 0, 0);
            main.Controls.Add(content, 0, 1);

            Controls.Add(main);
        }

        public void SetRecords(
            decimal? highestAmountValue,
            DateTime? highestAmountDateValue,
            string highestAmountResultLabelValue)
        {
            highestAmount = highestAmountValue;
            highestAmountDate = highestAmountDateValue;
            highestAmountResultLabel = highestAmountResultLabelValue;
            RefreshCurrency();
        }

        public void SetHistory(IEnumerable<(DateTime Date, decimal Amount, string ResultLabel)> entries)
        {
            historyEntries = entries?.ToList() ?? new List<(DateTime, decimal, string)>();
            RefreshCurrency();
        }

        public void RefreshLanguage()
        {
            btnPlay.Text = AppLocalization.Get("Home.PlayButton");
            labelHistoryTitle.Text = AppLocalization.Get("Home.HistoryTitle");
            labelInstructionsTitle.Text = AppLocalization.Get("Home.InstructionsTitle");
            labelInstructionsBody.Text = AppLocalization.Get("Home.InstructionsBody");
            labelSettingsTitle.Text = AppLocalization.Get("Home.SettingsTitle");
            labelAllTimeBestName.Text = AppLocalization.Get("Home.AllTimeBestRow");
            historyTable.SetColumns(
                new[] { AppLocalization.Get("Home.DateColumn"), AppLocalization.Get("Home.CaseColumn"), AppLocalization.Get("Home.ResultColumn") },
                new[] { HistoryDateColumnWidth, HistoryCaseColumnWidth, HistoryResultColumnWidth });
            RebuildSettingsRows();
            RefreshCurrency();
        }

        public void RefreshCurrency()
        {
            labelAllTimeBestCase.Text = highestAmountResultLabel ?? "";
            labelAllTimeBestAmount.Text = FormatOrPlaceholder(highestAmount);

            string dateTooltip = highestAmountDate.HasValue
                ? highestAmountDate.Value.ToString("yyyy-MM-dd HH:mm")
                : "";
            allTimeBestToolTip.SetToolTip(labelAllTimeBestName, dateTooltip);
            allTimeBestToolTip.SetToolTip(labelAllTimeBestCase, dateTooltip);
            allTimeBestToolTip.SetToolTip(labelAllTimeBestAmount, dateTooltip);

            // Always exactly HistoryDisplayRowCount rows - real entries
            // first, then blank padding - so the table's height never
            // changes as games get added.
            IEnumerable<string[]> historyRows = historyEntries.Select(entry => new[]
            {
                entry.Date.ToString("yyyy-MM-dd HH:mm"),
                entry.ResultLabel,
                AppCurrencyFormatter.Format(entry.Amount, "#,0")
            });
            IEnumerable<string[]> blankPadding = Enumerable
                .Repeat(new[] { string.Empty, string.Empty, string.Empty }, Math.Max(0, HistoryDisplayRowCount - historyEntries.Count));
            historyTable.SetRows(historyRows.Concat(blankPadding));
        }

        private void RebuildSettingsRows()
        {
            settingsTable.ClearRows();
            settingsTable.AddRow(AppLocalization.Get("Options.Language"), languageCombo);
            settingsTable.AddRow(AppLocalization.Get("Options.Currency"), currencyCombo);
        }

        private static Label CreateTableTitleLabel(string text)
        {
            Label label = new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                BackColor = Color.Transparent,
                ForeColor = UIStyles.Colors.YellowLighter,
                Font = new Font(UIStyles.Fonts.Normal.FontFamily, 16f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 10)
            };
            return label;
        }

        /// <summary>
        /// Title row (fixed height, same as every other block) above a
        /// boxed, darker-toned panel - same tone as the settings property
        /// table below it - that Dock=Fills whatever height its row ends up
        /// getting, so it visibly grows to soak up the leftover space in
        /// the right column instead of leaving a blank gap.
        /// </summary>
        private (Control Wrapper, Label TitleLabel, Label BodyLabel) CreateInstructionsBlock()
        {
            TableLayoutPanel wrapper = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                MinimumSize = new Size(RightColumnWidth, 0),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, RightColumnItemGap),
                ColumnCount = 1,
                RowCount = 2
            };
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            wrapper.RowStyles.Add(new RowStyle(SizeType.Absolute, TableTitleRowHeight));
            wrapper.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label titleLabel = CreateTableTitleLabel(AppLocalization.Get("Home.InstructionsTitle"));
            titleLabel.Anchor = AnchorStyles.None;
            wrapper.Controls.Add(titleLabel, 0, 0);

            Panel box = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UIStyles.Colors.BackgroundMedium,
                Padding = new Padding(12),
                Margin = new Padding(0)
            };

            // AutoSize alone won't wrap - without a MaximumSize.Width, a
            // Label computes its "preferred" size as one long single line
            // before Dock ever gets a say, so the width cap has to be set
            // explicitly here to force it to wrap within the box.
            Label bodyLabel = new Label
            {
                Text = AppLocalization.Get("Home.InstructionsBody"),
                AutoSize = true,
                MaximumSize = new Size(RightColumnWidth - box.Padding.Horizontal, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.Transparent,
                ForeColor = UIStyles.Colors.TextPrimary,
                Font = new Font(UIStyles.Fonts.Normal.FontFamily, 12f),
                Margin = new Padding(0)
            };
            box.Controls.Add(bodyLabel);

            wrapper.Controls.Add(box, 0, 1);

            return (wrapper, titleLabel, bodyLabel);
        }

        private static Control CreateTitledTable(Label title, Control table)
        {
            // Absolute title row (same fixed height for every table, not
            // AutoSize) + AutoSize table row, so the title sits directly
            // above its table and their header rows start at exactly the
            // same Y-offset regardless of which side has more rows or what
            // the title text itself measures out to.
            TableLayoutPanel wrapper = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.None,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 2
            };
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            wrapper.RowStyles.Add(new RowStyle(SizeType.Absolute, TableTitleRowHeight));
            wrapper.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            title.Anchor = AnchorStyles.None;
            table.Anchor = AnchorStyles.None;

            wrapper.Controls.Add(title, 0, 0);
            wrapper.Controls.Add(table, 0, 1);

            return wrapper;
        }

        /// <summary>
        /// historyTable plus one extra row directly underneath, always
        /// showing the all-time best amount (not subject to the 20-entry
        /// cap the way the regular history rows are) with its value in
        /// yellow so it stands out from the rest of the table.
        /// </summary>
        private (Control Wrapper, Label NameLabel, Label CaseLabel, Label AmountLabel) CreateHistoryTableWithAllTimeBest()
        {
            TableLayoutPanel wrapper = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.None,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 2
            };
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            wrapper.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            wrapper.RowStyles.Add(new RowStyle(SizeType.Absolute, TableRowHeight));

            historyTable.Anchor = AnchorStyles.None;
            wrapper.Controls.Add(historyTable, 0, 0);

            TableLayoutPanel bestRow = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.None,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                ColumnCount = 3,
                RowCount = 1
            };
            bestRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, HistoryDateColumnWidth));
            bestRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, HistoryCaseColumnWidth));
            bestRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, HistoryResultColumnWidth));
            bestRow.RowStyles.Add(new RowStyle(SizeType.Absolute, TableRowHeight));

            Label nameLabel = new Label
            {
                Text = NoRecordYetPlaceholder,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(0),
                Font = new Font(UIStyles.Fonts.Normal.FontFamily, TableRowFontSize),
                ForeColor = UIStyles.Colors.YellowLighter,
                BackColor = historyTable.RowBackColor,
                AutoEllipsis = true
            };

            Label amountLabel = new Label
            {
                Text = NoRecordYetPlaceholder,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(0),
                Font = new Font(UIStyles.Fonts.Normal.FontFamily, TableRowFontSize, FontStyle.Bold),
                ForeColor = UIStyles.Colors.YellowLighter,
                BackColor = historyTable.RowBackColor,
                AutoEllipsis = true
            };

            Label caseLabel = new Label
            {
                Text = NoRecordYetPlaceholder,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(0),
                Font = new Font(UIStyles.Fonts.Normal.FontFamily, TableRowFontSize),
                ForeColor = UIStyles.Colors.YellowLighter,
                BackColor = historyTable.RowBackColor,
                AutoEllipsis = true
            };

            bestRow.Controls.Add(nameLabel, 0, 0);
            bestRow.Controls.Add(caseLabel, 1, 0);
            bestRow.Controls.Add(amountLabel, 2, 0);

            wrapper.Controls.Add(bestRow, 0, 1);

            return (wrapper, nameLabel, caseLabel, amountLabel);
        }

        private static string FormatOrPlaceholder(decimal? amount)
        {
            return amount.HasValue ? AppCurrencyFormatter.Format(amount.Value, "#,0") : NoRecordYetPlaceholder;
        }
    }
}
