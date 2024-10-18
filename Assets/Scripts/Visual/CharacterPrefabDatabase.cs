using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu()]
public class CharacterPrefabDatabase : SerializedScriptableObject
{
    public Dictionary<string, GameObject> CharacterViews;
}
