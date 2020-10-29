using System.Collections.Generic;

namespace Mordred.Entities.Animals
{
    public interface IPackAnimal<T> : IPackAnimal where T : Animal
    {
        new List<T> PackMates { get; set; }
    }

    public interface IPackAnimal 
    { 
        List<Animal> PackMates { get; }
    }
}
