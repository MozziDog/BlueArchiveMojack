using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logic;

public class BulletVisual : MonoBehaviour
{
    public Bullet BulletLogic;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Position2 pos = BulletLogic.Position;
        transform.position = new Vector3(pos.x, 0, pos.y);
    }
}
