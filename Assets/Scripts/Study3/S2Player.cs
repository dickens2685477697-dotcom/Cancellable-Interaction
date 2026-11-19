using System.Collections;
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
    private BallSpawner ballSpawner;
    private S2AccMode accMode;
    private Coroutine mineBallRevealDelayCoroutine;
    private Coroutine yellowCueFrameCoroutine;
    internal double? YellowCueFrameRealtime { get; private set; }
    private int mineBallRevealDelayVersion = 0;
    private bool mineBallRevealDelayElapsed = false;
    private bool pendingMineBallCueRequested;
    private Material pendingMineBallRevealMaterial;
    private void Start()
    {
        acce = gameObject.GetComponentInParent<AcceStimulate>();
        accMode = gameObject.GetComponentInParent<S2AccMode>();
        GameObject spawnerObject = GameObject.Find("Spawner");
        if (spawnerObject != null)
        {
            ballSpawner = spawnerObject.GetComponent<BallSpawner>();
        }
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
        CancelMineBallRevealDelay();
        //更改外形为粉色
        gameObject.GetComponent<MeshRenderer>().material = pinkBall;
    }
    public void ChangeOpacityBack()
    {
        CancelMineBallRevealDelay();
        //更改外形为红色
        gameObject.GetComponent<MeshRenderer>().material = redBall;
        //if (!expStarted)
        //{
        //    redMeshRenderer.material = greenBall;
        //}
    }

    public void TurnYellowMineBall()
    {
        RevealMineBallMaterial(yellowBall);
    }
    public void TurnGreenMineBall()
    {
        RevealMineBallMaterial(greenBall);
    }
    public void TurnShallowYellowMineBall()
    {
        RevealMineBallMaterial(shallowYellowBall);
    }
    public void TurnShallowGreenMineBall()
    {
        RevealMineBallMaterial(shallowGreenBall);
    }


    public void TurnYellow()
    {
        CancelMineBallRevealDelay();
        gameObject.GetComponent<MeshRenderer>().material = materials[colorIndex];
        Acced = true;
        colorIndex = (colorIndex + 1) % 3;
    }
    public void TurnYellowBack()
    {
        CancelMineBallRevealDelay();
        gameObject.GetComponent<MeshRenderer>().material = blueBall;
        //print("变蓝1");
    }
    public void TurnShallow()
    {
        CancelMineBallRevealDelay();
        gameObject.GetComponent<MeshRenderer>().material = shallowBlueBall;
        Acced = false;

    }
    public void TurnShallowBack()
    {
        CancelMineBallRevealDelay();
        if (!Acced)
        {
            gameObject.GetComponent<MeshRenderer>().material = ordinaryBall;
            print("变蓝1");
        }


    }
    public void showCircle()
    {
        CancelMineBallRevealDelay();
        //circle.SetActive(true);
        gameObject.GetComponent<MeshRenderer>().material = reactedBall;
    }
    public void hideCircle()
    {
        CancelMineBallRevealDelay();
        //circle.SetActive(false);
        gameObject.GetComponent<MeshRenderer>().material = redBall;
    }

    internal void RevealPendingMineBallCue(bool isMineBall)
    {
        if (pendingMineBallCueRequested)
        {
            return;
        }

        ClearYellowCueTiming();
        ApplyMineBallMaterial(isMineBall ? shallowYellowBall : shallowGreenBall);
        pendingMineBallCueRequested = true;
    }

    private void RevealMineBallMaterial(Material material)
    {
        if (accMode != null && accMode.UsesPendingMineBallCue)
        {
            return;
        }

        ApplyMineBallMaterial(material);
    }

    private void ApplyMineBallMaterial(Material material)
    {
        if (!ShouldDelayMineBallReveal())
        {
            CancelMineBallRevealDelay();
            SetMineBallMaterial(material);
            return;
        }

        if (mineBallRevealDelayElapsed)
        {
            SetMineBallMaterial(material);
            return;
        }

        pendingMineBallRevealMaterial = material;
        if (mineBallRevealDelayCoroutine == null)
        {
            StartMineBallRevealDelay();
        }
    }

    private bool ShouldDelayMineBallReveal()
    {
        return ballSpawner != null
            && ballSpawner.enableMineBall
            && ballSpawner.enableMineBallRevealDelay
            && ballSpawner.mineBallRevealDelayTime > 0f
            && accMode != null;
    }

    private void StartMineBallRevealDelay()
    {
        mineBallRevealDelayVersion++;
        mineBallRevealDelayCoroutine = StartCoroutine(DelayMineBallReveal(mineBallRevealDelayVersion));
    }

    private IEnumerator DelayMineBallReveal(int version)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, ballSpawner.mineBallRevealDelayTime));

        if (version != mineBallRevealDelayVersion || ballSpawner == null || accMode == null)
        {
            yield break;
        }

        mineBallRevealDelayElapsed = true;
        if (pendingMineBallRevealMaterial != null)
        {
            SetMineBallMaterial(pendingMineBallRevealMaterial);
        }
        mineBallRevealDelayCoroutine = null;
    }

    private void SetMineBallMaterial(Material material)
    {
        var renderer = GetComponent<MeshRenderer>();
        renderer.material = material;
        if (accMode != null && accMode.UsesPendingMineBallCue && accMode.isMineBall
            && material == shallowYellowBall && material != null
            && !YellowCueFrameRealtime.HasValue && yellowCueFrameCoroutine == null)
        {
            yellowCueFrameCoroutine = StartCoroutine(RecordYellowCueFrame(renderer, material));
        }
    }

    private IEnumerator RecordYellowCueFrame(MeshRenderer renderer, Material cueMaterial)
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (true)
        {
            yield return endOfFrame;
            if (renderer == null || !renderer.enabled || renderer.sharedMaterial != cueMaterial)
            {
                break;
            }

            if (renderer.isVisible)
            {
                YellowCueFrameRealtime = Time.realtimeSinceStartupAsDouble;
                break;
            }
        }

        yellowCueFrameCoroutine = null;
    }

    internal void ClearYellowCueTiming() => YellowCueFrameRealtime = null;

    private void CancelMineBallRevealDelay()
    {
        if (yellowCueFrameCoroutine != null)
        {
            StopCoroutine(yellowCueFrameCoroutine);
            yellowCueFrameCoroutine = null;
        }

        pendingMineBallCueRequested = false;
        mineBallRevealDelayVersion++;
        mineBallRevealDelayElapsed = false;
        pendingMineBallRevealMaterial = null;
        if (mineBallRevealDelayCoroutine != null)
        {
            StopCoroutine(mineBallRevealDelayCoroutine);
            mineBallRevealDelayCoroutine = null;
        }
    }

    private void OnDisable()
    {
        CancelMineBallRevealDelay();
        ClearYellowCueTiming();
    }
}
