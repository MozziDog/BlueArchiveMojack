using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logic;

public class BulletVisual : MonoBehaviour
{
    Bullet _bulletLogic;
    BattleSceneManager _battleManager;

    public Bullet BulletLogic
    {
        get
        {
            return _bulletLogic;
        }
        set
        {
            if(BulletLogic != null)
            {
                _bulletLogic.OnExpired -= DestroyVisual;
            }
            _bulletLogic = value;
            _bulletLogic.OnExpired += DestroyVisual;
        }
    }


    // Update is called once per frame
    void Update()
    {
        Position2 pos = BulletLogic.Position;
        transform.position = new Vector3(pos.x, 0, pos.y);
    }

    void DestroyVisual()
    {
        Destroy(gameObject);
    }
}
