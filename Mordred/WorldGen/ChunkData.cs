using Venomaus.FlowVitae.Chunking;

namespace Mordred.WorldGen
{
    public class ChunkData : IChunkData
    {
        public int Seed { get; set; }
        public (int x, int y) ChunkCoordinate { get; set; }

        public double[] HeatMap, MoistureMap;

        public ChunkData(double[] heatMap, double[] moistureMap)
        {
            HeatMap = heatMap;
            MoistureMap = moistureMap;
        }
    }
}
