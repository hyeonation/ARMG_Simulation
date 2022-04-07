using System.Collections;
using System.Collections.Generic;
//using System;
using UnityEngine;
using S7.Net;

public class No_ALS_RMGC_Control : MonoBehaviour
{
    // Local command
    bool cmd_local_ctrl;

    // PLC
    Plc plc;
    string ip;
    short rack, slot;

    // Communication
    int DBnum_read, StartIdx_read, LenIdx_read;
    int DBnum_write, StartIdx_write, LenIdx_write;
    int startIdx;

    // TL, TR, H
    float tl_vel, d_t, del_pos, tr_vel, H_vel;
    Vector3 tl_pos, tr_pos, H_scale;
    GameObject trolley;

    // Twist lock
    int tw_lock;
    Twist_Lock_SPSS twist_lock;

    // LiDAR
    LiDAR_distance[] arr_LiDAR;
    byte[] arr_bytes, arr_bytes_temp;
    float[] arr_dist;
    string[] LiDAR_name;

    // Laser
    Laser_distance[] arr_Laser;
    string[] Laser_name;

    // pully
    GameObject[] arr_pully;
    string[] pully_name;
    HingeJoint hinge;
    GameObject spreader_up, spreader_down, spreader;
    Vector3 sp_pos;
    float sp_pos_y_imag, pully_diameter;

    // Container
    public GameObject Container_red, Container_green, Container_gray;
    GameObject[] arr_container;
    Vector3 pos_container_size, pos_container_init, pos_container_tmp, rotate_container_tmp;
    float interval_x, interval_z, err_rotate, err_shift;
    public int[,] arr_num_container;
    public float[] arr_pos_row, arr_pos_bay;
    int num_bay, num_row, num_container;
    int random_value;

    // sensor data
    float[] arr_sensor_float;
    byte[] arr_sensor_bytes;
    bool rope_slack;

    // RFID
    public GameObject RFID_Tag, TL_Calib_Target;
    GameObject[] arr_RFID_Tag, arr_TL_Calib_Target;
    GameObject RFID_SS;
    Vector3 pos_RFID_init, pos_RFID_tmp;
    float val_RFID_tag, range_recogn_RFID, dist_to_RFID_Tag;
    bool active_RFID;
    int idx_RFID;
    Laser_distance Laser_SS_Front, Laser_SS_Rear, Laser_LS_Front, Laser_LS_Rear;


