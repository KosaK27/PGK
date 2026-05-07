using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    [SerializeField] private WorldGenerator _worldGenerator;
    [SerializeField] private string _gameSceneName = "Game";

    [Header("Generation UI")]
    [SerializeField] private GameObject _generationPanel;
    [SerializeField] private TextMeshProUGUI _stepLabel;
    [SerializeField] private TextMeshProUGUI _timerLabel;
    [SerializeField] private Slider _generationProgressBar;

    [Header("Loading UI")]
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private TextMeshProUGUI _loadingStepLabel;
    [SerializeField] private Slider _loadingProgressBar;

    private Stopwatch _stopwatch = new();

    void Start()
    {
        if (LoadingContext.IsNewWorld)
        {
            _generationPanel.SetActive(true);
            _loadingPanel.SetActive(false);
            StartCoroutine(GenerateWorld());
        }
        else
        {
            _generationPanel.SetActive(false);
            _loadingPanel.SetActive(true);
            StartCoroutine(LoadWorld());
        }
    }

    void Update()
    {
        if (_stopwatch.IsRunning)
            _timerLabel.text = FormatTime(_stopwatch.Elapsed.TotalMilliseconds);
    }

    IEnumerator GenerateWorld()
    {
        _stopwatch.Start();

        var sm = SaveManager.Instance;
        var worldSave = sm.SelectedWorld;
        var data = new WorldData(worldSave.width, worldSave.height);

        _generationProgressBar.value = 0f;
        _stepLabel.text = "Starting...";

        yield return null;

        _worldGenerator.randomSeed = false;
        _worldGenerator.seed = worldSave.seed;

        yield return StartCoroutine(_worldGenerator.GenerateCoroutine(data, (progress, step) =>
        {
            _generationProgressBar.value = progress;
            _stepLabel.text = step;
        }));

        _generationProgressBar.value = 1f;
        _stepLabel.text = "Done.";
        _stopwatch.Stop();

        WorldDataTransfer.Data = data;
        WorldDataTransfer.OriginalData = data.Clone();

        yield return new WaitForSeconds(0.5f);

        var asyncLoad = SceneManager.LoadSceneAsync(_gameSceneName);
        asyncLoad.allowSceneActivation = false;
        while (asyncLoad.progress < 0.9f) yield return null;
        asyncLoad.allowSceneActivation = true;
    }

    IEnumerator LoadWorld()
    {
        var sm = SaveManager.Instance;
        var worldSave = sm.SelectedWorld;

        _loadingProgressBar.value = 0f;
        if (_loadingStepLabel != null) _loadingStepLabel.text = "Generating world...";

        yield return null;

        _worldGenerator.randomSeed = false;
        _worldGenerator.seed = worldSave.seed;

        var data = new WorldData(worldSave.width, worldSave.height);

        yield return StartCoroutine(_worldGenerator.GenerateCoroutine(data, (progress, step) =>
        {
            _loadingProgressBar.value = progress * 0.8f;
            if (_loadingStepLabel != null) _loadingStepLabel.text = step;
        }));

        WorldDataTransfer.OriginalData = data.Clone();

        if (_loadingStepLabel != null) _loadingStepLabel.text = "Loading world...";
        _loadingProgressBar.value = 0.8f;

        yield return null;

        var asyncLoad = SceneManager.LoadSceneAsync(_gameSceneName);
        asyncLoad.allowSceneActivation = false;

        float elapsed = 0f;
        while (asyncLoad.progress < 0.9f || elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            _loadingProgressBar.value = 0.8f + Mathf.Min(asyncLoad.progress / 0.9f, elapsed / 2f) * 0.2f;
            yield return null;
        }

        _loadingProgressBar.value = 1f;
        if (_loadingStepLabel != null) _loadingStepLabel.text = "Done.";
        yield return new WaitForSeconds(0.3f);

        WorldDataTransfer.Data = data;
        asyncLoad.allowSceneActivation = true;
    }

    private string FormatTime(double totalMs)
    {
        int minutes = (int)(totalMs / 60000);
        int seconds = (int)(totalMs % 60000 / 1000);
        int ms = (int)(totalMs % 1000);
        return $"{minutes:D2}:{seconds:D2}:{ms:D3}";
    }
}