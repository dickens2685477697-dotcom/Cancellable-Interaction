using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyLock : MonoBehaviour
{
    GameObject expCamera;
    // Start is called before the first frame update
    void Start()
    {
        expCamera = GameObject.Find("CenterEyeAnchor");
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.position = expCamera.transform.position;
    }
}
