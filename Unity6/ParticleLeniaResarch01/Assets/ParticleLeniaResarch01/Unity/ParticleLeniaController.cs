using Assets.ParticleLeniaResarch01.Scripts.Data;
using Assets.ParticleLeniaResarch01.Scripts.Initializers;
using System.Collections.Generic;
using UnityEngine;

public class ParticleLeniaController : MonoBehaviour
{
    public PLColorModeController colorModeController;
    public PLCameraModeController cameraModeController;
    public GameObject particlePrefab;
    public bool isPlay;
    public int seedIncrement = 1;
    public bool updateRecord;
    public ParticleInfoArray.Record lastRecord;

    [Min(1)]
    public int step = 1;

    public OriginalInitializer.State options;

    private List<PLParticleController> particles;

    private ParticleLeniaSimulation simulation;

    private void OnEnable()
    {
        Debug.Log("testA");
        var oldParticles = GetComponentsInChildren<PLParticleController>();
        foreach (var p in oldParticles)
        {
            Destroy(p.gameObject);
        }

        simulation = new();
        particles = new();
        var initializer = new OriginalInitializer();
        initializer.SetState(options);
        simulation.Initializer = initializer;
        SimulationInit();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            isPlay = !isPlay;
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            options.seed += seedIncrement;
            SimulationInit();
        }

        if (isPlay)
        {
            colorModeController.ColorReset();
            simulation.Next(step);

            var result = simulation.GetResult();
            LastRecordUpdate(result);
            foreach (var p in result.Array)
            {
                colorModeController.ColorUpdate(p);
            }
            LastRecordUpdate(result);
        }
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

        colorModeController.ColorReset();
        for (int i = 0; i < result.Array.Length; i++)
        {
            var go = Instantiate(particlePrefab, transform);
            var ctrl = go.GetComponent<PLParticleController>();
            ctrl.Setup(result[i], colorModeController);
            particles.Add(ctrl);
            colorModeController.ColorUpdate(result[i]);
        }

        cameraModeController.Setup(particles);
        LastRecordUpdate(result);
    }

    private void LastRecordUpdate(ParticleInfoArray raw)
    {
        if (updateRecord)
        {
            lastRecord = raw.ToRecord(e => JsonUtility.ToJson(e));
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
        options ??= new();
        if (_oldAutoWk != options.autoWk)
        {
            if (options.autoWk)
            {
                _oldWk = options.fields.wk;
            }
            else
            {
                options.fields.wk = _oldWk;
            }
            _oldAutoWk = options.autoWk;
        }

        if (options.autoWk)
        {
            var k = options.fields.kernelGauss;
            options.fields.wk = OriginalInitializer.CalcW(k.mu, k.sigma, options.dimensions);
        }
    }
}