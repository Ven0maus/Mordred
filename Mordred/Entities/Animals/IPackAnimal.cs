using System.Collections.Generic;

namespace Mordred.Entities.Animals
{
    public interface IPackAnimal<T> where T : Animal
    {
        List<T> PackMates { get; set; }
    }
}
