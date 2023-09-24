using Mordred.Entities;
using Mordred.WorldGen;
using SadConsole;
using SadConsole.Entities;
using SadRogue.Primitives;
using System;
using System.Linq;
using Venomaus.FlowVitae.Grids;
using Console = SadConsole.Console;

namespace Mordred.Graphics.Consoles
{
    public class MapConsole : Console
    { 
        private static MapConsole _instance;
        public static MapConsole Instance { get { return _instance; } }
        public static World World { get; private set; }
        public static Player Player { get; private set; }
        public EntityManager EntityRenderer { get; private set; }

        public MapConsole(int width, int height) : base(width, height)
        {
            Font = GameHost.Instance.DefaultFont;
            FontSize = Font.GetFontSize(IFont.Sizes.One);
            EntityRenderer = new EntityManager();
            SadComponents.Add(EntityRenderer);
            _instance = this;
        }

        public override void Update(TimeSpan delta)
        {
            base.Update(delta);

            if (Children.IsLocked) return;
            if (EntitySpawner.EntitiesToBeAdded.Count > 0)
            {
                var entitiesToHandle = EntitySpawner.EntitiesToBeAdded.ToArray();
                foreach (var entity in entitiesToHandle)
                {
                    Children.Add(entity);
                    EntityRenderer.Add(entity);
                }
                EntitySpawner.EntitiesToBeAdded.RemoveAll(a => entitiesToHandle.Contains(a));
            }
            if (EntitySpawner.EntitiesToBeRemoved.Count > 0)
            {
                var entitiesToHandle = EntitySpawner.EntitiesToBeRemoved.ToArray();
                foreach (var entity in entitiesToHandle)
                {
                    Children.Remove(entity);
                    EntityRenderer.Remove(entity);
                }
                EntitySpawner.EntitiesToBeRemoved.RemoveAll(a => entitiesToHandle.Contains(a));
            }
        }

        public void OnCellUpdate(object sender, CellUpdateArgs<int, WorldCell> args)
        {
            Surface[args.ScreenX, args.ScreenY].CopyAppearanceFrom(args.Cell, false);
            Surface[args.ScreenX, args.ScreenY].IsVisible = args.Cell.IsVisible;
        }

        public void InitializeWorld()
        {
            World = new World(Width, Height, 1000);
            World.Initialize();

            // Spawn player
            SpawnPlayer();

            // Apply world regrowth monitor
            Game.GameTick += WorldRegrowth.CheckRegrowthStatus;
        }

        private void SpawnPlayer()
        {
            var centerPos = new Point(Width / 2, Height / 2);
            var pos = World.GetCellCoordsFromCenter(Width / 2, Height / 2, a => a.Walkable).OrderBy(a => centerPos.SquaredDistance(a)).First();
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
