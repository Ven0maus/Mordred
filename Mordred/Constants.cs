﻿namespace Mordred
{
    public static class Constants
    {
        public static class GameSettings
        {
            public const bool DebugMode = false;
            public const int GameWindowWidth = 140;
            public const int GameWindowHeight = 50;
            public const float TimePerTickInSeconds = 0.25f;
        }

        public static class WorldSettings
        {
            public const int ChunkWidth = 100;
            public const int ChunkHeight = 100;
            public const int RegrowthStatusCheckTimeInSeconds = 10;

            public static class WildLife
            {
                public const int MinWildLifePerChunk = 4;
                public const int MaxWildLifePerChunk = 10;
                public const int PercentagePredators = 25;
            }

            public static class Resources
            {
                public const int MinResourcePerChunk = 20;
            }
        }

        public static class ActorSettings
        {
            public const int DefaultHungerTickRate = 10;
            public const int DefaultHealthRegenerationTickRate = 12;
            public const int DefaultPercentageHungerHealthRegen = 70;
            public const int DefaultMaxHunger = 100;
            public const int SecondsBeforeCorpsRots = 180;
            public const int BleedChanceFromAttack = 35;
            public const float DefaultSecondsPerBleeding = 1.5f;
            public const int StopBleedingAfterSeconds = 15;
            public const int MaxPackSize = 4;
            public const int PathingWidth = 40;
            public const int PathingHeight = 20;
            public static int LookForFoodAtHungerPercentage = 30;
        }

        public static class ActionSettings
        {
            public const int DefaultGatherTickRate = 8;
        }

        public static class VillageSettings
        {
            public const int MaxVillagesPerChunk = 1;
            public const int MaxItemStack = 64;
            public const int HumansPerHouse = 2;
        }
    }
}
