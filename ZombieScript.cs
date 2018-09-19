//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieScript : Vehicle
{
    public float seekingWeight;
    public float obstacleWeight;
    public float infectRadius;
    public float avoidMapEdgeWeight;
    public GameObject closestZombie;
    public GameObject target;

    public GameObject ragdollPrefab;
    public GameObject skateboardPrefab;

    private float wanderPointAngle = 90f;
    public SceneManager sceneManagerScript;

    public override void CalcSteerForces()
    {
        Vector3 ultimateForce = Vector3.zero;

        // pursue the target if it exists, otherwise wander
        if(target != null)
        {
            ultimateForce += Pursue(target);
        }
        else
        {
            ultimateForce += Wander();
            // outside bounds force

            Vector3 outofBoundsForce = CheckBounds();
            outofBoundsForce *= avoidMapEdgeWeight;
            ultimateForce += outofBoundsForce;
        }
            
        ultimateForce *= seekingWeight;

        // obstacles
        GameObject closestObstacle = null;
        float shortestDistance = 999;
        // find the closest obstacle
        foreach (var item in obstaclesList)
        {
            // find the distance between the two
            Vector3 vecDist = item.transform.position - transform.position;

            if (vecDist.magnitude < shortestDistance)
            {
                shortestDistance = vecDist.magnitude;
                closestObstacle = item;
            }
        }
        Vector3 avoidForce = ObstacleAvoidance(closestObstacle);
        avoidForce *= obstacleWeight;
        ultimateForce += avoidForce;

        // avoid the nearest zombie
        Vector3 zombieAvoidForce = ObstacleAvoidance(closestZombie);
        zombieAvoidForce *= obstacleWeight;
        ultimateForce += zombieAvoidForce;

        ultimateForce.Normalize();
        ultimateForce *= maxForce;
        ApplyForce(ultimateForce);
    }

    public void infectHuman(List<GameObject> humans)
    {
        // if there are no humans, just stop
        if(humans.Count == 0)
        {
            return;
        }

        foreach (var item in humans)
        {
            // compare radii
            float distance = Vector3.Magnitude(transform.position - item.transform.position);

            if(distance < infectRadius)
            {
                // get a reference to the human script
                HumanScript currentHumanScript = item.GetComponent<HumanScript>();

                currentHumanScript.Infect();
            }
            
        }
    }

    public void findClosestHuman(List<GameObject> humans)
    {
        if(humans == null || humans.Count == 0)
        {
            // if there are no humans, make target null
            target = null;
        }

        float currentDistance;
        float nearestDistance = -1;

        foreach (var item in humans)
        {
            if(item == null)
            {
                return;
            }
            // find the distance
            currentDistance = Vector3.Magnitude(item.transform.position - transform.position);

            if(currentDistance < nearestDistance || nearestDistance == -1) // if nearest distance is -1 it has not been set yet
            {
                nearestDistance = currentDistance; // our nearest distance is whatever object we just went through
                target = item; // so set it as our target
            }
        }

        // draw line to closest human
        if(target != null)
            Debug.DrawLine(transform.position, target.transform.position, Color.black);
    }

    public void findClosestZombie(List<GameObject> zombies)
    {
        if(zombies.Count == 0 || zombies == null)
        {
            return;
        }

        float currentDistance;
        float nearestDistance = -1;

        foreach (var item in zombies)
        {
            if(item == null)
            {
                return;
            }

            // find the distance
            currentDistance = Vector3.Magnitude(item.transform.position - transform.position);

            if (currentDistance < nearestDistance && currentDistance != 0 || nearestDistance == -1) // if nearest distance is -1 it has not been set yet. If distance is 0, we are currently at the current human sooo we don't wanna get a closest human as itself
            {
                nearestDistance = currentDistance; // our nearest distance is whatever object we just went through
                closestZombie = item; // so set it as our target
            }
        }
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

    public Vector3 CheckBounds()
    {
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
        if (transform.position.z > planeBounds || transform.position.z < -planeBounds)
        {
            Vector3 distToCenter = transform.position - Vector3.zero;
            // make the y component of the vector (and x component) 0
            distToCenter = new Vector3(0, 0, -distToCenter.z);
            distToCenter.Normalize();


            return distToCenter;
        }

        return Vector3.zero;
    }

    private void OnRenderObject()
    {
        base.OnRenderObject();

        if(renderLines)
        {
            if(target != null)
            {
                blackMat.SetPass(0);

                GL.Begin(GL.LINES);
                GL.Vertex(transform.position);
                GL.Vertex(target.transform.position);
                GL.End();
            }
            
            // draw future position
            redMat.SetPass(0);

            GL.Begin(GL.LINES);
            GL.Vertex(transform.position + transform.forward * 0.5f);
            GL.Vertex(transform.position + transform.forward * 0.65f);
            GL.End();
        }
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

        GameObject newRagdoll = Instantiate(ragdollPrefab, transform.position + new Vector3(0f,0.5f,0), transform.rotation); // add to the y location so the ragdoll doesn't get stuck in the skateboard
        GameObject newSkateboard = Instantiate(skateboardPrefab, new Vector3(transform.position.x, 0.5f, transform.position.z), transform.rotation);

        // find the rigidbody components
        Rigidbody skateboardRigidBody = newSkateboard.GetComponent<Rigidbody>();

        // get the pelvis rigidbody
        Transform pelvis = newRagdoll.transform.GetChild(0).GetChild(0);
        Rigidbody pelvisRigidbody = pelvis.GetComponent<Rigidbody>();

        // add the forces
        // skateboardRigidBody.angularVelocity = new Vector3(Random.Range(0, skateboardAngularVelo / distanceToExplosion), Random.Range(0, skateboardAngularVelo / distanceToExplosion), Random.Range(0, skateboardAngularVelo / distanceToExplosion)); // give it a random angular velocity depending on how far it was from the explosion


        skateboardRigidBody.AddForce((directionToRagdoll * skateboardForce) / distanceToExplosion);

        pelvisRigidbody.AddForce((directionToRagdoll * ragdollForce) / distanceToExplosion);

        // finally remove yourself from the list and then destroy yourself
        sceneManagerScript.zombiesList.Remove(gameObject);
        Destroy(gameObject);
    }
}
