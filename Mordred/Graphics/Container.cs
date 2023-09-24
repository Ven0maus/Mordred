using Mordred.Graphics.Consoles;
using SadConsole;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Graphics
{
    /// <summary>
    /// The general container where all windows of the game are stored.
    /// </summary>
    public class Container : ScreenObject
    {
        private readonly List<Console> Consoles = new List<Console>();
        private readonly int _width, _height;

        public Container()
        { 
            _width = Constants.GameSettings.GameWindowWidth;
            _height = Constants.GameSettings.GameWindowHeight;
        }

        public void InitializeConsoles()
        {
            Consoles.Add(new MapConsole(_width, _height));

            // Add all consoles as children of the container
            foreach (var console in Consoles)
                Children.Add(console);
        }

        public void InitializeGame()
        {
            var mapConsole = GetConsole<MapConsole>();
            mapConsole.InitializeWorld();
        }

        /// <summary>
        /// Get the console of the given type or null if it doesn't exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConsole<T>() where T : Console
        {
            return Consoles.OfType<T>().FirstOrDefault();
        }
    }
}
