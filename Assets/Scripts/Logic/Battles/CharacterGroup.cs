using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public Vector3 GetCenterPosition()
    {
        Vector3 sum = Vector3.zero;
        foreach(var ch in elements)
        {
            sum += ch.transform.position;
        }
        return sum / elements.Count;
    }
}
