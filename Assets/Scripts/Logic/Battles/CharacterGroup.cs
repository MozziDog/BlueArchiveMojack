using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Logic
{
    [Serializable]
    public class CharacterGroup : IEnumerable<CharacterLogic>
    {
        public List<CharacterLogic> elements = new List<CharacterLogic>();

        public int Count
        {
            get { return elements.Count; }
        }

        public CharacterLogic this[int i]
        {
            get { return elements[i]; }
            set { elements[i] = value; }
        }

        public void Add(CharacterLogic newCharacter)
        {
            elements.Add(newCharacter);
        }

        public void Remove(CharacterLogic deadCharacter)
        {
            elements.Remove(deadCharacter);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        public IEnumerator<CharacterLogic> GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        public Position2 GetCenterPosition()
        {
            Position2 sum = Position2.zero;
            foreach (var ch in elements)
            {
                sum += ch.Position;
            }
            return sum / elements.Count;
        }
    }
}