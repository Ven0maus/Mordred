using SadRogue.Primitives;

namespace Mordred.Entities
{
    public interface IEntity
    {
        Point Position { get; set; }
        Point WorldPosition { get; set; }
        bool IsVisible { get; set; }
        void UnSubscribe();
    }
}
