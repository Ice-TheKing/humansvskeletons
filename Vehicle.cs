using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Vehicle : MonoBehaviour
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 acceleration;
    public Vector3 direction;

    public Material blueMat;
    public Material greenMat;
    public Material blackMat;
    public Material redMat;
    public Material purpleMat;

    public List<GameObject> obstaclesList; // obstacles list is set by the scene manager on prefab instantiation


    public float maxForce;
    public float mass;
    public float radius;
    public float maxSpeed;
    public float obstacleRadius = 0.6f;
    public float vectorLineMult = 0.5f;
    public float ragdollForce;
    public float skateboardForce;
    public float skateboardAngularVelo;
    public float ragdollUpwardForce;

    public float planeSize = 4.2f;

    public abstract void CalcSteerForces();

    public float planeBounds = 8.7f;

    public bool renderLines = true;

    virtual public void Start()
    {

    }

    virtual public void Update()
    {
        CalcSteerForces();

        UpdatePosition();

        SetTransform();
        
        // ApplyFriction(0.9f);
    }

    // Apply Force applies an incoming force to the vehicles acceleration
    public void ApplyForce(Vector3 force)
    {
        acceleration += force / mass;
    }

    public void ApplyFriction(float coefficient)
    {
        Vector3 friction = -velocity;
        friction.Normalize();
        friction *= coefficient;
        acceleration += friction;
    }

    public void UpdatePosition()
    {
        position = transform.position;

        // add acceleration to velocity
        velocity += acceleration * Time.deltaTime;

        // set velocity to maxSpeed
        velocity.Normalize();
        velocity *= maxSpeed;

        // add velocity to position
        position += velocity * Time.deltaTime;

        // derrive direction from velocity
        direction = velocity.normalized;

        // Start fresh each frame
        acceleration = Vector3.zero;

        transform.position = position;
    }

    public void SetTransform()
    {
        //Quaternion rotation = Quaternion.Euler(0f, direction.y, 0f); // only face in the y
        //transform.rotation = rotation;
        transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
    }

    public Vector3 Seek(Vector3 targetPosition)
    {
        Vector3 desiredVelocity = targetPosition - transform.position;
        // scale to max speed
        desiredVelocity.Normalize();
        // add max speed
        desiredVelocity *= maxSpeed;

        Vector3 steeringForce = desiredVelocity - velocity;

        // draw forward debug line
        Debug.DrawLine(transform.position, transform.position + (velocity.normalized * vectorLineMult), Color.green);
        // draw right debug line
        Debug.DrawLine(transform.position, transform.position + transform.right * vectorLineMult * 0.5f, Color.blue); // half the size as the forward one

        return steeringForce;
    }

    public Vector3 Pursue(GameObject target)
    {
        // get a vector called target future position from myself, to where the target gameobject will be. vectorLineMult = 0.5f
        Vector3 targetFuturePos = target.transform.position + (target.transform.forward * vectorLineMult); // forward position

        Debug.DrawLine(transform.position, target.transform.position + (target.transform.forward * vectorLineMult));

        Vector3 desiredVelocity = targetFuturePos - transform.position;

        // scale to max speed
        desiredVelocity.Normalize();
        // add max speed
        desiredVelocity *= maxSpeed;

        Vector3 steeringForce = desiredVelocity - velocity;

        // draw forward debug line
        Debug.DrawLine(transform.position, transform.position + (velocity.normalized * vectorLineMult), Color.green);
        // draw right debug line
        Debug.DrawLine(transform.position, transform.position + transform.right * vectorLineMult * 0.5f, Color.blue); // half the size as the forward one

        return steeringForce;
    }

    public Vector3 Flee(Vector3 targetPosition)
    {
        Vector3 desiredVelocty = transform.position - targetPosition;

        desiredVelocty.Normalize();
        desiredVelocty *= maxSpeed;

        Vector3 steerForce = desiredVelocty - velocity;

        return steerForce;
    }

    public Vector3 ObstacleAvoidance(GameObject obstacle)
    {
        if(obstacle == null)
        {
            return Vector3.zero;
        }
        Vector3 vectorToObject = obstacle.transform.position - transform.position;

        if (obstacleRadius > Vector3.Magnitude(vectorToObject)) // check the magnitude between the two. If the distance between them is less than it should be, execute the rest
        {
            // draw a red line between the two
            Debug.DrawLine(transform.position, obstacle.transform.position, Color.red);

            // is the object in front or in back?
            float forwardBackDot = Vector3.Dot(vectorToObject, transform.forward);

            if(forwardBackDot > 0) // if the dot product is greater than zero, the object is in front so we have to worry about it
            {
                float rightLeftDot = Vector3.Dot(vectorToObject, transform.right);

                if(rightLeftDot < radius) // the obstacle is in the humans radius
                {
                    if(rightLeftDot > 0)
                    {
                        // positive = right
                        Vector3 avoidVelocity = Vector3.right * maxSpeed;
                        Vector3 steerForce = avoidVelocity - velocity;
                        return steerForce;
                    }
                    else
                    {
                        // negative = left
                        Vector3 avoidVelocity = -Vector3.right * maxSpeed;
                        Vector3 steerForce = avoidVelocity - velocity;
                        return steerForce;
                    }
                }
            }
        }

        return Vector3.zero; // if none of those things are the case we just return zero
    }

    public void OnRenderObject()
    {
        if(renderLines)
        {
            // green right vector
            greenMat.SetPass(0);

            GL.Begin(GL.LINES);
            GL.Vertex(transform.position);
            GL.Vertex(transform.position + transform.right * 0.4f);
            GL.End();

            // blue forward vector
            blueMat.SetPass(0);

            GL.Begin(GL.LINES);
            GL.Vertex(transform.position);
            GL.Vertex(transform.position + transform.forward * 0.6f);
            GL.End();

            
            // future position

        }
    }
}
