using System.Collections.Generic;

namespace SimpleECS
{
    public abstract class Properties
    {
        public abstract Dictionary<string, object> Serialize();
        public abstract void Deserialize(Dictionary<string, object> dictionary);
    }
}