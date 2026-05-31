using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Oculus.Platform.Samples.VrBoardGame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class RoundLogWriter : MonoBehaviour
{
    public ExpController expController;
    public GameObject expCamera;
    private string date, filePath, fileName, fileFullPath;
    private FileStream fs;
    private StreamWriter sw;
    private int caseTimes, roundTimes;
    private bool started = false;
    private MixedRealityPose pose;
    private List<Vector3> poses = new List<Vector3>();
    void Start()
    {
        expController = GetComponent<ExpController>();
        expCamera = GameObject.Find("CenterEyeAnchor");
        date = DateTime.Now.ToString("yyyyMMdd-HH-mm-ss");
        filePath = Application.persistentDataPath + "/Logs/" + date + "/";
        fileName = date + "-" + "Round" + ".csv";
        fileFullPath = filePath + fileName;
        Debug.Log(fileFullPath);
        //acce = gameObject.GetComponent<AcceStimulate>();
        caseTimes = 1;
        roundTimes = 1;
        InitialPath();
        expController.caseStart.AddListener(() => {
            started = true;

        });
        expController.caseOver.AddListener(() =>
        {
            Write();
            started = false;
        });
        expController.caseOver.AddListener(increCase);
        expController.roundOver.AddListener(increRound);
    }
    private void FixedUpdate()
    {
        if (started)
        {

        }
    }
    public string FormatMsg()
    {
        string result = string.Empty;
        result += roundTimes.ToString() + "," + caseTimes.ToString() + "," + expController.newTargetPos.x + "," + expController.newTargetPos.y + "," + expController.newTargetPos.z + "," +
            expCamera.transform.position.x + "," + expCamera.transform.position.y + "," + expCamera.transform.position.z + "," +
            expController.targetBall.transform.position.x + "," + expController.targetBall.transform.position.y + "," + expController.targetBall.transform.position.z;
        return result;
    }
    public void InitialPath()
    {
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }
        if (!File.Exists(fileFullPath))
        {
            // Close the operator after creation
            fs = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);
            sw = new StreamWriter(fs);
            sw.WriteLine("Round,Case,LocalTargetX,LocalTargetY,LocalTargetZ,HeadX,HeadY,HeadZ,TargetX,TargetY,TargetZ");
            sw.Close();
            fs.Close();
        }
    }

    public void Write()
    {
        // New an fs and sw, close an fs and sw after writing
        // because a writer and file stream will lock the memory storage so that other gameobject cannot write
        fs = new FileStream(fileFullPath, FileMode.Append, FileAccess.Write);
        sw = new StreamWriter(fs);
        string log = FormatMsg();
        sw.WriteLine(log);
        sw.Close();
        fs.Close();
    }
    public void increCase()
    {

        caseTimes++;


    }
    public void increRound()
    {
        roundTimes++;


    }
}



