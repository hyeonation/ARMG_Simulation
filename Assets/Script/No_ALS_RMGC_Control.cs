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
    int DBnum_write, LenIdx_write;
    int startIdx;
    int num_bits_write;
    BitArray bits_write;
    byte[] bytes_write;
    int len_int = 2;
    float conv_unit_m = 0.1f;
    float conv_unit_mm = 1000f;

    // PLC -> Unity
    bool PtU_SPSS;
    int PtU_TL_SS_vel, PtU_TL_LS_vel, PtU_TR_vel, PtU_H_vel, PtU_tw_lock;
    int PtU_MM_pos_bay, PtU_MM_pos_row, PtU_MM_pos_CW;

    // Unity -> PLC
    bool UtP_tw_lock, UtP_SPSS, UtP_rope_slack;
    int UtP_TL_SS_pos, UtP_TL_LS_pos, UtP_TR_pos, UtP_H_pos;
    int UtP_sp_laser_1, UtP_sp_laser_2, UtP_sp_laser_3, UtP_sp_laser_4, UtP_sp_laser_5, UtP_sp_laser_6;
    int UtP_RFID_SS_tag, UtP_RFID_SS_laser_front, UtP_RFID_SS_laser_rear;
    int UtP_RFID_LS_laser_front, UtP_RFID_LS_laser_rear;
    int[] UtP_SPSS_arr = new int[9];

    // init RMGC position
    Vector3 pos_init_RMGC, pos_init_Trolley, pos_init_spreader;
    float pos_init_TL, pos_init_TR, pos_init_Hoist;

    //// cmd
    // SPSS
    bool fb_tw_lock;
    bool fb_rope_slack;
    bool cmd_SPSS, fb_SPSS;

    // TL, TR, H
    float tl_SS_vel, tl_LS_vel, d_t, del_pos, tr_vel, H_vel;
    Vector3 tl_pos, tr_pos, H_scale;
    GameObject trolley;
    float theta, rail_width, skew_dev_LS;
    Vector3 tl_skew;

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
    Vector3 sp_pos, sp_rot;
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
    bool state_coll_old;

    // sensor data
    int[] arr_sensor_int;
    byte[] arr_sensor_bytes;
    bool rope_slack;

    // RFID
    public GameObject RFID_Tag, TL_Calib_Target;
    GameObject[] arr_RFID_Tag, arr_TL_Calib_Target;
    GameObject RFID_SS, RFID_LS;
    Vector3 pos_RFID_init, pos_RFID_tmp;
    float val_RFID_tag, range_recogn_RFID, dist_to_RFID_Tag;
    bool active_RFID;
    int idx_RFID;
    Laser_distance Laser_SS_Front, Laser_SS_Rear, Laser_LS_Front, Laser_LS_Rear;
    int num_RFID;

    // Micro motion
    float MM_pos_bay, MM_pos_row, MM_pos_CW;

    // Constant
    float conv_deg_to_rad = Mathf.PI / 180;

    // Start is called before the first frame update
    // Setting objects and init values
    void Start()
    {
        Debug.Log("start");

        //////////////////////////////////////////////////// Define data
        // random seed
        Random.InitState(42);

        // local control
        cmd_local_ctrl = false;

        // init RMGC position
        pos_init_TL = 6.0f;
        pos_init_TR = -10.0f;
        pos_init_Hoist = 21.6f;

        // plc preset
        ip = "192.168.100.10";
        rack = 0;
        slot = 1;

        // Comm data preset
        DBnum_read = 203;
        StartIdx_read = 0;
        LenIdx_read = 26;

        DBnum_write = 202;
        LenIdx_write = 60;
        num_bits_write = 8 * 10;

        // container variables
        interval_x = 0.5f;
        interval_z = 0.4f;
        err_rotate = 0f;
        err_shift = 0f;

        pos_container_size = Container_red.transform.localScale;
        pos_container_init = new Vector3(-12.075f, 1.2f, 0.0f);
        pos_container_init.y = pos_container_size.y / 2.0f;
        rotate_container_tmp = new Vector3(0, 0, 0);

        num_bay = 19;
        num_row = 9;

        arr_sensor_int = new int[10];
        arr_sensor_bytes = new byte[arr_sensor_int.Length * 2];

        // RFID
        pos_RFID_init = new Vector3(16.62f, 0, 0);
        range_recogn_RFID = 0.5f;   // [m], radius

        //////////////////////////////////////////////////// Preset

        // local control cmd
        if (cmd_local_ctrl == false)
        {
            // Connect PLC
            plc = new Plc(CpuType.S71500, ip, rack, slot);
            plc.Open();
        }

        //// comm
        bits_write = new BitArray(num_bits_write);
        bytes_write = new byte[LenIdx_write];

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
        Laser_name = new string[] {"Laser_TL_Front", "Laser_TL_Rear", 
                                   "Laser_TR_Front_L", "Laser_TR_Front_R",
                                   "Laser_TR_Rear_L", "Laser_TR_Rear_R", };
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

        //Debug.Log(arr_pully[0].transform.position.y);
        //Debug.Log(spreader_up.transform.position.y);
        //Debug.Log(spreader_down.transform.position.y);

        twist_lock = spreader_down.GetComponent<Twist_Lock_SPSS>();
        state_coll_old = twist_lock.state_coll;

        pully_diameter = arr_pully[0].transform.localScale.x * 2.0f;
        tw_lock = 0;
        rope_slack = false;

        //// RFID
        // Make instance
        num_RFID = num_bay * 4;
        arr_TL_Calib_Target = new GameObject[num_RFID];
        arr_RFID_Tag = new GameObject[num_RFID];
        for (int j = 0; j < num_RFID; j++)
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
            pos_RFID_tmp.z = ((interval_z + pos_container_size.z/2) / 2) * j;
            arr_TL_Calib_Target[j].transform.position = pos_RFID_tmp;

            //// RFID Tag
            // Make instance
            arr_RFID_Tag[j] = Instantiate(RFID_Tag);

            // set name
            arr_RFID_Tag[j].name = "RFID_Tag" + System.Convert.ToString(j + 1);

            // set parent
            arr_RFID_Tag[j].transform.SetParent(GameObject.Find("RFID_Tag").transform);

            // set position
            pos_RFID_tmp.x = pos_RFID_init.x;
            arr_RFID_Tag[j].transform.position = pos_RFID_tmp;

            // sea side RFID object
            RFID_SS = GameObject.Find("RFID_Reader_SS");
            RFID_LS = GameObject.Find("RFID_Reader_LS");

            // RFID Laser
            Laser_SS_Front = GameObject.Find("RFID_Reader_SS").transform.Find("Laser_Front").gameObject.GetComponent<Laser_distance>();
            Laser_SS_Rear = GameObject.Find("RFID_Reader_SS").transform.Find("Laser_Rear").gameObject.GetComponent<Laser_distance>();
            Laser_LS_Front = GameObject.Find("RFID_Reader_LS").transform.Find("Laser_Front").gameObject.GetComponent<Laser_distance>();
            Laser_LS_Rear = GameObject.Find("RFID_Reader_LS").transform.Find("Laser_Rear").gameObject.GetComponent<Laser_distance>();
        }

        // rail width
        GameObject rail_Left = GameObject.Find("Rail_Left");
        GameObject rail_Right = GameObject.Find("Rail_Right");
        rail_width = rail_Right.transform.position.x - rail_Left.transform.position.x;

        //// init RMGC position
        // TL
        pos_init_RMGC = transform.position;
        pos_init_RMGC.z = pos_init_TL;
        transform.position = pos_init_RMGC;

        // TR
        pos_init_Trolley = trolley.transform.position;
        pos_init_Trolley.x = pos_init_TR;
        trolley.transform.position = pos_init_Trolley;

        // Hoist
        pos_init_spreader = spreader.transform.position;
        pos_init_spreader.y = pos_init_Hoist - (spreader_down.transform.position.y - spreader_down.transform.localScale.y / 2);
        spreader.transform.position = pos_init_spreader;
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
        
        // bitarray
        bits_write[1] = rope_slack;
        bits_write[2] = fb_SPSS;

        // byte array. start idx : 0
        bits_write.CopyTo(bytes_write, 0);

        int[] arr_data = new int[16];
        arr_data[0] = (int)(RFID_SS.transform.position.z / conv_unit_m);
        arr_data[1] = (int)(RFID_LS.transform.position.z / conv_unit_m);
        arr_data[2] = (int)(tr_pos.x / conv_unit_m);
        arr_data[3] = (int)((spreader_down.transform.position.y - spreader_down.transform.localScale.y/2) / conv_unit_m);
        arr_data[4] = twist_lock.tw_lock_fb;
        arr_data[5] = (int)(arr_Laser[0].distance / conv_unit_m);
        arr_data[6] = (int)(arr_Laser[1].distance / conv_unit_m);
        arr_data[7] = (int)(arr_Laser[2].distance / conv_unit_m);
        arr_data[8] = (int)(arr_Laser[3].distance / conv_unit_m);
        arr_data[9] = (int)(arr_Laser[4].distance / conv_unit_m);
        arr_data[10] = (int)(arr_Laser[5].distance / conv_unit_m);
        arr_data[11] = (int)(val_RFID_tag);
        arr_data[12] = (int)(Laser_SS_Front.distance / conv_unit_m);
        arr_data[13] = (int)(Laser_SS_Rear.distance / conv_unit_m);
        arr_data[14] = (int)(Laser_LS_Front.distance / conv_unit_m);
        arr_data[15] = (int)(Laser_LS_Rear.distance / conv_unit_m);

        int i = 10;
        for(int j = 0; j < arr_data.Length; j++)
        {
            arr_bytes_temp = System.BitConverter.GetBytes(arr_data[j]);
            
            // data reverse
            bytes_write[i + 1] = arr_bytes_temp[0];
            bytes_write[i] = arr_bytes_temp[1];

            // update index
            i = i + 2;
        }

        //// SPSS
        if (cmd_SPSS)
        {
            fb_SPSS = true;
            // bay, row index
            int idx_bay = twist_lock.out_idx(arr_pos_bay, transform.position.z);

            // convert byte array
            int data = 0;
            for (int j = 0; j < num_row; j++)
            {
                data = arr_num_container[idx_bay, j];
                arr_bytes_temp = System.BitConverter.GetBytes(data);

                // data reverse
                bytes_write[i + 1] = arr_bytes_temp[0];
                bytes_write[i] = arr_bytes_temp[1];

                // update index
                i = i + 2;
            }

            // add
            //System.Buffer.BlockCopy(arr_sensor_bytes, 0, bytes_write, 40, arr_sensor_bytes.Length);
        }
        else
        {
            fb_SPSS = false;
        }

        // write
        plc.WriteBytes(DataType.DataBlock, DBnum_write, 0, bytes_write);
    }

    // Receive data from PLC
    // and update position based on data of PLC
    void PLC_to_Unity(){

        // Read data
        var data = plc.ReadBytes(DataType.DataBlock, DBnum_read, StartIdx_read, LenIdx_read);

        // read bool data
        byte[] byte_temp = new byte[2];
        byte_temp[0] = data[0];
        byte_temp[1] = data[1];
        BitArray bools = new BitArray(byte_temp);
        
        PtU_SPSS = bools[2];
        cmd_SPSS = PtU_SPSS;        

        // to convert big indian
        System.Array.Reverse(data);
        
        // Read raw data
        startIdx = 10;
        PtU_TL_SS_vel = System.BitConverter.ToInt16(data, LenIdx_read - (startIdx + len_int));
        startIdx = 12;
        PtU_TL_LS_vel = System.BitConverter.ToInt16(data, LenIdx_read - (startIdx + len_int));
        startIdx = 14;
        PtU_TR_vel = System.BitConverter.ToInt16(data, LenIdx_read - (startIdx + len_int));
        startIdx = 16;
        PtU_H_vel = System.BitConverter.ToInt16(data, LenIdx_read - (startIdx + len_int));
        startIdx = 18;
        PtU_tw_lock = System.BitConverter.ToInt16(data, LenIdx_read - (startIdx + len_int));
        startIdx = 20;
        PtU_MM_pos_bay = System.BitConverter.ToInt16(data, LenIdx_read - (startIdx + len_int));
        startIdx = 22;
        PtU_MM_pos_row = System.BitConverter.ToInt16(data, LenIdx_read - (startIdx + len_int));
        startIdx = 24;
        PtU_MM_pos_CW = System.BitConverter.ToInt16(data, LenIdx_read - (startIdx + len_int));

        // refine data
        tl_SS_vel = (float)(PtU_TL_SS_vel) / conv_unit_mm;
        tl_LS_vel = (float)(PtU_TL_LS_vel) / conv_unit_mm;
        tr_vel = (float)(PtU_TR_vel) / conv_unit_mm;
        H_vel = (float)(PtU_H_vel) / conv_unit_mm;
        tw_lock = PtU_tw_lock;

        MM_pos_bay = (float)(PtU_MM_pos_bay) / conv_unit_mm;
        MM_pos_row = (float)(PtU_MM_pos_row) / conv_unit_mm;
        MM_pos_CW = (float)(PtU_MM_pos_CW) * 0.1f;      // 0.1 deg
    }

    void manual_ctrl()
    {
        // Hoist, Twist lock

        if (Input.GetKey(KeyCode.LeftShift))
        {
            // Hoist
            if (Input.GetKey(KeyCode.UpArrow))
            {
                H_vel = 1000f;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                H_vel = -1000f;
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
                tl_SS_vel = 1f;
                tl_LS_vel = 1f;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                tl_SS_vel = -1f;
                tl_LS_vel = -1f;
            }
            else
            {
                tl_SS_vel = 0f;
                tl_LS_vel = 0f;
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
        del_pos = (tl_SS_vel + tl_LS_vel) / 2 * d_t;
        tl_pos.z += del_pos;
        transform.position = tl_pos;    // update drawing

        // skew
        tl_skew = transform.localEulerAngles;
        theta = tl_skew.y;
        skew_dev_LS = Mathf.Tan(theta * conv_deg_to_rad) * rail_width;
        skew_dev_LS += (tl_LS_vel - tl_SS_vel) * d_t;
        tl_skew.y = Mathf.Atan(skew_dev_LS / rail_width) / conv_deg_to_rad;
        transform.localEulerAngles = tl_skew;

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
            
            /*
            // ���� �ӵ����� õõ�� ȸ�����Ѽ� rope slack ȿ�� ����
            if (H_vel != 0)
            {
                motor.targetVelocity = H_vel / Mathf.Abs(H_vel) * (Mathf.Abs(H_vel) - 2);     
            }
            else
            {
                motor.targetVelocity = 0;
            }
            */

            hinge.motor = motor;
        }

        //// spreader position
        del_pos = H_vel * d_t;
        sp_pos = spreader.transform.localPosition;

        // collision
        if ((twist_lock.state_coll != state_coll_old) || rope_slack)
        {
            sp_pos_y_imag += del_pos;

            state_coll_old = twist_lock.state_coll;
            //Debug.Log(sp_pos.y - sp_pos_y_imag);
        }

        // imag ���� real ������ ���ų� Ŭ ���� update
        // rope slack ���� ����
        if (sp_pos.y <= sp_pos_y_imag)
        {
            sp_pos.y += del_pos;

            sp_pos_y_imag = sp_pos.y;
            rope_slack = false;
        }
        else
        {
            rope_slack = true;
        }

        // Micro motion
        sp_pos.x = MM_pos_row;
        sp_pos.z = MM_pos_bay;
        sp_rot = spreader.transform.localEulerAngles;
        sp_rot.y = MM_pos_CW;

        // apply spreader position
        spreader.transform.localPosition = sp_pos;
        spreader.transform.localEulerAngles = sp_rot;

        //// Twist lock
        twist_lock.tw_lock = tw_lock;

    }

    void RFID()
    {
        
        for(int i = 0; i < arr_RFID_Tag.Length; i++){
            
            pos_RFID_tmp = arr_RFID_Tag[i].transform.position;
            dist_to_RFID_Tag = (RFID_SS.transform.position - pos_RFID_tmp).magnitude;
            
            // check recognizing range
            if (dist_to_RFID_Tag < range_recogn_RFID)
            {
                val_RFID_tag = (i + 1) * 10;
                active_RFID = true;
                break;
            }
            else
            {
                active_RFID = false;
            }
        }
    }
}
