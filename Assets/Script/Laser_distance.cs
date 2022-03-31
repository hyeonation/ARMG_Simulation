using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser_distance : MonoBehaviour
{
    RaycastHit hit;
    bool bool_hit;

    float maxDistance;
    Vector3 point_src, dir;
    public float distance;

    // Start is called before the first frame update
    void Start()
    {
        // set parameters
        maxDistance = 12.0f;
    }

    // Update is called once per frame
    void Update()
    {

        // determine standard vector
        point_src = transform.position;
        dir = - transform.forward;      // z axis reverse

        // emit laser
        bool bool_hit = Physics.Raycast(point_src, dir, out hit, maxDistance);

        // save distance
        // [m]
        distance = hit.distance;
        //Debug.Log(distance);

        
        if (bool_hit)
        {
            Debug.DrawRay(point_src, dir * hit.distance, Color.blue, 0.1f);
        }
        else
        {
            Debug.DrawRay(point_src, dir * maxDistance, Color.red, 0.1f);
        }
        
    }
}
