using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class shift_particle : MonoBehaviour
{
    Vector3 vec_force;
    //int i = 0;
    Rigidbody particle;

    // Start is called before the first frame update
    void Start()
    {
        vec_force = new Vector3(0, -1e-5f, 0);
        particle = GetComponent<Rigidbody>();
        particle.AddForce(vec_force);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
        Destroy(particle);
        
    }
}
