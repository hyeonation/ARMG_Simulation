using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Twist_Lock_SPSS : MonoBehaviour
{
    public int tw_lock;
    int tw_lock_old;
    FixedJoint fixedJoint;
    public bool state_coll;

    No_ALS_RMGC_Control RMGC;
    GameObject container;
    Vector3 pos_container;
    int idx_bay, idx_row;
    bool lift_container;

    // Start is called before the first frame update
    void Start()
    {
        tw_lock_old = tw_lock;
        RMGC = GameObject.Find("RMGC").GetComponent<No_ALS_RMGC_Control>();

        lift_container = false;
    }

    // Update is called once per frame
    void Update()
    {
        // collision state
        if (state_coll)
        {
            // Trigger
            if ((tw_lock != tw_lock_old) && (tw_lock != 0))
            {
                //// Motion
                // Lock
                if (tw_lock == -1)
                {
                    Debug.Log("Lock");
                    container.transform.SetParent(transform);
                    lift_container = true;

                    /*
                    gameObject.AddComponent<FixedJoint>();
                    fixedJoint = GetComponent<FixedJoint>();
                    fixedJoint.connectedBody = container.GetComponent<Rigidbody>();
                    */
                }

                // Unlock
                else if (tw_lock == 1)
                {
                    Debug.Log("Unlock");
                    container.transform.SetParent(GameObject.Find("Container").transform);
                    lift_container = false;

                    //Destroy(fixedJoint);
                }

                //// update SPSS array
                // Load position
                pos_container = container.transform.position;

                // bay, row index
                idx_bay = out_idx(RMGC.arr_pos_bay, pos_container.z);
                idx_row = out_idx(RMGC.arr_pos_row, pos_container.x);

                // 범위 안에 있을 때만 SPSS update
                if ((idx_bay != -1) && (idx_row != -1))
                {
                    RMGC.arr_num_container[idx_bay, idx_row] += tw_lock;
                }
                else
                {
                    Debug.Log("No apply stacking");
                }

                if (idx_bay != -1)
                {
                    // 배열 출력
                    string confirm_arr = "Stack: ";
                    int count;

                    for (int i = 0; i < RMGC.arr_num_container.GetLength(1); i++)
                    {
                        count = RMGC.arr_num_container[idx_bay, i];
                        confirm_arr += count.ToString() + " ";
                    }

                    // Monitoring
                    Debug.Log(confirm_arr);
                }
            }
        }

        // update old value
        tw_lock_old = tw_lock;
    }

    private void OnTriggerEnter(Collider collision)
    {
        state_coll = true;
        
        // 들고 있는 컨테이너가 없을 때만
        if (lift_container == false)
        {
            container = collision.gameObject;
        }
        
        Debug.Log("Collision");
        Debug.Log(collision.gameObject.name);
    }

    private void OnTriggerExit(Collider collision)
    {
        state_coll = false;
        Debug.Log("No Collision");
        
    }

    public int out_idx(float[] arr_pos, float target)
    {
        int idx = -1;

        for(int i = 0; i < arr_pos.Length; i++)
        {
            if (target < arr_pos[i])
            {
                idx = i - 1;
                break;
            }
        }

        // 양쪽으로 벗어나는 경우는 -1 출력
        return idx;
    }
}
