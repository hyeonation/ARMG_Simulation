using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sticky : MonoBehaviour
{
    FixedJoint fixedJoint;
    Rigidbody none;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)){
            
            Debug.Log("Sticky");
            gameObject.AddComponent<FixedJoint>();
            fixedJoint = GetComponent<FixedJoint>();
            fixedJoint.connectedBody = GameObject.Find("Weight").GetComponent<Rigidbody>();

        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log("Destroy");
            Destroy(fixedJoint);

        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision");
        gameObject.AddComponent<FixedJoint>();
        fixedJoint = GetComponent<FixedJoint>();
        fixedJoint.connectedBody = collision.gameObject.GetComponent<Rigidbody>();
    }

    /*
    private void OnCollisionStay(Collision collision)
    {
        Debug.Log("Collision Stay");
        gameObject.AddComponent<FixedJoint>();
        fixedJoint = GetComponent<FixedJoint>();
        fixedJoint.connectedBody = collision.gameObject.GetComponent<Rigidbody>();
    }
    */

}
