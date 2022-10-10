using Mordred.Graphics.Consoles;
using SadConsole;
using System.Collections.Generic;
using System.Linq;

namespace Mordred.Graphics
{
    /// <summary>
    /// The general container where all windows of the game are stored.
    /// </summary>
    public class Container : Console
    {
        private readonly List<Console> Consoles = new List<Console>();

        public Container(int gameWindowWidth, int gameWindowHeight) : base(gameWindowWidth, gameWindowHeight)
        { }

        public void InitializeConsoles()
        {
            Consoles.Add(new MapConsole(Width, Height));

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
