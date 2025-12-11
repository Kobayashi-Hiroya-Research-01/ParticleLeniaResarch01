using Assets.ParticleLeniaResarch01.Analysis;
using Assets.ParticleLeniaResarch01.Analyzers;
using Assets.ParticleLeniaResarch01.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.Initializers;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ParticleLeniaController : MonoBehaviour
{
    private class ParticlesAnalyzer : IParticleLeniaAnalyzer
    {
        public readonly ParticleLeniaController particleLeniaController;
        public readonly List<PLParticleController> particles;
        private readonly ParticleLeniaAnalyzerElement[] elements;

        public ParticlesAnalyzer(ParticleLeniaController particleLeniaController)
        {
            this.particleLeniaController = particleLeniaController;
            particles = new();

            elements = new ParticleLeniaAnalyzerElement[]
            {
                new(timing: ParticleLeniaAnalyzerTiming.ForeachCreated, loopStart: StartCreate, @foreach: ForeachCreated)
            };
        }

        public bool Enabled { get; set; } = true;

        public ReadOnlySpan<ParticleLeniaAnalyzerElement> GetAnalyzerElements() => elements;

        public void Reset()
        {
            particles.Clear();
            particleLeniaController.DestroyParticles();
        }

        public void SetupDependencies(ParticleLeniaAnalyzer dependencies, IParticleLenia particleLenia)
        {
            var clusterTrackingDbscans = dependencies.SearchAll<ClusterTrackingDbscanAnalyzer>();

            foreach (var clusterTrackingDbscan in clusterTrackingDbscans)
            {
                clusterTrackingDbscan.OnClustered -= ClusterTrackingDbscan_OnClustered;
                clusterTrackingDbscan.OnClustered += ClusterTrackingDbscan_OnClustered;
            }
        }

        private void StartCreate(ParticleInfoArray particles)
        {
            this.particles.Capacity = particleLeniaController.simulationOptions.particleCount;
        }

        private void ForeachCreated(ParticleInfo info, int index)
        {
            var p = particleLeniaController.CreateParticle(info, index);
            particles.Add(p);
        }

        private void ClusterTrackingDbscan_OnClustered(ClusterTrackingDbscanAnalyzer.Point point, ClusterTrackingCluster<ClusterTrackingDbscanAnalyzer.Point> cluster, string clusteringName)
        {
            particles[point.Index].clusterInfo.SetCluster(clusteringName, cluster);
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
    public UtHistController utHistController;
    public PLAnalyzerListController analyzerList;
    public GameObject particlePrefab;
    public PlayerOptions playerOptions;
    public OriginalInitializer.State simulationOptions;

    private ParticlesAnalyzer particlesAnalyzer;

    private ParticleLeniaSimulation simulation;

    private ParticleLeniaAnalyzer analyzer;

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

        if (utHistController == null)
        {
            utHistController = FindFirstObjectByType<UtHistController>();
        }

        if (analyzerList == null)
        {
            analyzerList = FindFirstObjectByType<PLAnalyzerListController>();
        }
    }

    private void OnEnable()
    {
        Awake();
        DestroyParticles();

        simulation = new();
        analyzer = new();
        particlesAnalyzer = new(this);

        var initializer = new OriginalInitializer();
        initializer.SetState(simulationOptions);
        simulation.Initializer = initializer;

        analyzer.AddRange(analyzerList.ParticleLeniaAnalyzers);
        analyzer.Add(particlesAnalyzer);

        analyzer.AnalyzeAndInit(simulation);
    }

    private PLParticleController CreateParticle(ParticleInfo info, int index)
    {
        var go = Instantiate(particlePrefab, transform);
        var component = go.GetComponent<PLParticleController>();
        component.InitPostAction(info, viewModeController, analyzer, index);

        return component;
    }

    private void DestroyParticles()
    {
        var oldParticles = GetComponentsInChildren<PLParticleController>();
        foreach (var p in oldParticles)
        {
            Destroy(p.gameObject);
        }
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
            analyzer.AnalyzeAndInit(simulation);
            cameraController.UpdateCamera(analyzer);
        }

        if (playerOptions.isPlay || Input.GetKeyUp(KeyCode.N))
        {
            analyzer.AnalyzeAndNext(simulation, playerOptions.stepParFrame);
            cameraController.UpdateCamera(analyzer);
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