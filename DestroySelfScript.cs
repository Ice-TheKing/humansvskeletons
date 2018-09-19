using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelfScript : MonoBehaviour {

    public float destroyDelay;

    private void Update()
    {
        destroyDelay -= Time.deltaTime;

        if(destroyDelay < 0)
        {
            Destroy(gameObject);
        }
    }
}
