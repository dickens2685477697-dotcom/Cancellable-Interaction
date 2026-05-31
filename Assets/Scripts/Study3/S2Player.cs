using System.Collections.Generic;
using UnityEngine;

public class S2Player : MonoBehaviour
{
    public AcceStimulate acce;
    [Tooltip("雷球进入后显示的材质")]
    public Material yellowBall;
    public Material shallowYellowBall;
    public Material blueBall;
    public Material shallowBlueBall;
    public Material ordinaryBall;
    public Material redBall;
    public Material pinkBall;
    public Material greenBall;
    public Material shallowGreenBall;
    private List<Material> materials;
    private int colorIndex = 0;
    public GameObject circle;
    //Acce被触发
    public bool Acced = false;
    public Material reactedBall;
    private void Start()
    {
        acce = gameObject.GetComponentInParent<AcceStimulate>();
        materials = new List<Material>
        {
            yellowBall,
            redBall,
            greenBall,
            pinkBall
        };

    }
    public void ChangeOpacity()
    {
        //更改外形为粉色
        gameObject.GetComponent<MeshRenderer>().material = pinkBall;
    }
    public void ChangeOpacityBack()
    {
        //更改外形为红色
        gameObject.GetComponent<MeshRenderer>().material = redBall;
        //if (!expStarted)
        //{
        //    redMeshRenderer.material = greenBall;
        //}
    }

    public void TurnYellowMineBall()
    {
        gameObject.GetComponent<MeshRenderer>().material = yellowBall;
    }
    public void TurnGreenMineBall()
    {
        gameObject.GetComponent<MeshRenderer>().material = greenBall;
    }
    public void TurnShallowYellowMineBall()
    {
        gameObject.GetComponent<MeshRenderer>().material = shallowYellowBall;
    }
    public void TurnShallowGreenMineBall()
    {
        gameObject.GetComponent<MeshRenderer>().material = shallowGreenBall;
    }


    public void TurnYellow()
    {
        gameObject.GetComponent<MeshRenderer>().material = materials[colorIndex];
        Acced = true;
        colorIndex = (colorIndex + 1) % 3;
    }
    public void TurnYellowBack()
    {
        gameObject.GetComponent<MeshRenderer>().material = blueBall;
        //print("变蓝1");
    }
    public void TurnShallow()
    {
        gameObject.GetComponent<MeshRenderer>().material = shallowBlueBall;
        Acced = false;

    }
    public void TurnShallowBack()
    {
        if (!Acced)
        {
            gameObject.GetComponent<MeshRenderer>().material = ordinaryBall;
            print("变蓝1");
        }


    }
    public void showCircle()
    {
        //circle.SetActive(true);
        gameObject.GetComponent<MeshRenderer>().material = reactedBall;
    }
    public void hideCircle()
    {
        //circle.SetActive(false);
        gameObject.GetComponent<MeshRenderer>().material = redBall;
    }

}
