using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Logic
{
    [Serializable]
    public class CharacterGroup : IEnumerable<Character>
    {
        public List<Character> elements = new List<Character>();

        public int Count
        {
            get { return elements.Count; }
        }

        public Character this[int i]
        {
            get { return elements[i]; }
            set { elements[i] = value; }
        }

        public void Add(Character newCharacter)
        {
            elements.Add(newCharacter);
        }

        public void Remove(Character deadCharacter)
        {
            elements.Remove(deadCharacter);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        public IEnumerator<Character> GetEnumerator()
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