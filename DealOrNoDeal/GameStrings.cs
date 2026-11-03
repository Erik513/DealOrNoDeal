using System.Collections.Generic;
using ErikwnkCore;

namespace DealOrNoDeal
{
    /// <summary>
    /// All of this game's own translated text, registered with
    /// ErikwnkCore's AppLocalization once at startup. "Deal Or No Deal ?" on
    /// the banker-offer screen is deliberately left untranslated - it's the
    /// show's own name, not translated in the real show's German version
    /// either.
    /// </summary>
    internal static class GameStrings
    {
        internal static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            [TextKeys.Game.ChooseCase] = "Choose your case!",
            [TextKeys.Game.YourCase] = "Your case...",
            [TextKeys.Game.MyCaseLabel] = "My Case:",
            [TextKeys.Game.OffersLabel] = "Offers:",
            [TextKeys.Game.BankerCalculating] = "The banker is calculating!",
            [TextKeys.Game.YourOffer] = "Your offer",
            [TextKeys.Game.AcceptOrContinue] = "Accept the offer, or keep going?",
            [TextKeys.Game.OpenMoreCaseSingular] = "Open {0} more case until the banker's offer!",
            [TextKeys.Game.OpenMoreCases] = "Open {0} more cases until the banker's offer!",
            [TextKeys.Game.MakeDecision] = "Make your decision!",
            [TextKeys.Game.Over] = "Game complete!",

            [TextKeys.App.AlreadyRunning] = "Deal or No Deal is already running.",

            [TextKeys.GameOver.Prefix] = "Your winnings:",
            [TextKeys.GameOver.Restart] = "Restart",
            [TextKeys.GameOver.MainMenu] = "Main menu",
            [TextKeys.GameOver.OwnCaseContained] = "Your own case contained: {0}",

            [TextKeys.BankerOffer.Placeholder] = "Offer",
            [TextKeys.BankerOffer.CaseUntilNextSingular] = "({0} case until the next offer)",
            [TextKeys.BankerOffer.CasesUntilNext] = "({0} cases until the next offer)",
            [TextKeys.BankerOffer.FinalOffer] = "(last offer)",
            [TextKeys.BankerOffer.ClickToSkip] = "Click to skip",

            [TextKeys.FinalChoice.Prompt] = "Keep or swap your case?",
            [TextKeys.FinalChoice.Keep] = "Keep",
            [TextKeys.FinalChoice.Swap] = "Swap",

            [TextKeys.OpenCase.ClickToSkip] = "Click to skip",
            [TextKeys.OpenCase.ClickToClose] = "Click to close",

            [TextKeys.Options.Language] = "Language",
            [TextKeys.Options.Currency] = "Currency",

            [TextKeys.Game.MainMenuButton] = "Main menu",
            [TextKeys.Game.ConfirmMainMenuTitle] = "Return to main menu?",
            [TextKeys.Game.ConfirmMainMenuMessage] = "Your current game will be lost. Continue?",

            [TextKeys.Home.HistoryTitle] = "History",
            [TextKeys.Home.SettingsTitle] = "Settings",
            [TextKeys.Home.InstructionsTitle] = "How to Play",
            [TextKeys.Home.InstructionsBody] = "• Pick one case as your own.\n• Open the other cases to rule out amounts.\n• Every few cases, the banker offers to buy your case - deal or no deal?\n• Keep going for a shot at more, or accept and bank it.\n• At the end, keep your case or swap it for the last one left.\n• Goal: win as much money as possible.",
            [TextKeys.Home.DateColumn] = "Date",
            [TextKeys.Home.CaseColumn] = "Case",
            [TextKeys.Home.ResultColumn] = "Result",
            [TextKeys.Home.AllTimeBestRow] = "All-Time Best",
            [TextKeys.Home.PlayButton] = "Play",
        };

