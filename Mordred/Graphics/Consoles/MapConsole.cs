using Microsoft.Xna.Framework;
using Mordred.WorldGen;
using SadConsole;

namespace Mordred.Graphics.Consoles
{
    public class MapConsole : ScrollingConsole
    {
        public static World World { get; private set; }

        public MapConsole(int width, int height, Font font, Rectangle viewport) : base(width, height, font, viewport)
        { }

        public void InitializeWorld()
        {
            World = new World(Width, Height);
            World.GenerateLands();
            World.GenerateVillages();
            World.Render(true, true);
        }
    }
}
