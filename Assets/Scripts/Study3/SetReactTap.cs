using Oculus.Platform.Samples.VrHoops;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// This is the script to control the touchable ball (target ball) behaviour and response to user behaviour

public class SetReactTap : MonoBehaviour
{
    public ReactTap Acce;
    public GameObject ball, ball2;
    public S2Player Player, Player2;
    public BallSpawner ballSpawner;
    public int modeSelected;
    public AudioSource source;
    public AudioClip clip;
    public List<GameObject> balls;
    public bool inBall = false;
    public bool canceled = false;
    public int num;
    private bool handInBall = false;

    public void Start()
    {
        num = 2;
        Acce = GetComponent<ReactTap>();
        Player = ball.GetComponent<S2Player>();
        Player2 = ball2.GetComponent<S2Player>();
        SetAngleMode();
    }

    // 新增 SetAngleMode 方法
    public void SetAngleMode()
    {
        Acce.InEvent.AddListener(() =>
        {
            if (!handInBall)
            {
                // 记录初始夹角
                Acce.initialAngle = Acce.CalculateAngle_between_finger_wrist(); // 假设 CalculateAngle_between_finger_wrist 方法用于计算夹角
                Debug.Log("initial angle:" + Acce.initialAngle);
            }
        });

        Acce.HesEvent.AddListener(() =>
        {
            canceled = true;
            Debug.Log("Hes!");
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            Player2.TurnShallowBack();
            canceled = false;
        });

        Acce.OutEvent.AddListener(() =>
        {
            print("out触发");
            if (!canceled)
            {
                
            }
            //！！！先暂时放在判断条件外面，如果后续需要取消再思考 lyx
            Player.TurnYellow();
            print("变色");
        });
    }


}