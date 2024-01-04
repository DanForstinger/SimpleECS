using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleECS
{
    public interface IDictionarySerializable
    {
        void FromDictionary(Dictionary<string, object> dict, World world);
        Dictionary<string, object> ToDictionary(World world);
    }
}