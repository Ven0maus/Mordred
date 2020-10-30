namespace Mordred
{
    public static class Constants
    {
        public static class GameSettings
        {
            public const int GameWindowWidth = 140;
            public const int GameWindowHeight = 50;
            public const float TimePerTickInSeconds = 0.25f;
        }

        public static class WorldSettings
        {
            public const int MinWildLife = 10;
            public const int MaxWildLife = 18;
        }

        public static class ActorSettings
        {
            public const int DefaultHungerTickRate = 10;
            public const int DefaultMaxHunger = 75;
            public const int SecondsBeforeCorpsRots = 180;
        }

        public static class ActionSettings
        {
            public const int DefaultGatherTickRate = 8;
        }

        public static class VillageSettings
        {
            public const int MaxVillages = 4;
            public const int MaxItemStack = 64;
            public const int TribemenPerHut = 2;
        }
    }
}
