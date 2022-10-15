namespace Mordred
{
    public static class Constants
    {
        public static class GameSettings
        {
            public const bool DebugMode = true;
            public const int GameWindowWidth = 140;
            public const int GameWindowHeight = 50;
            public const float TimePerTickInSeconds = 0.25f;
        }

        public static class WorldSettings
        {
            public const int ChunkWidth = 100;
            public const int ChunkHeight = 100;
            public const int MinWildLife = 15;
            public const int MaxWildLife = 25;
        }

        public static class ActorSettings
        {
            public const int DefaultHungerTickRate = 10;
            public const int DefaultHealthRegenerationTickRate = 30;
            public const int DefaultPercentageHungerHealthRegen = 70;
            public const int DefaultMaxHunger = 100;
            public const int SecondsBeforeCorpsRots = 180;
            public const int BleedChanceFromAttack = 35;
            public const float DefaultSecondsPerBleeding = 1.5f;
            public const int StopBleedingAfterSeconds = 15;
            public const int MaxPackSize = 4;
        }

        public static class ActionSettings
        {
            public const int DefaultGatherTickRate = 8;
        }

        public static class VillageSettings
        {
            public const int MaxVillages = 4;
            public const int MaxItemStack = 64;
            public const int HumansPerHouse = 2;
        }
    }
}