        internal static readonly Dictionary<string, string> German = new Dictionary<string, string>
        {
            [TextKeys.Game.ChooseCase] = "Wähle deinen Koffer aus!",
            [TextKeys.Game.YourCase] = "Dein Koffer...",
            [TextKeys.Game.MyCaseLabel] = "Mein Koffer:",
            [TextKeys.Game.OffersLabel] = "Angebote:",
            [TextKeys.Game.BankerCalculating] = "Der Bänker rechnet!",
            [TextKeys.Game.YourOffer] = "Dein Angebot",
            [TextKeys.Game.AcceptOrContinue] = "Angebot annehmen oder weiter ins Risiko?",
            // "Koffer" is invariant here too (see BankerOffer.CaseUntilNextSingular).
            [TextKeys.Game.OpenMoreCaseSingular] = "Öffne {0} Koffer bis zum Angebot des Bänkers!",
            [TextKeys.Game.OpenMoreCases] = "Öffne {0} Koffer bis zum Angebot des Bänkers!",
            [TextKeys.Game.MakeDecision] = "Triff deine Entscheidung!",
            [TextKeys.Game.Over] = "Spiel beendet!",

            [TextKeys.App.AlreadyRunning] = "Deal or No Deal läuft bereits.",

            [TextKeys.GameOver.Prefix] = "Dein Gewinn:",
            [TextKeys.GameOver.Restart] = "Neu starten",
            [TextKeys.GameOver.MainMenu] = "Hauptmenü",
            [TextKeys.GameOver.OwnCaseContained] = "Dein eigener Koffer enthielt: {0}",

            [TextKeys.BankerOffer.Placeholder] = "Angebot",
            // "Koffer" is invariant here - German doesn't inflect this
            // noun for singular/plural (1 Koffer, 2 Koffer), so this is
            // deliberately identical to the plural entry below, unlike
            // the English "case"/"cases" pair it corresponds to.
            [TextKeys.BankerOffer.CaseUntilNextSingular] = "({0} Koffer bis zum nächsten Angebot)",
            [TextKeys.BankerOffer.CasesUntilNext] = "({0} Koffer bis zum nächsten Angebot)",
            [TextKeys.BankerOffer.FinalOffer] = "(letztes Angebot)",
            [TextKeys.BankerOffer.ClickToSkip] = "Klicken zum Überspringen",

            [TextKeys.FinalChoice.Prompt] = "Koffer behalten oder tauschen?",
            [TextKeys.FinalChoice.Keep] = "Behalten",
            [TextKeys.FinalChoice.Swap] = "Tauschen",

            [TextKeys.OpenCase.ClickToSkip] = "Klicken zum Überspringen",
            [TextKeys.OpenCase.ClickToClose] = "Klicken zum Schließen",

            [TextKeys.Options.Language] = "Sprache",
            [TextKeys.Options.Currency] = "Währung",

            [TextKeys.Game.MainMenuButton] = "Hauptmenü",
            [TextKeys.Game.ConfirmMainMenuTitle] = "Zurück zum Hauptmenü?",
            [TextKeys.Game.ConfirmMainMenuMessage] = "Dein aktuelles Spiel geht verloren. Fortfahren?",

            [TextKeys.Home.HistoryTitle] = "Verlauf",
            [TextKeys.Home.SettingsTitle] = "Einstellungen",
            [TextKeys.Home.InstructionsTitle] = "Spielanleitung",
            [TextKeys.Home.InstructionsBody] = "• Wähle einen Koffer als deinen eigenen.\n• Öffne die anderen Koffer, um Beträge auszuschließen.\n• Alle paar Koffer bietet dir der Bänker Geld für deinen Koffer - Deal or No Deal?\n• Spiel weiter für die Chance auf mehr, oder nimm an und sicher dir den Betrag.\n• Am Ende behältst du deinen Koffer oder tauschst ihn gegen den letzten übrigen.\n• Ziel: so viel Geld wie möglich gewinnen.",
            [TextKeys.Home.DateColumn] = "Datum",
            [TextKeys.Home.CaseColumn] = "Koffer",
            [TextKeys.Home.ResultColumn] = "Ergebnis",
            [TextKeys.Home.AllTimeBestRow] = "Allzeit-Bestwert",
            [TextKeys.Home.PlayButton] = "Spielen",
        };

        public static void Register()
        {
            AppLocalization.Register(AppLanguage.English, English);
            AppLocalization.Register(AppLanguage.German, German);
        }
    }
}
