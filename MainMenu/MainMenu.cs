using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Elements")]
    public Button startButton;
    public Button continueButton;

    [Header("Scene Names")]
    public string cutsceneSceneName = "CutsceneScene";
    public string gameSceneName = "GameScene";

    [Header("Database References")]
    public JamuDatabase jamudatabase; 
    public NPCDatabase npcdatabase;

    [Header("UI Elements")]
    public GameObject mainMenuUI;
    public GameObject btnPlay;
    public GameObject blackPanel;

    [Header("Cutscene Elements")]
    public GameObject cutsceneObject;
    public VideoPlayer videoPlayer;
    public GameObject skipButton;
    public RawImage videoRawImage;

    [Header("Fade Settings")]
    public Animator fadeAnimator;
    public string fadeOutTrigger = "FadeOut";
    public string fadeInTrigger = "FadeIn";
    public float fadeOutTime = 1f;
    public float fadeInTime = 1f;
    public float delayBeforeFadeIn = 3f;

    [Header("Fade Out Settings")]
    public float videoFadeDuration = 3f;

    private bool isSkipping = false;
    private Coroutine videoFadeCoroutine = null;

    private void Awake()
    {
        if (jamudatabase == null)
            jamudatabase = Resources.Load<JamuDatabase>("eJamuuu/JamuDatabase"); // Pastikan ejaannya sesuai nama file di Resources

        if (npcdatabase == null)
            npcdatabase = Resources.Load<NPCDatabase>("eJamuuu/NPCDatabase");
    }

    private void Start()
    {
        CheckForSaveGame();

        if (startButton != null) startButton.onClick.AddListener(PlayWithCutscene);
        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);

        if (skipButton != null)
        {
            skipButton.SetActive(false);
            var btn = skipButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(SkipButtonPressed);
                btn.onClick.AddListener(SkipButtonPressed);
            }
        }

        if (cutsceneObject != null) cutsceneObject.SetActive(false);
        if (blackPanel != null) blackPanel.SetActive(false);
    }

    private void CheckForSaveGame()
    {
        // Sekarang lewat GameManager, bukan ManagerPP
        if (GameManager.instance != null && GameManager.instance.gameData != null)
            continueButton.interactable = true;
        else
            continueButton.interactable = false;
    }

    public void PlayWithCutscene()
    {
        StartCoroutine(StartGameSequence());
    }

    public void ContinueGame()
    {
        StartCoroutine(ContinueGameSequence());
    }

    private IEnumerator StartGameSequence()
    {
        if (GameManager.instance != null)
            GameManager.instance.CreateNewGameData();
        else
            Debug.LogWarning("GameManager tidak ditemukan!");

        // Reset cutscene status lewat GameManager
        if (GameManager.instance != null)
            GameManager.instance.ResetCutsceneStatus();

        // Fade Out
        if (fadeAnimator != null)
            fadeAnimator.SetTrigger(fadeOutTrigger);
        yield return new WaitForSeconds(fadeOutTime);

        if (mainMenuUI != null)
            mainMenuUI.SetActive(false);

        if (btnPlay != null)
            btnPlay.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false); // <--- TAMBAHKAN INI DI SINI

        if (blackPanel != null)
            blackPanel.SetActive(true);

        yield return new WaitForSeconds(delayBeforeFadeIn);

        if (blackPanel != null)
            blackPanel.SetActive(false);

        if (fadeAnimator != null)
            fadeAnimator.SetTrigger(fadeInTrigger);

        if (cutsceneObject != null)
            cutsceneObject.SetActive(true);

        if (skipButton != null)
            skipButton.SetActive(true);

        PlayCutscene();
    }

    private IEnumerator ContinueGameSequence()
    {
        // Tandai cutscene sudah ditonton lewat GameManager
        if (GameManager.instance != null)
            GameManager.instance.SetCutsceneWatched();

        if (fadeAnimator != null)
            fadeAnimator.SetTrigger(fadeOutTrigger);
        yield return new WaitForSeconds(fadeOutTime);

        if (mainMenuUI != null)
            mainMenuUI.SetActive(false);

        // Tambahan: pastikan kedua tombol juga nonaktif
        if (btnPlay != null)
            btnPlay.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        AsyncOperation ao = SceneManager.LoadSceneAsync(gameSceneName);
        while (!ao.isDone) yield return null;
    }

    private void PlayCutscene()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();

            if (videoFadeCoroutine != null) StopCoroutine(videoFadeCoroutine);
            videoFadeCoroutine = StartCoroutine(VideoAndAudioFadeOutBeforeVideoEnds());
        }
        else
        {
            Debug.LogWarning("VideoPlayer tidak di-assign di MainMenu!");
            FinishCutscene();
        }
    }

    private IEnumerator VideoAndAudioFadeOutBeforeVideoEnds()
    {
        double fadeStartTime = videoPlayer.length - videoFadeDuration;
        while (videoPlayer.time < fadeStartTime)
        {
            yield return null;
        }

        if (!isSkipping)
        {
            yield return StartCoroutine(FadeOutVideoAndAudio());
        }
    }

    private void FinishCutscene()
    {
        // Simpan cutscene sudah ditonton lewat GameManager
        if (GameManager.instance != null)
            GameManager.instance.SetCutsceneWatched();

        if (cutsceneObject != null)
            cutsceneObject.SetActive(false);

        if (skipButton != null)
            skipButton.SetActive(false);

        SceneManager.LoadScene(gameSceneName);
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (isSkipping) return;
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;

        StartCoroutine(FinishCutsceneSequence());
    }

    public void SkipButtonPressed()
    {
        if (isSkipping) return;
        isSkipping = true;

        if (skipButton != null)
            skipButton.SetActive(false);

        if (videoFadeCoroutine != null) StopCoroutine(videoFadeCoroutine);

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            StartCoroutine(FadeOutVideoAndAudioAndPanelAndFinishCutscene());
        }
        else
        {
            StartCoroutine(FinishCutsceneSequence());
        }
    }

    private IEnumerator FadeOutVideoAndAudioAndPanelAndFinishCutscene()
    {
        yield return StartCoroutine(FadeOutVideoAndAudio());

        if (blackPanel != null)
            blackPanel.SetActive(true);

        if (fadeAnimator != null)
            fadeAnimator.SetTrigger(fadeOutTrigger);

        if (fadeAnimator != null)
            yield return new WaitForSeconds(fadeOutTime);

        // Simpan cutscene sudah ditonton lewat GameManager
        if (GameManager.instance != null)
            GameManager.instance.SetCutsceneWatched();

        if (cutsceneObject != null)
            cutsceneObject.SetActive(false);

        AsyncOperation ao = SceneManager.LoadSceneAsync(gameSceneName);
        while (!ao.isDone) yield return null;
    }

    private IEnumerator FadeOutVideoAndAudio()
    {
        float startAlpha = 1f;
        float elapsed = 0f;
        if (videoRawImage != null)
            startAlpha = videoRawImage.color.a;
        ushort track = 0;
        float currentVolume = videoPlayer.GetDirectAudioVolume(track);

        while (elapsed < videoFadeDuration)
        {
            float t = elapsed / videoFadeDuration;
            float newAlpha = Mathf.Lerp(startAlpha, 0f, t);
            float newVolume = Mathf.Lerp(currentVolume, 0f, t);

            if (videoRawImage != null)
            {
                Color c = videoRawImage.color;
                c.a = newAlpha;
                videoRawImage.color = c;
            }
            else if (videoPlayer.renderMode == VideoRenderMode.CameraFarPlane ||
                    videoPlayer.renderMode == VideoRenderMode.CameraNearPlane)
            {
                videoPlayer.targetCameraAlpha = newAlpha;
            }

            videoPlayer.SetDirectAudioVolume(track, newVolume);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (videoRawImage != null)
        {
            Color c = videoRawImage.color;
            c.a = 0f;
            videoRawImage.color = c;
        }
        else if (videoPlayer.renderMode == VideoRenderMode.CameraFarPlane ||
                 videoPlayer.renderMode == VideoRenderMode.CameraNearPlane)
        {
            videoPlayer.targetCameraAlpha = 0f;
        }
        videoPlayer.SetDirectAudioVolume(track, 0f);
    }

    private IEnumerator FinishCutsceneSequence()
    {
        if (GameManager.instance != null)
            GameManager.instance.SetCutsceneWatched();

        if (cutsceneObject != null)
            cutsceneObject.SetActive(false);

        if (skipButton != null)
            skipButton.SetActive(false);

        AsyncOperation ao = SceneManager.LoadSceneAsync(gameSceneName);
        while (!ao.isDone) yield return null;
    }

    // Tidak perlu lagi method ResetCutsceneStatus/SetCutsceneWatched di MainMenu, cukup lewat GameManager!
}