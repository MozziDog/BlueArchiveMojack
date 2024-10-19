using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logic;

public class ExSkillUI : MonoBehaviour
{
    public BattleSceneManager _battleManager;

    [SerializeField] TMP_Text _costCountText;
    [SerializeField] Slider _costRechargingGauge;
    [SerializeField] SkillCardUI[] _skillCardSlot;

    BattleLogic _battleLogic;

    // Start is called before the first frame update
    void Awake()
    {
        _battleManager.OnBattleBegin += SetBattleLogic;
    }

    void SetBattleLogic(BattleLogic battle)
    {
        _battleLogic = battle;
    }

    // Update is called once per frame
    void Update()
    {
        _costCountText.text = _battleLogic.ExCostCount.ToString();
        _costRechargingGauge.value = _battleLogic.ExCostCount + (float)_battleLogic.ExCostRecharging / _battleLogic.ExCostGaugePerCount;
        int i;
        for(i=0; i<_battleLogic.skillCardHand.Count; i++)
        {
            _skillCardSlot[i].SetSkillCard(_battleLogic.skillCardHand[i]);
        }
        for(; i<3; i++)
        {
            _skillCardSlot[i].DisableSkillCard();
        }
    }
}
