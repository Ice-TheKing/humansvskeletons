using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeScript : MonoBehaviour {

    public float fuseTime;
    public GameObject explosionPrefab;
    public float explRadius;

    public SceneManager sceneManagerScript;

    private void Update()
    {
        if(fuseTime < 0)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
            // call the scene manager explosion method
            sceneManagerScript.Explosion(transform.position, explRadius);
            return;
        }
        fuseTime -= Time.deltaTime;
    }
}
