using Mordred.Entities;
using Mordred.WorldGen;
using SadConsole;
using SadConsole.Entities;
using SadRogue.Primitives;
using System.Linq;
using Venomaus.FlowVitae.Basics;

namespace Mordred.Graphics.Consoles
{
    public class MapConsole : Console
    { 
        private static MapConsole _instance;
        public static MapConsole Instance { get { return _instance; } }
        public static World World { get; private set; }
        public static Player Player { get; private set; }
        public Renderer EntityRenderer { get; private set; }

        public MapConsole(int width, int height) : base(width, height)
        {
            Font = GameHost.Instance.DefaultFont;
            FontSize = Font.GetFontSize(IFont.Sizes.One);
            EntityRenderer = new Renderer();
            SadComponents.Add(EntityRenderer);
            _instance = this;
        }

        public void OnCellUpdate(object sender, CellUpdateArgs<int, WorldCell> args)
        {
            Surface.SetGlyph(args.ScreenX, args.ScreenY, args.Cell);
            Surface[args.ScreenX, args.ScreenY].IsVisible = args.Cell.IsVisible;
        }

        public void InitializeWorld()
        {
            World = new World(Width, Height);
            //World.GenerateVillages();
            //World.GenerateWildLife();
            //World.HideObstructedCells();

            // Spawn player
            SpawnPlayer();

            // Apply world regrowth monitor
            Game.GameTick += WorldRegrowth.CheckRegrowthStatus;
        }

        private void SpawnPlayer()
        {
            var centerPos = new Point(Width / 2, Height / 2);
            var pos = World.GetCellCoords(a => a.Walkable).OrderBy(a => centerPos.SquaredDistance(a)).First();
            Player = new Player(pos, false)
            {
                IsFocused = true
            };
            EntitySpawner.Spawn(Player);

            // The initial center doesn't need to be off-threaded
            World.UseThreading = false;
            World.Center(Player.Position.X, Player.Position.Y);
            World.UseThreading = true;
        }
    }
}
