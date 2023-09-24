using Mordred.Graphics;
using SadConsole;
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
            SetSettings();

            SadConsole.Game.Configuration gameStartup = new SadConsole.Game.Configuration()
                .SetScreenSize(Constants.GameSettings.GameWindowWidth, Constants.GameSettings.GameWindowHeight)
                .OnStart(Init)
                .UseFrameUpdateEvent(Instance_FrameUpdate)
                .IsStartingScreenFocused(false)
                .SetStartingScreen<Container>();

            SadConsole.Game.Create(gameStartup);
            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }

        private static void Instance_FrameUpdate(object sender, SadConsole.GameHost e)
        {
            _timeSinceLastTick -= e.UpdateFrameDelta.Milliseconds;
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
            SadConsole.Game.Instance.DestroyDefaultStartingConsole();

            // Initialize random with (TODO: seed)
            Random = new Random();
            TicksPerSecond = (int)Math.Round(1f / Constants.GameSettings.TimePerTickInSeconds);

            Container = (Container)SadConsole.Game.Instance.Screen;
            Container.InitializeConsoles();
            Container.InitializeGame();
        }

        private static void SetSettings()
        {
            Settings.WindowTitle = "Mordred";
            Settings.ResizeMode = Settings.WindowResizeOptions.Stretch;
            Settings.AllowWindowResize = true;
        }
    }
}
