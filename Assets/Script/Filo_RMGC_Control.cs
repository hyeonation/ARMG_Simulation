using System.Collections;
using System.Collections.Generic;
//using System;
using UnityEngine;
using S7.Net;

public class Filo_RMGC_Control : MonoBehaviour
{
    // PLC
    Plc plc;
    string ip;
    short rack, slot;

    // TL, TR, H
    float tl_vel, d_t, del_pos, tr_vel, H_vel;
    int DBnum_read, StartIdx_read, LenIdx_read;
    int DBnum_write, StartIdx_write, LenIdx_write;
    int startIdx;
    Vector3 tl_pos, tr_pos, H_scale;
    GameObject trolley;

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
    GameObject spreader_up, spreader_down;

    // Container
    public GameObject Container_red, Container_green, Container_gray;
    GameObject[] arr_container;
    Vector3 pos_container_size, pos_container_init, pos_container_tmp, rotate_container_tmp;
    float interval_x, interval_z, err_rotate, err_shift;
    int[,] arr_num_container;
    int num_row, num_column, num_container;
    int random_value;

    // sensor data
    float[] arr_sensor_float;
    byte[] arr_sensor_bytes;

    // Start is called before the first frame update
    // Setting objects and init values
    void Start()
    {
        Debug.Log("start");

        //////////////////////////////////////////////////// Define data
        // random seed
        Random.InitState(42);

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

        num_row = 9;
        num_column = 9;

        arr_sensor_float = new float[10];
        arr_sensor_bytes = new byte[arr_sensor_float.Length * 4];

        //////////////////////////////////////////////////// Preset
        
        // Connect PLC
        //plc = new Plc(CpuType.S71500, ip, rack, slot);
        //plc.Open();

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

        // Make number
        num_container = 0;

        // Make container address
        arr_num_container = new int[num_row, num_column];
        for(int j = 0; j < num_row; j++){
            for (int k = 0; k < num_column; k++)
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
        }

        // shift container position
        num_container = 0;
        for (int j = 0; j < num_row; j++){
            for (int k = 0; k < num_column; k++){

                for (int l = 0; l < arr_num_container[j, k]; l++){
                    
                    // pos
                    pos_container_tmp.x = (pos_container_size.x + interval_x) * k;
                    pos_container_tmp.y = pos_container_size.y * l;
                    pos_container_tmp.z = (pos_container_size.z + interval_z) * j;
                    pos_container_tmp += pos_container_init;

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

        Debug.Log(arr_pully[0].transform.position.y);
        Debug.Log(spreader_up.transform.position.y);
        Debug.Log(spreader_down.transform.position.y);
        
    }

    // Update is called once per frame
    void Update()
    {
        // LiDAR data to PLC
        //Unity_to_PLC();

        // Read PLC data
        //PLC_to_Unity();

        //Debug.Log(Time.deltaTime); 
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

        // scan time
        d_t = Time.deltaTime;

        //// Traveling
        startIdx = 4;
        tl_vel = ((float)(System.BitConverter.ToInt16(data, startIdx)));

        // shift position
        tl_pos = transform.position;
        del_pos = tl_vel * d_t;
        tl_pos.z += del_pos;

        // update drawing
        transform.position = tl_pos;

        //// Trolling
        startIdx = 2;
        tr_vel = ((float)(System.BitConverter.ToInt16(data, startIdx)));

        // shift position
        tr_pos = trolley.transform.position;
        del_pos = tr_vel * d_t;
        tr_pos.x += del_pos;

        // update drawing
        trolley.transform.position = tr_pos;

        //// Hoist
        startIdx = 0;
        H_vel = ((float)(System.BitConverter.ToInt16(data, startIdx)));
        
        foreach (GameObject pully in arr_pully)
        {
            // velocity
            hinge = pully.GetComponent<HingeJoint>();
            var motor = hinge.motor;
            motor.targetVelocity = H_vel;
            hinge.motor = motor;

        }
        
    }
}
