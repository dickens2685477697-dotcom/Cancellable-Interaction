using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Random = UnityEngine.Random;
using UnityEngine.Events;
using System.Runtime.CompilerServices;

public class ExpController : MonoBehaviour
{
    readonly string sArguments = @"SpawnBall2.py";//这里是python的文件名
    private GameObject over;

    [Header("ball")]
    public GameObject startBall;
    public GameObject targetBall;

    [Header("parameters")]
    public int clickTargetTimes = 10;
    public float radius = 0.02f;
    public int roundsTotal = 3;

    [Header("Events")]
    public UnityEvent roundOver = new UnityEvent();
    public UnityEvent caseStart = new UnityEvent();
    public UnityEvent caseOver = new UnityEvent();
    public UnityEvent expStart = new UnityEvent();

    static List<float> coordinates = new List<float>();

    static List<Vector3> targetCoordinates = new List<Vector3>();
    static List<Vector3> targetCoordinates2 = new List<Vector3>();
    static List<Vector3> targetCoordinates3 = new List<Vector3>();
    public Vector3 newTargetPos;

    [Header("Proceeding")]
    public int clickTimes = 0;
    public int roundTimes = 0;
    // Start is called before the first frame update
    void Start()
    {
        startBall.GetComponent<Set4AIStart>().startClicked.AddListener(StartClicked);
        targetBall.GetComponent<Set4AITarget>().targetClicked.AddListener(TargetClicked);
        over = GameObject.Find("over");
        over.SetActive(false);
        //startBall.GetComponent<Set4AITarget>().targetInEvent.AddListener(() =>
        //{
        //    caseOver.Invoke();
        //});
        //由于临时生成目标球位置会卡，所以提前生成
        InitExp1();
        InitExp2();
        InitExp3();
        expStart.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void StartClicked()
    {
        //起点球被点击后，起点球消失，目标球出现
        UnableStart();
        //获得下一个目标球位置
        newTargetPos = GetNewTargetPos();
        newTargetPos.y += 1f;
        newTargetPos.z += 0.3f;
        targetBall.transform.position = newTargetPos;
        EnableTarget();
        caseStart.Invoke();
        
    }
    private void TargetClicked()
    {

        //一例实验结束
        caseOver.Invoke();
        clickTimes++;
        //一轮实验结束
        if (clickTimes == clickTargetTimes)
        {
            roundOver.Invoke();
            roundTimes++;
            targetCoordinates.Clear();
            if (roundTimes == 1) {
                targetCoordinates = targetCoordinates2;
                //radius = 0.03f;
                //改变球半径
                //Vector3 ballScale = new Vector3(2*radius, 2*radius, 2*radius);
                //startBall.transform.localScale = ballScale;
                //targetBall.transform.localScale = ballScale;               
            }
            else if (roundTimes == 2)
            {
                targetCoordinates = targetCoordinates3;
                //radius = 0.045f;
                //改变球半径
                //Vector3 ballScale = new Vector3(2 * radius, 2 * radius, 2 * radius);
                //startBall.transform.localScale = ballScale;
                //targetBall.transform.localScale = ballScale;
            }
            else if (roundTimes == 3)
            {
                UnableTarget();
                UnableStart();
                over.SetActive(true);
                return;
            }
            clickTimes = 0;
        }

        //目标球被点击后，目标球消失，起点球出现
        startBall.GetComponent<Set4AIStart>().inBall = false;
        UnableTarget();
        EnableStart();
    }
    private void InitExp1()
    {
        targetCoordinates.Clear();
        //生成所有目标球位置
        RunPythonScript(sArguments, "");
        int temp = 0;
        for (int i = 0; i < clickTargetTimes; i++)
        {
            temp = Random.Range(0, coordinates.Count / 3 - 1);
            targetCoordinates.Add(new Vector3(coordinates[temp * 3], coordinates[temp * 3 + 1], coordinates[temp * 3 + 2]));
        }
    }
    private void InitExp2()
    {
        targetCoordinates2.Clear();
        //生成所有目标球位置
        RunPythonScript(sArguments, "");
        int temp = 0;
        for (int i = 0; i < clickTargetTimes; i++)
        {
            temp = Random.Range(0, coordinates.Count / 3 - 1);
            targetCoordinates2.Add(new Vector3(coordinates[temp * 3], coordinates[temp * 3 + 1], coordinates[temp * 3 + 2]));
        }
    }
    private void InitExp3()
    {
        targetCoordinates3.Clear();
        //生成所有目标球位置
        RunPythonScript(sArguments, "");
        int temp = 0;
        for (int i = 0; i < clickTargetTimes; i++)
        {
            temp = Random.Range(0, coordinates.Count / 3 - 1);
            targetCoordinates3.Add(new Vector3(coordinates[temp * 3], coordinates[temp * 3 + 1], coordinates[temp * 3 + 2]));
        }
    }
    private Vector3 GetNewTargetPos()
    {
        Vector3 vector3 = targetCoordinates[clickTimes];
        //vector3.x -= 0.4f;
        //vector3.y -= 0.4f;
        //vector3.z += 0.3f;
        return vector3;
    }
    private void EnableStart()
    {
        startBall.SetActive(true);
    }
    private void UnableStart()
    {
        startBall.SetActive(false);
    }
    private void EnableTarget()
    {
        targetBall.SetActive(true);
    }
    private void UnableTarget()
    {
        targetBall.SetActive(false);
    }
    public static void RunPythonScript(string sArgName, string args = "")
    {
        Process p = new Process();
        //python脚本的路径
        string path = @"C:\Users\Liu_Yixuan\Downloads\MagicTapandKeyboard\Assets\Py\" + sArgName;
        path = path + " " + args;
        string sArguments = path;


        //(注意：用的话需要换成自己的)没有配环境变量的话，可以像我这样写python.exe的绝对路径
        //(用的话需要换成自己的)。如果配了，直接写"python.exe"即可
        p.StartInfo.FileName = @"C:\Users\Neo-R\miniconda3\python.exe";
        //p.StartInfo.FileName = @"C:\Program Files\Python35\python.exe";

        // sArguments为python脚本的路径   python值的传递路线strArr[]->teps->sigstr->sArguments 
        //在python中用sys.argv[ ]使用该参数
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.Arguments = sArguments;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.CreateNoWindow = true;
        p.Start();
        p.BeginOutputReadLine();
        p.OutputDataReceived += new DataReceivedEventHandler(Out_RecvData);
        Console.ReadLine();
        p.WaitForExit();
    }

    static void Out_RecvData(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            //UnityEngine.Debug.Log(e.Data);
            
            coordinates.Add(float.Parse(e.Data));
        }
    }
}
