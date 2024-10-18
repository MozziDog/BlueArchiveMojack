using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logic;

public class SkillCardUI : MonoBehaviour
{
    [SerializeField] TMP_Text display;
    [SerializeField] Button button;

    public void SetSkillCard(Character character)
    {
        button.interactable = true;
        display.text = character.Name;
    }

    public void DisableSkillCard()
    {
        button.interactable = false;
    }
}
