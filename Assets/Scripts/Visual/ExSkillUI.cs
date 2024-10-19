using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logic;

public class ExSkillUI : MonoBehaviour
{
    [SerializeField] BattleLogic _battleManager;
    [SerializeField] TMP_Text _costCountText;
    [SerializeField] Slider _costRechargingGauge;
    [SerializeField] SkillCardUI[] _skillCardSlot;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        _costCountText.text = _battleManager.ExCostCount.ToString();
        _costRechargingGauge.value = _battleManager.ExCostCount + (float)_battleManager.ExCostRecharging / _battleManager.ExCostGaugePerCount;
        int i;
        for(i=0; i<_battleManager.skillCardHand.Count; i++)
        {
            _skillCardSlot[i].SetSkillCard(_battleManager.skillCardHand[i]);
        }
        for(; i<3; i++)
        {
            _skillCardSlot[i].DisableSkillCard();
        }
    }
}
