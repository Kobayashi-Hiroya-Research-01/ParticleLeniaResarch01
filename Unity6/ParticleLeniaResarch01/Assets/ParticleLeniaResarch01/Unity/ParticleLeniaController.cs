using Assets.ParticleLeniaResarch01.Analysis;
using Assets.ParticleLeniaResarch01.Analyzers;
using Assets.ParticleLeniaResarch01.Scripts;
using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.Initializers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Unity.Profiling.ProfilerMarker;

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

        private void ClusterTrackingDbscan_OnClustered(ClusterTrackingPoint point, ClusterTrackingCluster<ClusterTrackingPoint> cluster, string clusteringName)
        {
            particles[point.Index].clusterInfo.SetCluster(clusteringName, cluster);
        }

        public void RemovePoints(IParticleLenia particleLenia, ReadOnlySpan<int> pointIndexs)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                foreach (var index in pointIndexs)
                {
                    if (p.particleIndex == index)
                    {
                        particles.RemoveAt(i);
                        Destroy(p.gameObject);
                    }
                }
            }

            for (int i = 0; i < particles.Count; i++)
            {
                var p = particles[i];
                p.particleIndex = i;
            }
        }
    }

    [Serializable]
    public class PlayerOptions
    {
        public bool isPlay;
        public int seedIncrement = 1;

        [Min(1)]
        public int stepParFrame = 1;

        public int autoStop = -1;
    }

    [Serializable]
    public class AutopoieticPotentialOptions
    {
        [Min(0)]
        public int analyzeFrequency = 120;

        [Min(1)]
        public int deltaFrame = 60;
    }

    [Serializable]
    public class DebugInfo
    {
        [Serializable]
        public class AutopoieticPotentialItem
        {
            public int clusterID = -1;
            public double autopoieticPotential;
        }

        public int currentStep;
        public double currentTime;

        public AutopoieticPotentialItem[] autopoieticPotentials = new AutopoieticPotentialItem[0];
    }

    public DebugInfo debugInfo;
    public bool calcAutopoieticSystemPotential;
    public PLViewModeController viewModeController;
    public PLCameraController cameraController;
    public UtHistController utHistController;
    public PLAnalyzerListController analyzerList;
    public GameObject particlePrefab;
    public PlayerOptions playerOptions;
    public AutopoieticPotentialOptions autopoieticPotentialOptions;
    public OriginalInitializer.State simulationOptions;

    private ParticlesAnalyzer particlesAnalyzer;
    private ParticleLeniaSimulation simulation;
    private ParticleLeniaAnalyzer analyzer;
    private AutopoieticSystemPotential autopoieticSystemPotential;
    private AutopoieticSystemPotentialArg autopoieticSystemPotentialArg;
    private int autopoieticSystemPotentialFreqIndex = 0;

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

        autopoieticSystemPotential ??= new AutopoieticSystemPotential();
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
            autopoieticSystemPotentialFreqIndex = 0;
        }

        if (playerOptions.isPlay || Input.GetKeyUp(KeyCode.N))
        {
            analyzer.AnalyzeAndNext(simulation, playerOptions.stepParFrame);
            CalcAutopoieticSystemPotential();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            analyzer.AnalyzeAndNext(simulation, 0);
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            Span<int> removes = stackalloc int[90];
            for (int i = 0; i < removes.Length; i++) removes[i] = i;
            analyzer.RemovePoints(simulation, removes);
        }
        var result = simulation.GetLastResult();

        if (playerOptions.autoStop > 0 && result.Step >= playerOptions.autoStop)
        {
            playerOptions.isPlay = false;
        }

        debugInfo.currentStep = result.Step;
        debugInfo.currentTime = result.Time;

        cameraController.UpdateCamera(analyzer);
    }

    private void CalcAutopoieticSystemPotential()
    {
        if (!calcAutopoieticSystemPotential) return;

        if (autopoieticSystemPotentialFreqIndex == 0)
        {
            if (autopoieticSystemPotentialArg == null)
            {
                var clusterTrackingDbscanAnalyzers = analyzer.SearchAll<ClusterTrackingDbscanAnalyzer>();
                autopoieticSystemPotentialArg = new()
                {
                    baseUnitAnalyzer = clusterTrackingDbscanAnalyzers.First(e => e.ClusteringName == "Unit"),
                    baseComponentAnalyzer = clusterTrackingDbscanAnalyzers.First(e => e.ClusteringName == "Component"),
                };
            }

            autopoieticSystemPotentialArg.baseSimulation = simulation;
            autopoieticSystemPotentialArg.step = autopoieticPotentialOptions.deltaFrame;

            var results = autopoieticSystemPotential.CheckPotential(autopoieticSystemPotentialArg);

            debugInfo ??= new();
            debugInfo.autopoieticPotentials = results
                .Select(e => new DebugInfo.AutopoieticPotentialItem() { clusterID = e.Key, autopoieticPotential = e.Value })
                .OrderBy(e => e.clusterID)
                .ToArray();
        }

        if (autopoieticSystemPotentialFreqIndex >= autopoieticPotentialOptions.analyzeFrequency - 1)
        {
            autopoieticSystemPotentialFreqIndex = 0;
        }
        else
        {
            autopoieticSystemPotentialFreqIndex++;
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