using Microsoft.Xna.Framework;
using Mordred.Graphics;
using System;

namespace Mordred
{
    public class Game
    {
        public static Random Random { get; private set; }
        public static Container Container { get; private set; }

        public static event EventHandler GameTick;

        public static int TicksPerSecond { get; private set; }

        private static float _timeSinceLastTick;

        static void Main()
        {
            // Setup the engine and create the main window.
            SadConsole.Game.Create(Constants.GameSettings.GameWindowWidth, Constants.GameSettings.GameWindowHeight);

            // Hook the start event so we can add consoles to the system.
            SadConsole.Game.OnInitialize = Init;
            SadConsole.Game.OnUpdate = Update;

            // Start the game.
            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }

        private static void Update(GameTime obj)
        {
            _timeSinceLastTick -= obj.ElapsedGameTime.Milliseconds;
            if (_timeSinceLastTick <= 0)
            {
                _timeSinceLastTick = Constants.GameSettings.TimePerTickInSeconds * 1000;
                GameTick?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Base initialize method to setup the MapScreen as current screen
        /// </summary>
        private static void Init()
        {
            // Initialize random with (TODO: seed)
            Random = new Random();

            TicksPerSecond = (int)Math.Round(1f / Constants.GameSettings.TimePerTickInSeconds);

            // Create a container console, that contains all the game consoles
            Container = new Container(Constants.GameSettings.GameWindowWidth, Constants.GameSettings.GameWindowHeight);
            Container.InitializeConsoles();
            Container.InitializeGame();

            // Set container as the current/main screen
            SadConsole.Global.CurrentScreen = Container;
        }
    }
}