    // Start is called before the first frame update
    // Setting objects and init values
    void Start()
    {
        Debug.Log("start");

        //////////////////////////////////////////////////// Define data
        // random seed
        Random.InitState(42);

        // local control
        cmd_local_ctrl = true;

        // plc preset
        ip = "192.168.0.212";
        rack = 0;
        slot = 1;

        // Comm data preset
        DBnum_read = 203;
        StartIdx_read = 0;
        LenIdx_read = 6;

        DBnum_write = 204;
        StartIdx_write = 0;
        LenIdx_write = 7200;

        // container variables
        interval_x = 0.5f;
        interval_z = 0.5f;
        err_rotate = 0.2f;
        err_shift = 0.2f;

        pos_container_size = Container_red.transform.localScale;
        pos_container_init = new Vector3(-12.075f, 1.2f, 0.0f);
        pos_container_init.y = pos_container_size.y / 2.0f;
        rotate_container_tmp = new Vector3(0, 0, 0);

        num_bay = 9;
        num_row = 9;

        arr_sensor_float = new float[10];
        arr_sensor_bytes = new byte[arr_sensor_float.Length * 4];

        // RFID
        pos_RFID_init = new Vector3(16.62f, 0, 0);
        range_recogn_RFID = 3.0f;   // [m], radius

        //////////////////////////////////////////////////// Preset

        // local control cmd
        if (cmd_local_ctrl == false)
        {
            // Connect PLC
            plc = new Plc(CpuType.S71500, ip, rack, slot);
            plc.Open();
        }

        // load trolley instance
        trolley = GameObject.Find("Trolley");

        // arr bytes
        arr_bytes = new byte[LenIdx_write];

        // LiDAR instance
        LiDAR_name = new string[]{"LiDAR_Front", "LiDAR_Rear", "LiDAR_Left", "LiDAR_Right"};
        arr_LiDAR = new LiDAR_distance[4];

        int i = 0;
        foreach(string name in LiDAR_name){

            arr_LiDAR[i] = GameObject.Find(name).GetComponent<LiDAR_distance>();
            i++;

        }

        // Laser instance
        Laser_name = new string[] {"Laser_TL_Front", "Laser_TR_Front_L", "Laser_TR_Rear_L", 
                                   "Laser_TL_Rear", "Laser_TR_Rear_R", "Laser_TR_Front_R"};
        arr_Laser = new Laser_distance[6];

        i = 0;
        foreach (string name in Laser_name)
        {
            arr_Laser[i] = GameObject.Find(name).GetComponent<Laser_distance>();
            i++;
        }

        //// Container
        // Make number
        num_container = 0;

        // Make container address
        arr_num_container = new int[num_bay, num_row];
        for(int j = 0; j < num_bay; j++){
            for (int k = 0; k < num_row; k++)
            {
                arr_num_container[j,k] = Random.Range(0, 7);
                num_container += arr_num_container[j, k];
            }
        }

        // Make container instance
        arr_container = new GameObject[num_container];
        for(int j = 0; j < num_container; j++){

            random_value = Random.Range(0, 3);

            // for making variable color containers
            if (random_value == 0) 
                arr_container[j] = Instantiate(Container_gray);
            
            else if (random_value == 1) 
                arr_container[j] = Instantiate(Container_red);
            
            else
                arr_container[j] = Instantiate(Container_green);

            // set parent
            arr_container[j].transform.SetParent(GameObject.Find("Container").transform);
        }

        // Make bay, row position
        arr_pos_bay = new float[num_bay + 1];
        arr_pos_row = new float[num_row + 1];
        for (int j = 0; j <= num_bay; j++)
        {
            arr_pos_bay[j] = pos_container_init.z - (pos_container_size.z / 2) + ((pos_container_size.z + interval_z) * j);
        }
        for (int k = 0; k <= num_row; k++)
        {
            arr_pos_row[k] = pos_container_init.x - (pos_container_size.x / 2) + ((pos_container_size.x + interval_x) * k);
        }

        // shift container position
        num_container = 0;
        for (int j = 0; j < num_bay; j++){
            for (int k = 0; k < num_row; k++){

                for (int l = 0; l < arr_num_container[j, k]; l++){
                    
                    // pos
                    pos_container_tmp.x = arr_pos_row[k] + (pos_container_size.x / 2);
                    pos_container_tmp.y = (pos_container_size.y * l) + (pos_container_size.y / 2.0f);
                    pos_container_tmp.z = arr_pos_bay[j] + (pos_container_size.z / 2);

                    // error
                    pos_container_tmp.x += Random.Range(-err_shift, err_shift);
                    pos_container_tmp.z += Random.Range(-err_shift, err_shift);
                    rotate_container_tmp.y = Random.Range(-err_rotate, err_rotate);

                    arr_container[num_container].transform.position = pos_container_tmp;
                    arr_container[num_container].transform.Rotate(rotate_container_tmp);
                    arr_container[num_container].SetActive(true);
                    arr_container[num_container].name = "Container_" + System.Convert.ToString(num_container + 1);

                    num_container++;
                }
            }
        }

        // pully instance
        arr_pully = new GameObject[4];
        pully_name = new string[] {"Main_Front_Left", "Main_Front_Right",  "Main_Rear_Left", "Main_Rear_Right"};

        i = 0;
        foreach (string name in pully_name)
        {
            arr_pully[i] = GameObject.Find(name);
            i++;
        }

        // spreader
        spreader_up = GameObject.Find("Spreader_up");
        spreader_down = GameObject.Find("Spreader_down");
        spreader = GameObject.Find("Spreader");

        Debug.Log(arr_pully[0].transform.position.y);
        Debug.Log(spreader_up.transform.position.y);
        Debug.Log(spreader_down.transform.position.y);

        twist_lock = spreader_down.GetComponent<Twist_Lock_SPSS>();

        pully_diameter = arr_pully[0].transform.localScale.x * 2.0f;

        //// RFID
        // Make instance
        arr_TL_Calib_Target = new GameObject[num_bay];
        arr_RFID_Tag = new GameObject[num_bay];
        for (int j = 0; j < num_bay; j++)
        {
            //// TL_Calib_Target
            // Make instance
            arr_TL_Calib_Target[j] = Instantiate(TL_Calib_Target);

            // set name
            arr_TL_Calib_Target[j].name = "TL_Calib_Target" + System.Convert.ToString(j+1);

            // set parent
            arr_TL_Calib_Target[j].transform.SetParent(GameObject.Find("TL_Calib_Target").transform);

            // set position
            pos_RFID_tmp = -pos_RFID_init;
            pos_RFID_tmp.z = arr_pos_bay[j] + (pos_container_size.z / 2);
            arr_TL_Calib_Target[j].transform.position = pos_RFID_tmp;

            //// RFID Tag
            // Make instance
            arr_RFID_Tag[j] = Instantiate(RFID_Tag);

            // set name
            arr_RFID_Tag[j].name = "RFID_Tag" + System.Convert.ToString(j + 1);

            // set parent
            arr_RFID_Tag[j].transform.SetParent(GameObject.Find("RFID_Tag").transform);

            // set position
            pos_RFID_tmp = 1*pos_RFID_init;
            pos_RFID_tmp.z = arr_pos_bay[j] + (pos_container_size.z / 2);
            arr_RFID_Tag[j].transform.position = pos_RFID_tmp;

            // sea side RFID object
            RFID_SS = GameObject.Find("RFID_Reader_SS");

            // RFID Laser
            Laser_SS_Front = GameObject.Find("RFID_Reader_SS").transform.Find("Laser_Front").gameObject.GetComponent<Laser_distance>();
            Laser_SS_Rear = GameObject.Find("RFID_Reader_SS").transform.Find("Laser_Rear").gameObject.GetComponent<Laser_distance>();
            Laser_LS_Front = GameObject.Find("RFID_Reader_LS").transform.Find("Laser_Front").gameObject.GetComponent<Laser_distance>();
            Laser_LS_Rear = GameObject.Find("RFID_Reader_LS").transform.Find("Laser_Rear").gameObject.GetComponent<Laser_distance>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        RFID();

        // manual control
        if (cmd_local_ctrl)
        {
            manual_ctrl();
        }

        // PLC control
        else
        {
            // LiDAR data to PLC
            Unity_to_PLC();

            // Read PLC data
            PLC_to_Unity();
        }

        // update position
        update_pos();

    }

    // Send data to PLC
    void Unity_to_PLC(){

        // LiDAR data
        int num_LiDAR = 0;
        int i = 0;
        foreach (LiDAR_distance LiDAR in arr_LiDAR)
        {
            arr_dist = LiDAR.arr_dist;

            i = 0;
            foreach (float dist in arr_dist)
            {

                arr_bytes_temp = System.BitConverter.GetBytes(dist);
                System.Array.Reverse(arr_bytes_temp);

                foreach (byte bb in arr_bytes_temp)
                {
                    arr_bytes[i] = bb;
                    i += 1;
                }
            }

            plc.WriteBytes(DataType.DataBlock, (DBnum_write + num_LiDAR), StartIdx_write, arr_bytes);
            num_LiDAR++;
        }

        // sensor
        arr_sensor_float[0] = tl_pos.z;     // Traveling sensor
        arr_sensor_float[1] = tr_pos.x;     // Trolley sensor
        arr_sensor_float[2] = arr_pully[0].transform.position.y - (spreader_up.transform.position.y + spreader_up.transform.localScale.y/2);    // hoist length

        // Laser distance
        for(int j=0; j<6; j++){
            arr_sensor_float[j+3] = arr_Laser[j].distance;
        }

        // convert byte array
        i = 0;
        foreach (float data in arr_sensor_float)
        {
            arr_bytes_temp = System.BitConverter.GetBytes(data);
            System.Array.Reverse(arr_bytes_temp);

            foreach (byte bb in arr_bytes_temp)
            {
                arr_sensor_bytes[i] = bb;
                i += 1;
            }
        }
        
        plc.WriteBytes(DataType.DataBlock, 250, 0, arr_sensor_bytes);

    }

    // Receive data from PLC
    // and update position based on data of PLC
    void PLC_to_Unity(){

        // Read data
        var data = plc.ReadBytes(DataType.DataBlock, DBnum_read, StartIdx_read, LenIdx_read);

        // to convert big indian
        System.Array.Reverse(data);

        //// refine data

        //// Traveling
        startIdx = 4;
        tl_vel = ((float)(System.BitConverter.ToInt16(data, startIdx)));

        //// Trolling
        startIdx = 2;
        tr_vel = ((float)(System.BitConverter.ToInt16(data, startIdx)));

        //// Hoist
        startIdx = 0;
        H_vel = ((float)(System.BitConverter.ToInt16(data, startIdx)));
        
    }

    void manual_ctrl()
    {

        // Hoist, Twist lock
        if (Input.GetKey(KeyCode.LeftShift))
        {
            // Hoist
            if (Input.GetKey(KeyCode.UpArrow))
            {
                H_vel = 150f;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                H_vel = -150f;
            }
            else
            {
                H_vel = 0f;
            }

            // Twist lock
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                tw_lock = -1;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                tw_lock = 1;
            }
            else
            {
                tw_lock = 0;
            }
        }

        // TL, TR
        else
        {
            // TL
            if (Input.GetKey(KeyCode.UpArrow))
            {
                tl_vel = 1f;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                tl_vel = -1f;
            }
            else
            {
                tl_vel = 0f;
            }

            // TR
            if (Input.GetKey(KeyCode.RightArrow))
            {
                tr_vel = 1f;
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                tr_vel = -1f;
            }
            else
            {
                tr_vel = 0f;
            }
        }
    }

