using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Male_body_select : MonoBehaviour {

    public GameObject B1;
    public GameObject B2;
    public GameObject B3;
    public GameObject B4;
    public GameObject B5;

    public GameObject H1;
    public GameObject H2;
    public GameObject H3;
    public GameObject H4;
    public GameObject H5;

    private int BS = 1;
    private int HS = 1;

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

        //Body selection 
        if (BS == 1)
        {
            B1.SetActive(true);
            B2.SetActive(false);
            B3.SetActive(false);
            B4.SetActive(false);
            B5.SetActive(false);
        } else if (BS == 2)
        {
            B1.SetActive(false);
            B2.SetActive(true);
            B3.SetActive(false);
            B4.SetActive(false);
            B5.SetActive(false);
        }
        else if (BS == 3)
        {
            B1.SetActive(false);
            B2.SetActive(false);
            B3.SetActive(true);
            B4.SetActive(false);
            B5.SetActive(false);
        }
        else if (BS == 4)
        {
            B1.SetActive(false);
            B2.SetActive(false);
            B3.SetActive(false);
            B4.SetActive(true);
            B5.SetActive(false);
        }
        else if (BS == 5)
        {
            B1.SetActive(false);
            B2.SetActive(false);
            B3.SetActive(false);
            B4.SetActive(false);
            B5.SetActive(true);
        }
        //Head selection
        if (HS == 1)
        {
            H1.SetActive(true);
            H2.SetActive(false);
            H3.SetActive(false);
            H4.SetActive(false);
            H5.SetActive(false);
        }
        else if (HS == 2)
        {
            H1.SetActive(false);
            H2.SetActive(true);
            H3.SetActive(false);
            H4.SetActive(false);
            H5.SetActive(false);
        }
        else if (HS == 3)
        {
            H1.SetActive(false);
            H2.SetActive(false);
            H3.SetActive(true);
            H4.SetActive(false);
            H5.SetActive(false);
        }
        else if (HS == 4)
        {
            H1.SetActive(false);
            H2.SetActive(false);
            H3.SetActive(false);
            H4.SetActive(true);
            H5.SetActive(false);
        }
        else if (HS == 5)
        {
            H1.SetActive(false);
            H2.SetActive(false);
            H3.SetActive(false);
            H4.SetActive(false);
            H5.SetActive(true);
        }

    }
    public void BodySelect()
    {
        if (BS < 5)
        {
            BS++;
        }
        else if (BS == 5)
        {
            BS = 1;
        }
       
    }
    public void HeadSelect()
    {
        if (HS < 5)
        {
            HS++;
        }
        else if (HS == 5)
        {
            HS = 1;
        }
    }



}
