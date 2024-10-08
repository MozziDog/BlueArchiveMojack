using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Battle
{
    public Battle() { }

    public List<Character> characters;

    public Battle(BattleData battleData)
    {
        characters = new List<Character>(battleData.characters);
        InitBattle();
    }

    public void InitBattle()
    {
        Debug.Log("Init Battle");
        foreach(var ch in characters)
        {
            ch.Init();
        }
    }

    public void tick()
    {
        foreach(var ch in characters)
        {
            ch.Tick();
        }
    }
}
