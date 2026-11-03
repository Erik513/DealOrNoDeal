namespace DealOrNoDeal
{
    /// <summary>
    /// The keys of every translated text, as constants: a typo in a key is a
    /// compile error here instead of a raw key showing up on screen at run time.
    /// The texts themselves are in <see cref="GameStrings"/>.
    /// </summary>
    internal static class TextKeys
    {
        public static class Game
        {
            public const string ChooseCase = "Game.ChooseCase";
            public const string YourCase = "Game.YourCase";
            public const string MyCaseLabel = "Game.MyCaseLabel";
            public const string OffersLabel = "Game.OffersLabel";
            public const string BankerCalculating = "Game.BankerCalculating";
            public const string YourOffer = "Game.YourOffer";
            public const string AcceptOrContinue = "Game.AcceptOrContinue";
            public const string OpenMoreCaseSingular = "Game.OpenMoreCaseSingular";
            public const string OpenMoreCases = "Game.OpenMoreCases";
            public const string MakeDecision = "Game.MakeDecision";
            public const string Over = "Game.Over";
            public const string MainMenuButton = "Game.MainMenuButton";
            public const string ConfirmMainMenuTitle = "Game.ConfirmMainMenuTitle";
            public const string ConfirmMainMenuMessage = "Game.ConfirmMainMenuMessage";
        }

        public static class App
        {
            public const string AlreadyRunning = "App.AlreadyRunning";
        }

        public static class GameOver
        {
            public const string Prefix = "GameOver.Prefix";
            public const string Restart = "GameOver.Restart";
            public const string MainMenu = "GameOver.MainMenu";
            public const string OwnCaseContained = "GameOver.OwnCaseContained";
        }

        public static class BankerOffer
        {
            public const string Placeholder = "BankerOffer.Placeholder";
            public const string CaseUntilNextSingular = "BankerOffer.CaseUntilNextSingular";
            public const string CasesUntilNext = "BankerOffer.CasesUntilNext";
            public const string FinalOffer = "BankerOffer.FinalOffer";
            public const string ClickToSkip = "BankerOffer.ClickToSkip";
        }

        public static class FinalChoice
        {
            public const string Prompt = "FinalChoice.Prompt";
            public const string Keep = "FinalChoice.Keep";
            public const string Swap = "FinalChoice.Swap";
        }

        public static class OpenCase
        {
            public const string ClickToSkip = "OpenCase.ClickToSkip";
            public const string ClickToClose = "OpenCase.ClickToClose";
        }

        public static class Options
        {
            public const string Language = "Options.Language";
            public const string Currency = "Options.Currency";
        }

        public static class Home
        {
            public const string HistoryTitle = "Home.HistoryTitle";
            public const string SettingsTitle = "Home.SettingsTitle";
            public const string InstructionsTitle = "Home.InstructionsTitle";
            public const string InstructionsBody = "Home.InstructionsBody";
            public const string DateColumn = "Home.DateColumn";
            public const string CaseColumn = "Home.CaseColumn";
            public const string ResultColumn = "Home.ResultColumn";
            public const string AllTimeBestRow = "Home.AllTimeBestRow";
            public const string PlayButton = "Home.PlayButton";
        }
    }
}
