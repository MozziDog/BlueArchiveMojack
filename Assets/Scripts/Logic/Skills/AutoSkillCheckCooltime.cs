using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSkillCheckCooltime : IAutoSkillCheck
{
    int _cooldownTick;
    bool _skillAvailable = false;
    IEnumerator _cooldownCoroutine;

    public AutoSkillCheckCooltime(int cooldownTick)
    {
        this._cooldownTick = cooldownTick;
        _cooldownCoroutine = CooldownCoroutine();
    }

    public void CheckSkillCondition()
    {
        if(_cooldownCoroutine != null)
        {
            _cooldownCoroutine.MoveNext();
        }
    }

    IEnumerator CooldownCoroutine()
    {
        for(int i=0; i<_cooldownTick; i++)
        {
            yield return 0;
        }
        _skillAvailable = true;
        _cooldownCoroutine = null;
    }

    public bool CanUseSkill()
    {
        return _skillAvailable;
    }

    public void ResetSkillCondition()
    {
        _cooldownCoroutine = CooldownCoroutine();
    }
}
