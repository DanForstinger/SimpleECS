using UnityEngine;
using System.Collections.Generic;

namespace SimpleECS
{
    public class Entity
    {
        public string ID { get; private set; }
        public List<string> Components;

        public Entity(string id)
        {
            ID = id;
            Components = new List<string>();
        }
    }
}