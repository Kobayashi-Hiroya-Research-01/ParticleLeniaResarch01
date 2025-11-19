using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.Fields;
using Assets.ParticleLeniaResarch01.Scripts.Initializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
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
    public GameObject particlePrefab;
    public PlayerOptions playerOptions;
    //public bool updateTimeLine;
    //public bool viewTimeLine;
    //public ParticleLeniaTimeLine.Record timeLine;

    public OriginalInitializer.State simulationOptions;

    private List<PLParticleController> particles;

    private ParticleLeniaSimulation simulation;

    private void Awake()
    {
        viewModeController = viewModeController != null ? viewModeController : FindFirstObjectByType<PLViewModeController>();
        cameraController = cameraController != null ? cameraController : FindFirstObjectByType<PLCameraController>();
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

        if (Input.GetKeyUp(KeyCode.S))
        {
            string json = JsonUtility.ToJson(simulation.TimeLine.ToRecord(Serializer.Instance), true);

            var date = DateTime.Now.ToShortString();
            var filePath = EditorUtility.SaveFilePanel("Save TimeLine", "Assets", $"ParticleLenia_TimeLine_{date}", "json");

            if (!string.IsNullOrEmpty(filePath))
            {
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
        }

        if (playerOptions.isPlay)
        {
            viewModeController.ColorReset();
            simulation.Next(playerOptions.stepParFrame, false);

            var result = simulation.GetResult();
            foreach (var p in result.Array)
            {
                viewModeController.ColorUpdate(p);
            }
        }

        /*
        if (viewTimeLine)
        {
            LastRecordUpdate(simulation.TimeLine);
        }
        */
    }

    private void SimulationInit()
    {
        foreach (var p in particles)
        {
            Destroy(p.gameObject);
        }

        particles.Clear();
        simulation.Init();
        var result = simulation.GetResult();

        viewModeController.ColorReset();
        for (int i = 0; i < result.Array.Length; i++)
        {
            var go = Instantiate(particlePrefab, transform);
            var ctrl = go.GetComponent<PLParticleController>();
            ctrl.Setup(result[i], viewModeController);
            particles.Add(ctrl);
            viewModeController.ColorUpdate(result[i]);
        }

        cameraController.Setup(particles);
        LastRecordUpdate(simulation.TimeLine);
    }

    private void LastRecordUpdate(ParticleLeniaTimeLine raw)
    {
        /*
        if (timeLine.TimeLine.Count < raw.TimeLine.Count)
        {
            timeLine.TimeLine.Clear();
            timeLine.TimeLine.AddRange(raw.TimeLine.Select(e => e.ToRecord(Serializer.Instance)));
        }
        */
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