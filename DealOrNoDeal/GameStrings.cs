using System.Collections.Generic;
using CustomWFUI;

namespace DealOrNoDeal
{
    /// <summary>
    /// All of this game's own translated text, registered with
    /// CustomWFUI's AppLocalization once at startup. "Deal Or No Deal ?" on
    /// the banker-offer screen is deliberately left untranslated - it's the
    /// show's own name, not translated in the real show's German version
    /// either.
    /// </summary>
    internal static class GameStrings
    {
        public static void Register()
        {
            AppLocalization.Register(AppLanguage.English, new Dictionary<string, string>
            {
                ["Game.ChooseCase"] = "Choose your case!",
                ["Game.YourCase"] = "Your case...",
                ["Game.MyCaseLabel"] = "My Case:",
                ["Game.OffersLabel"] = "Offers:",
                ["Game.BankerCalculating"] = "The banker is calculating!",
                ["Game.YourOffer"] = "Your offer",
                ["Game.AcceptOrContinue"] = "Accept the offer, or keep going?",
                ["Game.OpenMoreCases"] = "Open {0} more cases until the banker's offer!",
                ["Game.MakeDecision"] = "Make your decision!",
                ["Game.Over"] = "Game complete!",

                ["App.AlreadyRunning"] = "Deal or No Deal is already running.",

                ["GameOver.Prefix"] = "Your winnings:",
                ["GameOver.Restart"] = "Restart",
                ["GameOver.MainMenu"] = "Main menu",
                ["GameOver.OwnCaseContained"] = "Your own case contained: {0}",

                ["BankerOffer.Placeholder"] = "Offer",
                ["BankerOffer.CasesUntilNext"] = "({0} cases until the next offer)",
                ["BankerOffer.FinalOffer"] = "(last offer)",
                ["BankerOffer.ClickToSkip"] = "Click to skip",

                ["FinalChoice.Prompt"] = "Keep or swap your case?",
                ["FinalChoice.Keep"] = "Keep",
                ["FinalChoice.Swap"] = "Swap",

                ["OpenCase.ClickToSkip"] = "Click to skip",
                ["OpenCase.ClickToClose"] = "Click to close",

                ["Options.Language"] = "Language",
                ["Options.Currency"] = "Currency",

                ["Game.MainMenuButton"] = "Main menu",
                ["Game.ConfirmMainMenuTitle"] = "Return to main menu?",
                ["Game.ConfirmMainMenuMessage"] = "Your current game will be lost. Continue?",

                ["Home.HistoryTitle"] = "History",
                ["Home.SettingsTitle"] = "Settings",
                ["Home.InstructionsTitle"] = "How to Play",
                ["Home.InstructionsBody"] = "• Pick one case as your own.\n• Open the other cases to rule out amounts.\n• Every few cases, the banker offers to buy your case - deal or no deal?\n• Keep going for a shot at more, or accept and bank it.\n• At the end, keep your case or swap it for the last one left.\n• Goal: win as much money as possible.",
                ["Home.DateColumn"] = "Date",
                ["Home.CaseColumn"] = "Case",
                ["Home.ResultColumn"] = "Result",
                ["Home.AllTimeBestRow"] = "All-Time Best",
                ["Home.PlayButton"] = "Play",
            });

            AppLocalization.Register(AppLanguage.German, new Dictionary<string, string>
            {
                ["Game.ChooseCase"] = "Wähle deinen Koffer aus!",
                ["Game.YourCase"] = "Dein Koffer...",
                ["Game.MyCaseLabel"] = "Mein Koffer:",
                ["Game.OffersLabel"] = "Angebote:",
                ["Game.BankerCalculating"] = "Der Bänker rechnet!",
                ["Game.YourOffer"] = "Dein Angebot",
                ["Game.AcceptOrContinue"] = "Angebot annehmen oder weiter ins Risiko?",
                ["Game.OpenMoreCases"] = "Öffne {0} Koffer bis zum Angebot des Bänkers!",
                ["Game.MakeDecision"] = "Triff deine Entscheidung!",
                ["Game.Over"] = "Spiel beendet!",

                ["App.AlreadyRunning"] = "Deal or No Deal läuft bereits.",

                ["GameOver.Prefix"] = "Dein Gewinn:",
                ["GameOver.Restart"] = "Neu starten",
                ["GameOver.MainMenu"] = "Hauptmenü",
                ["GameOver.OwnCaseContained"] = "Dein eigener Koffer enthielt: {0}",

                ["BankerOffer.Placeholder"] = "Angebot",
                ["BankerOffer.CasesUntilNext"] = "({0} Koffer bis zum nächsten Angebot)",
                ["BankerOffer.FinalOffer"] = "(letztes Angebot)",
                ["BankerOffer.ClickToSkip"] = "Klicken zum Überspringen",

                ["FinalChoice.Prompt"] = "Koffer behalten oder tauschen?",
                ["FinalChoice.Keep"] = "Behalten",
                ["FinalChoice.Swap"] = "Tauschen",

                ["OpenCase.ClickToSkip"] = "Klicken zum Überspringen",
                ["OpenCase.ClickToClose"] = "Klicken zum Schließen",

                ["Options.Language"] = "Sprache",
                ["Options.Currency"] = "Währung",

                ["Game.MainMenuButton"] = "Hauptmenü",
                ["Game.ConfirmMainMenuTitle"] = "Zurück zum Hauptmenü?",
                ["Game.ConfirmMainMenuMessage"] = "Dein aktuelles Spiel geht verloren. Fortfahren?",

                ["Home.HistoryTitle"] = "Verlauf",
                ["Home.SettingsTitle"] = "Einstellungen",
                ["Home.InstructionsTitle"] = "Spielanleitung",
                ["Home.InstructionsBody"] = "• Wähle einen Koffer als deinen eigenen.\n• Öffne die anderen Koffer, um Beträge auszuschließen.\n• Alle paar Koffer bietet dir der Bänker Geld für deinen Koffer - Deal or No Deal?\n• Spiel weiter für die Chance auf mehr, oder nimm an und sicher dir den Betrag.\n• Am Ende behältst du deinen Koffer oder tauschst ihn gegen den letzten übrigen.\n• Ziel: so viel Geld wie möglich gewinnen.",
                ["Home.DateColumn"] = "Datum",
                ["Home.CaseColumn"] = "Koffer",
                ["Home.ResultColumn"] = "Ergebnis",
                ["Home.AllTimeBestRow"] = "Allzeit-Bestwert",
                ["Home.PlayButton"] = "Spielen",
            });
        }
    }
}
