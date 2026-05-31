using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class EyeLogWriter : MonoBehaviour
{
    public ExpController expController;
    public GameObject expCamera;
    private string date, filePath, fileName, fileFullPath;
    private FileStream fs;
    private StreamWriter sw;
    private int caseTimes, roundTimes;
    public bool started = false;
    void Start()
    {
        expController = GetComponent<ExpController>();
        expCamera = GameObject.Find("CenterEyeAnchor");
        date = DateTime.Now.ToString("yyyyMMdd-HH-mm-ss");
        filePath = Application.persistentDataPath + "/Logs/" + date + "/";
        fileName = date + "-" + "Eye" + ".csv";
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
            started = false;
        });
        expController.caseOver.AddListener(increCase);
        expController.roundOver.AddListener(increRound);
    }
    private void FixedUpdate()
    {
        if (started)
        {
            print("eye");
            print(CoreServices.InputSystem.EyeGazeProvider.IsEyeTrackingDataValid);
            Write();
        }
    }
    public string FormatMsg()
    {
        return roundTimes.ToString() + "," + caseTimes.ToString()
            + "," + CoreServices.InputSystem.EyeGazeProvider.GazeOrigin.x + "," + CoreServices.InputSystem.EyeGazeProvider.GazeOrigin.y + "," + CoreServices.InputSystem.EyeGazeProvider.GazeOrigin.z
            + "," + CoreServices.InputSystem.EyeGazeProvider.GazeDirection.x + "," + CoreServices.InputSystem.EyeGazeProvider.GazeDirection.y + "," + CoreServices.InputSystem.EyeGazeProvider.GazeDirection.z
            + "," + CoreServices.InputSystem.EyeGazeProvider.HeadMovementDirection.x + "," + CoreServices.InputSystem.EyeGazeProvider.HeadMovementDirection.y + "," + CoreServices.InputSystem.EyeGazeProvider.HeadMovementDirection.z
            + "," + expCamera.transform.position.x + "," + expCamera.transform.position.y + "," + expCamera.transform.position.z
            + "," + expCamera.transform.rotation.x + "," + expCamera.transform.rotation.y + "," + expCamera.transform.rotation.z
            + "," + expController.newTargetPos.x + "," + expController.newTargetPos.y + "," + expController.newTargetPos.z;
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
            sw.WriteLine("Round,Case,GazeOriginX,GazeOriginY,GazeOriginZ,GazeDirectionX,GazeDirectionY,GazeDirectionZ,HeadMovementDirectionX,HeadMovementDirectionY,HeadMovementDirectionZ,HeadX,HeadY,HeadZ,HeadDrectionX,HeadDrectionY,HeadDrectionZ,LocalTargetX,LocalTargetY,LocalTargetZ");
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



