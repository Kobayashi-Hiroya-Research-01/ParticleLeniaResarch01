using Assets.ParticleLenia.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.Fields;
using Assets.ParticleLeniaResarch01.Scripts.Initializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ParticleLeniaController : MonoBehaviour
{
    public class Serializer : IParticleFieldsSerializer
    {
        public static Serializer Instance { get; } = new Serializer();

        public IParticleFields Read(string text)
        {
            return new ParticleFieldsOriginal(JsonUtility.FromJson<ParticleFieldsOriginal.State>(text));
        }

        public string Write(IParticleFields fields)
        {
            return JsonUtility.ToJson(((ParticleFieldsOriginal)fields).GetState());
        }
    }

    [Serializable]
    public class PlayerOptions
    {
        public bool isPlay;
        public int seedIncrement = 1;

        [Min(1)]
        public int stepParFrame = 1;
    }

    public PLViewModeController viewModeController;
    public PLCameraController cameraController;
    public PLMinMax minMax;
    public UtHistController utHistController;
    public PLDBSCAN[] pLDBSCANs;
    public GameObject particlePrefab;
    public PlayerOptions playerOptions;
    //public bool updateTimeLine;
    //public bool viewTimeLine;
    //public ParticleLeniaTimeLine.Record timeLine;

    public OriginalInitializer.State simulationOptions;

    private List<PLParticleController> particles;

    private ParticleLeniaSimulation simulation;

    public List<PLParticleController> Particles => particles;
    public DistanceCache LastDistances => simulation.LastDistances;

    private void Awake()
    {
        if (viewModeController == null)
        {
            viewModeController = FindFirstObjectByType<PLViewModeController>();
        }

        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<PLCameraController>();
        }

        if (minMax == null)
        {
            minMax = FindFirstObjectByType<PLMinMax>();
        }

        if (utHistController == null)
        {
            utHistController = FindFirstObjectByType<UtHistController>();
        }

        if (pLDBSCANs == null || pLDBSCANs.Length == 0)
        {
            pLDBSCANs = FindObjectsByType<PLDBSCAN>(FindObjectsSortMode.None).OrderBy(e => e.name, StringComparer.Ordinal).ToArray();
        }
        cameraController.Awake();
    }

    private void OnEnable()
    {
        viewModeController = viewModeController != null ? viewModeController : FindFirstObjectByType<PLViewModeController>();
        cameraController = cameraController != null ? cameraController : FindFirstObjectByType<PLCameraController>();

        var oldParticles = GetComponentsInChildren<PLParticleController>();
        foreach (var p in oldParticles)
        {
            Destroy(p.gameObject);
        }

        simulation = new();
        particles = new();
        var initializer = new OriginalInitializer();
        initializer.SetState(simulationOptions);
        simulation.Initializer = initializer;
        SimulationInit();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            playerOptions.isPlay = !playerOptions.isPlay;
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            simulationOptions.seed += playerOptions.seedIncrement;
            SimulationInit();
        }

        if (playerOptions.isPlay || Input.GetKeyUp(KeyCode.N))
        {
            utHistController.SimulationInit();
            minMax.SimulationInit();
            simulation.Next(playerOptions.stepParFrame, IntermediatePostAction, LastPostAction, PostStepAction);
        }
    }

    private void IntermediatePostAction(ParticleInfo info, int index)
    {
        minMax.IntermediatePostAction(info);
        particles[index].InfoUpdate(info);
        //ÉåÉRÅ[Éhèàóù
    }

    private void LastPostAction(ParticleInfo info, int index)
    {
        utHistController.LastPostAction(info);
        minMax.LastPostAction(info);
        particles[index].InfoUpdate(info);
    }

    private void PostStepAction()
    {
        Parallel.ForEach(pLDBSCANs, PLDBSCANUpdate);
    }

    private void PLDBSCANUpdate(PLDBSCAN pLDBSCAN)
    {
        pLDBSCAN.UpdateSimulationAfter();
    }

    private void SimulationInit()
    {
        foreach (var p in particles)
        {
            Destroy(p.gameObject);
        }
        particles.Clear();

        minMax.SimulationInit();
        utHistController.SimulationInit();
        cameraController.SimulationInit(minMax);
        foreach (var e in pLDBSCANs)
        {
            e.SimulationInit();
        }

        simulation.Init(InitPostAction);
        PostStepAction();
    }

    private void InitPostAction(ParticleInfo info, int index)
    {
        var go = Instantiate(particlePrefab, transform);
        var component = go.GetComponent<PLParticleController>();
        component.InitPostAction(info, viewModeController, minMax, index);
        particles.Add(component);

        utHistController.InitPostAction(info);
        minMax.InitPostAction(info);

        foreach (var e in pLDBSCANs)
        {
            e.InitPostAction(component);
        }
    }

    [SerializeField]
    [HideInInspector]
    private double _oldWk;

    [SerializeField]
    [HideInInspector]
    private bool _oldAutoWk;

    private void OnValidate()
    {
        simulationOptions ??= new();
        simulationOptions.OnValidate(ref _oldAutoWk, ref _oldWk);
    }
}