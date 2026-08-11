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
                ["Game.AcceptOrContinue"] = "Accept the offer, or keep going?",
                ["Game.OpenMoreCases"] = "Open {0} more cases until the banker's offer!",
                ["Game.MakeDecision"] = "Make your decision!",
                ["Game.Over"] = "Game over!",

                ["GameOver.Prefix"] = "You have",
                ["GameOver.Suffix"] = "won!",
                ["GameOver.Restart"] = "Restart",
                ["GameOver.OwnCaseContained"] = "Your own case contained: {0}",

                ["BankerOffer.Placeholder"] = "Offer",

                ["FinalChoice.Prompt"] = "Keep or swap your case?",

                ["OpenCase.ClickToSkip"] = "Click to skip!",
                ["OpenCase.ClickToClose"] = "Click to close!",

                ["Options.MenuButton"] = "Options",
                ["Options.Title"] = "Options",
                ["Options.Language"] = "Language",
                ["Options.Currency"] = "Currency",

                ["Game.ResetButton"] = "Reset game",
            });

            AppLocalization.Register(AppLanguage.German, new Dictionary<string, string>
            {
                ["Game.ChooseCase"] = "Wähle deinen Koffer aus!",
                ["Game.YourCase"] = "Dein Koffer...",
                ["Game.MyCaseLabel"] = "Mein Koffer:",
                ["Game.OffersLabel"] = "Angebote:",
                ["Game.BankerCalculating"] = "Der Bänker rechnet!",
                ["Game.AcceptOrContinue"] = "Angebot annehmen oder weiter ins Risiko?",
                ["Game.OpenMoreCases"] = "Öffne {0} Koffer bis zum Angebot des Bänkers!",
                ["Game.MakeDecision"] = "Triff deine Entscheidung!",
                ["Game.Over"] = "Spiel beendet!",

                ["GameOver.Prefix"] = "Du hast",
                ["GameOver.Suffix"] = "gewonnen!",
                ["GameOver.Restart"] = "Neu starten",
                ["GameOver.OwnCaseContained"] = "Dein eigener Koffer enthielt: {0}",

                ["BankerOffer.Placeholder"] = "Angebot",

                ["FinalChoice.Prompt"] = "Koffer behalten oder tauschen?",

                ["OpenCase.ClickToSkip"] = "Klicken zum Überspringen!",
                ["OpenCase.ClickToClose"] = "Klicken zum Schließen!",

                ["Options.MenuButton"] = "Optionen",
                ["Options.Title"] = "Optionen",
                ["Options.Language"] = "Sprache",
                ["Options.Currency"] = "Währung",

                ["Game.ResetButton"] = "Spiel zurücksetzen",
            });
        }
    }
}
