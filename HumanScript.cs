//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanScript : Vehicle
{
    public float seekWeight;
    public float fleeWeight;
    public float obstacleWeight;
    public float avoidMapEdgeWeight;
    public float wanderRadius;
    public float fleeRadius;
    public float secondsBetweenGrenadeThrows;
    public float throwForce;
    private float timeToThrowGrenade;
    public GameObject fleeTarget;

    public GameObject nearestHuman;
    public GameObject zombiePrefab;
    public GameObject grenadePrefab;
    public SceneManager sceneManagerScript;

    public GameObject ragdollPrefab;
    public GameObject skateboardPrefab;

    private void Awake()
    {
        timeToThrowGrenade = Random.Range(2, 6);
    }

    private float wanderPointAngle = 90;

    public override void CalcSteerForces()
    {
        Vector3 ultimateForce = Vector3.zero;

        // ultimateForce += Seek(seekTarget.transform.position);
        ultimateForce += Wander();
        ultimateForce *= seekWeight;
        
        if(fleeTarget != null)
        {
            Vector3 fleeingForces = Flee(fleeTarget.transform.position);

            // if too close add flee force
            if (Vector3.Magnitude(fleeTarget.transform.position - transform.position) < fleeRadius)
            {
                fleeingForces *= fleeWeight;
                ultimateForce += fleeingForces;

                // throw a grenade at the near target if we can
                if(timeToThrowGrenade < 0)
                {
                    // if the time is less than zero, we are ready to throw again. Throw the grenade far in front of where the zombie is going
                    Vector3 vectorToThrow = (fleeTarget.transform.position + (fleeTarget.transform.forward*2)) - transform.position;

                    // scale it back a bit
                    vectorToThrow *= throwForce;

                    // set the y value of the throw
                    vectorToThrow = new Vector3(vectorToThrow.x, 0.4f, vectorToThrow.z); // don't wanna throw in the Y

                    // instantiate a grenade at your position in the air
                    GameObject newGrenade = Instantiate(grenadePrefab, new Vector3(transform.position.x, 0.6f, transform.position.z), Quaternion.identity);
                    Rigidbody grenadeRigidBody = newGrenade.GetComponent<Rigidbody>();
                    grenadeRigidBody.AddForce(vectorToThrow);

                    // add the scene manager script reference to the grenade script
                    GrenadeScript grenadeScript = newGrenade.GetComponent<GrenadeScript>();
                    grenadeScript.sceneManagerScript = sceneManagerScript;

                    timeToThrowGrenade = secondsBetweenGrenadeThrows + Random.Range(-4, 4);
                }
            }
        }

        GameObject closestObstacle = null;
        float shortestDistance = 999;
        // find the closest obstacle
        foreach (var item in obstaclesList)
        {
            // find the distance between the two
            Vector3 vecDist = item.transform.position - transform.position;

            if(vecDist.magnitude < shortestDistance)
            {
                shortestDistance = vecDist.magnitude;
                closestObstacle = item;
            }
        }
        Vector3 avoidForce = ObstacleAvoidance(closestObstacle);
        avoidForce *= obstacleWeight;
        ultimateForce += avoidForce;

        Vector3 avoidNearestHuman = ObstacleAvoidance(nearestHuman);
        avoidNearestHuman *= obstacleWeight;
        ultimateForce += avoidNearestHuman;

        // check to see if out of bounds
        Vector3 outofBoundsForce = CheckBounds();
        outofBoundsForce *= avoidMapEdgeWeight;
        ultimateForce += outofBoundsForce;

        ultimateForce.Normalize();
        ultimateForce *= maxForce;
        ApplyForce(ultimateForce);
    }

    public Vector3 Wander()
    {
        float randomDegreeChange = Random.Range(-5f, 5f);
        // update the wander point angle
        wanderPointAngle += randomDegreeChange * Time.deltaTime;

        // project a point in front of the object
        Vector3 circleCenter = transform.position + transform.forward;

        // extended vector is the rotating vector centered at the circle's center, and rotates around
        Vector3 extendedVector = circleCenter + (transform.forward * 0.3f);

        // rotate the extended vector by the wandering point angle
        Vector3 extendedWithDirection = Quaternion.Euler(0f, wanderPointAngle, 0f) * extendedVector;
        
        // now seek the extended vector
        // Debug.DrawLine(circleCenter, extendedWithDirection, Color.magenta);

        Vector3 seekLocation = Seek(extendedWithDirection);

        return seekLocation;
    }

    public void findClosestHuman(List<GameObject> humans)
    {
        float currentDistance;
        float nearestDistance = -1;

        foreach (var item in humans)
        {
            // find the distance
            currentDistance = Vector3.Magnitude(item.transform.position - transform.position);

            if (currentDistance < nearestDistance && currentDistance != 0 || nearestDistance == -1) // if nearest distance is -1 it has not been set yet. If distance is 0, we are currently at the current human sooo we don't wanna get a closest human as itself
            {
                nearestDistance = currentDistance; // our nearest distance is whatever object we just went through
                nearestHuman = item; // so set it as our target
            }
        }
    }

    public void findClosestZombie(List<GameObject> zombies)
    {
        float currentDistance;
        float nearestDistance = -1;

        foreach (var item in zombies)
        {
            if(item == null)
            {
                return;
            }
            currentDistance = Vector3.Magnitude(item.transform.position - transform.position);

            if (currentDistance < nearestDistance && currentDistance != 0 || nearestDistance == -1) // if nearest distance is -1 it has not been set yet. If distance is 0, we are currently at the current human sooo we don't wanna get a closest human as itself
            {
                nearestDistance = currentDistance; // our nearest distance is whatever object we just went through
                fleeTarget = item; // so set it as our target
            }
        }
    }

    public void Infect()
    {
        // instantiate a zombie here
        GameObject newZombie = Instantiate(zombiePrefab, transform.position, Quaternion.identity);
        ZombieScript currentZombieScript = newZombie.GetComponent<ZombieScript>();
        currentZombieScript.obstaclesList = obstaclesList; // set the obstacles list
        currentZombieScript.sceneManagerScript = sceneManagerScript;

        sceneManagerScript.AddZombie(newZombie);
        sceneManagerScript.RemoveHuman(gameObject);

        // finally destroy itself
        Destroy(gameObject);
    }

    public Vector3 CheckBounds()
    {
        // incriment grenade timer
        timeToThrowGrenade -= Time.deltaTime;

        // check to see if the human is about to move out of bounds

        // x bounds
        if (transform.position.x > planeBounds || transform.position.x < -planeBounds)
        {
            Vector3 distToCenter = transform.position - Vector3.zero;
            // make the y component of the vector (and z component) 0
            distToCenter = new Vector3(-distToCenter.x, 0, 0);
            distToCenter.Normalize();

            return distToCenter;
        }

        // y bounds
        if(transform.position.z > planeBounds || transform.position.z < -planeBounds)
        {
            Vector3 distToCenter = transform.position - Vector3.zero;
            // make the y component of the vector (and x component) 0
            distToCenter = new Vector3(0, 0, -distToCenter.z);
            distToCenter.Normalize();


            return distToCenter;
        }

        return Vector3.zero;
    }

    public void Die(Vector3 location)
    {
        // find a vector from the location to here, so we can add that force
        Vector3 directionToRagdoll = transform.position - location;
        float distanceToExplosion = directionToRagdoll.magnitude;

        // "clamp" the distance to explosion float so that it will never have an infinite force, and an upper limit cause small ragdoll forces are boring
        distanceToExplosion = Mathf.Min(distanceToExplosion, 0.2f);
        distanceToExplosion = Mathf.Max(distanceToExplosion, 0.6f);

        directionToRagdoll += new Vector3(0f, ragdollUpwardForce / distanceToExplosion, 0f);  // also add the vector y = 1 so they fly upward when they explode

        GameObject newRagdoll = Instantiate(ragdollPrefab, transform.position + new Vector3(0f, 0.5f, 0), transform.rotation); // add to the y location so the ragdoll doesn't get stuck in the skateboard
        GameObject newSkateboard = Instantiate(skateboardPrefab, new Vector3(transform.position.x, 0.5f, transform.position.z), transform.rotation);

        // find the rigidbody components
        Rigidbody skateboardRigidBody = newSkateboard.GetComponent<Rigidbody>();

        // get the pelvis rigidbody
        Transform pelvis = newRagdoll.transform.GetChild(1);
        Rigidbody pelvisRigidbody = pelvis.GetComponent<Rigidbody>();

        // add the forces
        // skateboardRigidBody.angularVelocity = new Vector3(Random.Range(0, skateboardAngularVelo / distanceToExplosion), Random.Range(0, skateboardAngularVelo / distanceToExplosion), Random.Range(0, skateboardAngularVelo / distanceToExplosion)); // give it a random angular velocity depending on how far it was from the explosion


        skateboardRigidBody.AddForce((directionToRagdoll * skateboardForce) * 0.2f / distanceToExplosion);

        pelvisRigidbody.AddForce((directionToRagdoll * ragdollForce) / distanceToExplosion);

        // finally remove yourself from the list and then destroy yourself
        sceneManagerScript.humansList.Remove(gameObject);
        Destroy(gameObject);
    }

    private void OnRenderObject()
    {
        if (renderLines)
        {
            base.OnRenderObject();

            purpleMat.SetPass(0);

            GL.Begin(GL.LINES);
            GL.Vertex(transform.position + transform.forward * 0.8f);
            GL.Vertex(transform.position + transform.forward * 0.85f);
            GL.End();
        }
    }
}
