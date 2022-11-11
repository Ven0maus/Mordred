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
    public class WorldWindow : Console
    { 
        private static WorldWindow _instance;
        public static WorldWindow Instance { get { return _instance; } }
        public static World World { get; private set; }
        public static Player Player { get; private set; }
        public Renderer EntityRenderer { get; private set; }

        private readonly Console _terrainConsole, _objectsConsole;

        public WorldWindow(int width, int height) : base(width, height)
        {
            if (Instance != null)
                throw new Exception("Can only have one instance of world window!");

            _instance = this;

            // Create terrain console
            _terrainConsole = new Console(width, height)
            {
                Font = GameHost.Instance.DefaultFont,
                FontSize = GameHost.Instance.DefaultFont.GetFontSize(IFont.Sizes.One)
            };
            Children.Add(_terrainConsole);

            // Create overlayed objects console
            _objectsConsole = new Console(width, height)
            {
                Font = GameHost.Instance.DefaultFont,
                FontSize = GameHost.Instance.DefaultFont.GetFontSize(IFont.Sizes.One)
            };
            Children.Add(_objectsConsole);

            // Add entity renderer component to the last rendered child console
            EntityRenderer = new Renderer();
            Children[Children.Count - 1].SadComponents.Add(EntityRenderer);
        }

        public void OnTerrainUpdate(object sender, CellUpdateArgs<int, WorldCell> args)
        {
            UpdateSurface(_terrainConsole, args);
        }

        public void OnObjectUpdate(object sender, CellUpdateArgs<int, WorldCell> args)
        {
            UpdateSurface(_objectsConsole, args);
        }

        private static void UpdateSurface(Console console, CellUpdateArgs<int, WorldCell> args)
        {
            console.Surface.SetGlyph(args.ScreenX, args.ScreenY, args.Cell);
            console.Surface[args.ScreenX, args.ScreenY].IsVisible = args.Cell.IsVisible;
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

        public void InitializeWorld()
        {
            World = new World(Width, Height, Constants.WorldSettings.Seed);
            World.Initialize();

            // Spawn player
            SpawnPlayer();

            // Initial world loading uses threading, after it can be adjusted
            World.UseThreading = Constants.GameSettings.UseThreading;

            // Apply world regrowth monitor
            Game.GameTick += WorldRegrowth.CheckRegrowthStatus;
        }

        private void SpawnPlayer()
        {
            var centerPos = new Point(Width / 2, Height / 2);
            // TODO: Find valid spot to spawn, right now we could spawn in an object
            var pos = World.GetCellCoordsFromCenter(WorldLayer.TERRAIN, Width / 2, Height / 2, a => a.Walkable).OrderBy(a => centerPos.SquaredDistance(a)).First();
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
