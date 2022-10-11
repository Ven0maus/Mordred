﻿using Mordred.WorldGen;
using SadConsole;
using SadConsole.Entities;
using SadRogue.Primitives;
using Venomaus.FlowVitae.Basics;

namespace Mordred.Graphics.Consoles
{
    public class MapConsole : Console
    { 
        private static MapConsole _instance;
        public static MapConsole Instance { get { return _instance; } }
        public static World World { get; private set; }
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
        }

        public void InitializeWorld()
        {
            World = new World(Width, Height);
            World.GenerateLands();
            World.GenerateVillages();
            World.GenerateWildLife();
            World.Render(true);

            // Apply world regrowth monitor
            Game.GameTick += WorldRegrowth.CheckRegrowthStatus;
        }
    }
}
