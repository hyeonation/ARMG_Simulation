using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using S7.Net;
using System.Threading;

public class ex220118 : MonoBehaviour
{
    Plc plc;

    string ip;
    short rack, slot;
    float tr_vel, d_t, del_pos;
    Vector3 vec_pos;

    Collision coll;

    public GameObject bullet;

    RaycastHit hit;
    float maxDistance = 15f;
    
    Vector3 point_src = new Vector3(0, 0.3f, -10);
    Vector3 dir = new Vector3(0, 0, 1);

    float time_start;

    float[] arr_dist = new float[4800];
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("start");

        ip = "192.168.1.2";
        rack = 0;
        slot = 1;

        plc = new Plc(CpuType.S71500, ip, rack, slot);

        plc.Open();

        bool aa = (bool)plc.Read("DB1000.DBX0.0");
        var data = plc.ReadBytes(DataType.DataBlock, 203, 0, 6);

        Array.Reverse(data);

        coll = new Collision();

        plc.Close();

        
        
        bool bool_hit = Physics.Raycast(point_src, dir, out hit, maxDistance);
        Debug.DrawRay(point_src, dir * maxDistance, Color.red, 1f);
        Debug.Log(bool_hit);
        Debug.Log(hit.distance);
    }

    // Update is called once per frame
    void Update()
    {
        /*
        //var data = plc.ReadBytes(DataType.DataBlock, 203, 0, 6);
        
        // to convert big indian
        //Array.Reverse(data);

        // refine data
        //tr_vel = ((float)(BitConverter.ToInt16(data, 4)));
        d_t = Time.deltaTime;

        // shift position
        vec_pos = transform.position;
        //del_pos = tr_vel * d_t / 16384;
        del_pos = tr_vel * d_t / 3000;
        vec_pos.z += del_pos;
        //Debug.Log(tr_vel);
        //Debug.Log(del_pos);
        //Debug.Log(d_t);
        
        transform.position = vec_pos;
        */

        time_start = Time.time;

        for (int i=0; i < 4800; i++){
            
            bool bool_hit = Physics.Raycast(point_src, dir, out hit, maxDistance);
            arr_dist[i] = hit.distance;
        }
        Debug.Log(arr_dist[1199]);

        //Thread.Sleep(1000);

        //Debug.Log(time_start);
        Debug.Log(Time.time);
        //Debug.Log((Time.time - time_start) * 1000);
        Debug.Log(Time.deltaTime);

        

    }

    private void OnCollisionEnter(Collision other) {

        Debug.Log("충돌 시작!");
        
        foreach (ContactPoint contact in other.contacts)
        {
            Debug.Log(contact.point);
        }
    }

    private void OnCollisionStay(Collision other)
    {

        Debug.Log("충돌 중!");
        //Debug.Log(other.contacts);
        //Debug.Log(other.contacts[0]);
        //Debug.Log(other.contacts[0].GetType().Name);
        //Debug.Log(other.contacts[0].point.GetType().Name);

        foreach (ContactPoint contact in other.contacts){
            Debug.Log(contact.point);
        }
        
    }

    private void OnCollisionExit(Collision other)
    {

        Debug.Log("충돌 끝!");
        
    }
    
}
