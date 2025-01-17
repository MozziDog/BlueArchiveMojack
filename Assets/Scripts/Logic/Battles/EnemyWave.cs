using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Logic
{
    public class EnemyWave
    {
        List<KeyValuePair<CharacterLogic, Position2>> elements = new List<KeyValuePair<CharacterLogic, Position2>>();

        public int Count
        {
            get { return elements.Count; }
        }

        public KeyValuePair<CharacterLogic, Position2> this[int i]
        {
            get { return elements[i]; }
            set { elements[i] = value; }
        }

        public void Add(KeyValuePair<CharacterLogic, Position2> newCharacter)
        {
            elements.Add(newCharacter);
        }

        public void Remove(CharacterLogic deadCharacter)
        {
            elements.RemoveAll(x => x.Key == deadCharacter);
        }
    }
}