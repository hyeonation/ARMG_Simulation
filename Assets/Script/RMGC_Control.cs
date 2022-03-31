using System.Collections;
using System.Collections.Generic;
//using System;
using UnityEngine;
using S7.Net;

public class RMGC_Control : MonoBehaviour
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

    // Container
    public GameObject Container_red, Container_green, Container_gray;
    GameObject[] arr_container;
    Vector3 pos_container_size, pos_container_init, pos_container_tmp, rotate_container_tmp;
    float interval_x, interval_z, err_rotate, err_shift;
    int[,] arr_num_container;
    int num_row, num_column, num_container;
    int random_value;

    // wire
    GameObject[] arr_wire;
    string[] wire_name;
    Vector3 scale_wire;

    // sensor data
    float[] arr_sensor_float;
    byte[] arr_sensor_bytes;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("start");

        //////////////////////////////////////////////////// Define data
        // random seed
        Random.InitState(42);

        // plc preset
        ip = "192.168.1.2";
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

        //////////////////////////////////////////////////// Preset
        arr_sensor_float = new float[4];
        arr_sensor_bytes = new byte[arr_sensor_float.Length * 4];

        // Connect PLC
        plc = new Plc(CpuType.S71500, ip, rack, slot);
        plc.Open();

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
        
        // wire instance
        arr_wire = new GameObject[4];
        wire_name = new string[]{"Wire_Left_Front", "Wire_Left_Rear", "Wire_Right_Front", "Wire_Right_Rear"};

        i = 0;
        foreach(string name in wire_name){
            arr_wire[i] = GameObject.Find(name);
            print(arr_wire[i]);
            i++;

        }
        
    }

    // Update is called once per frame
    void Update()
    {
        // LiDAR data to PLC
        Unity_to_PLC();

        // Read PLC data
        PLC_to_Unity();

        //Debug.Log(Time.deltaTime); 
    }


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
        arr_sensor_float[2] = arr_wire[0].transform.localScale.y * 2.0f;       // Hoist sensor

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
        del_pos = tl_vel * d_t / 16384;
        tl_pos.z += del_pos;

        // update drawing
        transform.position = tl_pos;

        //// Trolling
        startIdx = 2;
        tr_vel = ((float)(System.BitConverter.ToInt16(data, startIdx)));

        // shift position
        tr_pos = trolley.transform.position;
        del_pos = tr_vel * d_t / 16384;
        tr_pos.x += del_pos;

        // update drawing
        trolley.transform.position = tr_pos;

        //// Hoist
        startIdx = 0;
        H_vel = ((float)(System.BitConverter.ToInt16(data, startIdx)));
        del_pos = H_vel * d_t / 16384 / 2;

        foreach (GameObject wire in arr_wire)
        {
            // shift position
            H_scale = wire.transform.localScale;
            H_scale.y += del_pos;

            // update drawing
            wire.transform.localScale = H_scale;

            // This is necessary to increase the wire length
            // and it must deactive "Aute configure connected anchor"
            wire.GetComponents<CharacterJoint>()[0].anchor = wire.GetComponents<CharacterJoint>()[0].anchor;
            wire.GetComponents<CharacterJoint>()[1].anchor = wire.GetComponents<CharacterJoint>()[1].anchor;
        }
    }
}
