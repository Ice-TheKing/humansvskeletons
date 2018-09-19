using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public GameObject humanPrefab;
    public GameObject zombiePrefab;

    public float sphereRadius;

    public List<GameObject> obstaclesList = new List<GameObject>();
    public List<GameObject> humansList = new List<GameObject>();
    public List<GameObject> zombiesList = new List<GameObject>();
    public float zombieSpawnTimer;

    private float planeSize = 4.5f;
    private float heightOffGround = 0.05f;
    private bool renderLines = false;
    private float currentZombieTimer;

    private void Start()
    {
        currentZombieTimer = zombieSpawnTimer;
        // spawn two zombies
        for (int i = 0; i < 2; i++)
        {
            GameObject newZombie = SpawnObject(zombiePrefab, randomLocation());
            ZombieScript currentZombieScript = newZombie.GetComponent<ZombieScript>();
            currentZombieScript.obstaclesList = obstaclesList;
            currentZombieScript.sceneManagerScript = this;

            zombiesList.Add(newZombie);
        }
        

        // spawn a bunch of humans

        for (int i = 0; i < 16; i++)
        {
            GameObject newHuman = SpawnObject(humanPrefab, randomLocation());

            HumanScript currentHumanScript = newHuman.GetComponent<HumanScript>();
            currentHumanScript.obstaclesList = obstaclesList; // set the obstacles list
            currentHumanScript.sceneManagerScript = this;

            humansList.Add(newHuman);
        }
    }

    private void Update()
    {
        // update zombie targets
        UpdateZombies();
        UpdateHumans();

        if (Input.GetKeyDown(KeyCode.D))
        {
            // flip the render lines
            renderLines = !renderLines;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            // spawn a human
            SpawnHuman();
        }

        // spawn zombies if its time
        if(currentZombieTimer < 0)
        {
            SpawnZombie();
            currentZombieTimer = zombieSpawnTimer;
        }

        currentZombieTimer -= Time.deltaTime;
    }

    public GameObject SpawnObject(GameObject prefab, Vector3 position) // just to make spawning frick easier
    {
        GameObject newObject = Instantiate(prefab, position, Quaternion.identity);

        return newObject;
    }

    public Vector3 randomLocation()
    {
        Vector3 randomLocation = new Vector3(Random.Range(-planeSize, planeSize), heightOffGround, Random.Range(-planeSize, planeSize));
        return randomLocation;
    }

    public Vector3 randomFarLocation()
    {
        Vector3 randomLocation = new Vector3(Random.Range(-planeSize * 4, planeSize * 4), heightOffGround, Random.Range(-planeSize * 4, planeSize * 4));

        while (randomLocation.x < planeSize || randomLocation.z < planeSize)
        {
            // while the random location lies in the playing field, keep trying to get a new one
            randomLocation = new Vector3(Random.Range(-planeSize * 4, planeSize * 4), heightOffGround, Random.Range(-planeSize * 4, planeSize * 4));
        }

        return randomLocation;
    }

    public void UpdateZombies()
    {
        // update targets
        foreach (var item in zombiesList)
        {
            // get the script reference
            if(item == null)
            {
                return;
            }
            ZombieScript currentScript = item.GetComponent<ZombieScript>();

            // let them figure out what the closest human is from the list of humans
            currentScript.findClosestHuman(humansList);

            // figure out what the closest zombie is
            currentScript.findClosestZombie(zombiesList);


            // see if the zombie infects any humans
            currentScript.infectHuman(humansList);

            currentScript.renderLines = renderLines;
        }
    }

    public void UpdateHumans()
    {

        // update other human locations
        foreach (var item in humansList)
        {
            // get the script reference
            HumanScript currentScript = item.GetComponent<HumanScript>();

            // let them figure out what the closest human is from the list of humans
            currentScript.findClosestHuman(humansList);

            // let it figure out what the closest zombie is
            currentScript.findClosestZombie(zombiesList);

            currentScript.renderLines = renderLines;
        }
    }

    public void AddZombie(GameObject zombie)
    {
        // adds a zombie to the current list of active zombies
        zombiesList.Add(zombie);
    }

    public void RemoveHuman(GameObject human)
    {
        // remove a human to the current list of active humans
        humansList.Remove(human);
    }

    public void SpawnHuman()
    {
        GameObject newHuman = SpawnObject(humanPrefab, randomLocation());

        HumanScript currentHumanScript = newHuman.GetComponent<HumanScript>();
        currentHumanScript.obstaclesList = obstaclesList; // set the obstacles list
        currentHumanScript.sceneManagerScript = this;

        humansList.Add(newHuman);
    }

    public void Explosion(Vector3 explLocation, float explRadius)
    {
        // a method called by the grenade script that will test the location of the grenade against every human and zombie in the scene to see if they explode
        for (int i = 0; i < zombiesList.Count; i++)
        {
            if (zombiesList[i] == null)
                return;

            ZombieScript currentScript = zombiesList[i].GetComponent<ZombieScript>();

            // check radius
            float dist = Vector3.Magnitude(zombiesList[i].transform.position - explLocation);

            // if the distance is smaller than the radius it should explode, then kill the current zombie
            if (dist < explRadius)
            {
                currentScript.Die(explLocation);
            }
        }

        for (int i = 0; i < humansList.Count; i++)
        {
            if (humansList[i] == null)
                return;

            HumanScript currentScript = humansList[i].GetComponent<HumanScript>();

            // check radius
            float dist = Vector3.Magnitude(humansList[i].transform.position - explLocation);

            // if the distance is smaller than the radius it should explode, then kill the current zombie
            if (dist < explRadius)
            {
                currentScript.Die(explLocation);
            }
        }
    }

    public void SpawnZombie()
    {
        GameObject newZombie = SpawnObject(zombiePrefab, randomFarLocation());
        ZombieScript currentZombieScript = newZombie.GetComponent<ZombieScript>();
        currentZombieScript.obstaclesList = obstaclesList;
        currentZombieScript.sceneManagerScript = this;

        zombiesList.Add(newZombie);
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(Screen.width/2 -200, 0, 400, 30), "Press D to show lines");
        GUI.Box(new Rect(Screen.width/2 - 200, 30, 400, 30), "Press S to spawn more humans");
    }
}
