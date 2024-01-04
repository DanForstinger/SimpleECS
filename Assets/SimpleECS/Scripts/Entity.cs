using UnityEngine;
using System.Collections.Generic;

namespace SimpleECS
{
    public class Entity
    {
        public int ID { get; private set; }
        public List<string> Components;

        public Entity(int id)
        {
            ID = id;
            Components = new List<string>();
        }
    }
}