    void update_pos()
    {
        // scan time
        d_t = Time.deltaTime;

        //// Traveling
        // shift position
        tl_pos = transform.position;
        del_pos = tl_vel * d_t;
        tl_pos.z += del_pos;
        transform.position = tl_pos;    // update drawing

        //// Trolley
        // shift position
        tr_pos = trolley.transform.position;
        del_pos = tr_vel * d_t;
        tr_pos.x += del_pos;
        trolley.transform.position = tr_pos;    // update drawing



        //// Hoist
        foreach (GameObject pully in arr_pully)
        {
            // velocity
            hinge = pully.GetComponent<HingeJoint>();
            var motor = hinge.motor;
            motor.targetVelocity = H_vel;
            hinge.motor = motor;
        }

        //// spreader position
        del_pos = (H_vel / 360) * (pully_diameter * (Mathf.PI)) * d_t;
        sp_pos = spreader.transform.position;

        // collision
        if (twist_lock.state_coll)
        {
            sp_pos_y_imag -= del_pos;
        }

        // no collision
        else
        {
            sp_pos_y_imag = sp_pos.y;
        }

        // imag 값이 real 값보다 같거나 클 때만 update
        // rope slack 동시 구현
        if (sp_pos.y <= sp_pos_y_imag)
        {
            sp_pos.y -= del_pos;
            spreader.transform.position = sp_pos;
            rope_slack = false;
        }
        else
        {
            rope_slack = true;
        }
        
        //// Twist lock
        twist_lock.tw_lock = tw_lock;
    }

    void RFID()
    {
        idx_RFID = twist_lock.out_idx(arr_pos_bay, tl_pos.z);
        if (idx_RFID != -1)
        {
            pos_RFID_tmp = GameObject.Find("RFID_Tag" + System.Convert.ToString(idx_RFID + 1)).transform.position;
            dist_to_RFID_Tag = (RFID_SS.transform.position - pos_RFID_tmp).magnitude;
            
            if (dist_to_RFID_Tag < range_recogn_RFID)
            {
                val_RFID_tag = pos_RFID_tmp.z;
                active_RFID = true;
            }

            else
            {
                active_RFID = false;
            }
        }

        else
        {
            active_RFID = false;
        }

    }

}
