using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiDAR_distance : MonoBehaviour
{
    RaycastHit hit;
    bool bool_hit;

    float maxDistance, theta, resolution;
    int max_angle, min_angle, arr_length;
    Vector3 point_src, dir, dir_std, dir_rotate;
    public float[] arr_dist;

    // Start is called before the first frame update
    void Start()
    {
        // set parameters
        maxDistance = 30f;
        resolution = 0.1f;
        max_angle = 90;
        min_angle = -90;

        // Calculation
        arr_length = (int)((float)(max_angle - min_angle) / resolution);
        arr_dist = new float[arr_length];
    }

    // Update is called once per frame
    void Update()
    {

        // determine standard vector
        point_src = transform.position;
        dir_std = transform.right;
        dir_rotate = transform.up;

        for (int i = 0; i < arr_length; i++)
        {
            // rotate angle
            // rotate to CCW
            theta = 90 - (i * 0.1f);

            // rotate laser direction
            dir = Quaternion.AngleAxis(theta, dir_rotate) * dir_std;

            // emit laser
            bool bool_hit = Physics.Raycast(point_src, dir, out hit, maxDistance);

            /*
            if (bool_hit)
            {
                Debug.DrawRay(point_src, dir * hit.distance, Color.blue, 1f);
            }
            else
            {
                Debug.DrawRay(point_src, dir * maxDistance, Color.red, 1f);
            }
            */
            
            // save distance
            arr_dist[i] = hit.distance * 1000;
        }
        
    }
}
