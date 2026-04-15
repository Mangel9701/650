using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

public class UIIngameManager : MonoBehaviour
{
    public UIManager uiManager;

    [Header("Mensajes interacciones")]
    public GameObject interactionText;
    public GameObject doorInteractionText;

    [SerializeField] private bool cursorVisible;

    public static UIIngameManager Instance { get; private set; }

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    [SerializeField] private float fadeDuration = 0.4f;

    [Header("Display Panel")]
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header("Video")]
    [SerializeField] private RawImage itemVideoPlayer;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private CanvasGroup itemVideoCanvasGroup;
    [SerializeField] private Material OriginalMat;

    [Header("Loading Spinners")]
    [SerializeField] private GameObject objLoader;

    public static UnityAction CustomClose;

    private bool OscilationMode = false;
    private bool _isFirstVideoPlay = true;
    private bool _showLoaderThisCycle = false;
    private bool _playForwardNext = true;
    private bool _waitingFirstFrame = false;

    private VideoClip _videoClipForward;
    private VideoClip _videoClipReverse;

    private List<Sprite> _currentImages = new List<Sprite>();
    private int _currentImageIndex = 0;
    private bool _showSlideOnly = false;

    private InputSystem_Actions inputActions;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        inputActions = new InputSystem_Actions();

    }

    private void Start()
    {
        if (uiManager == null)
        {
            GameObject uiscript = GameObject.FindGameObjectWithTag("UIManager");
            if (uiscript != null)
            {
                uiManager = uiscript.GetComponent<UIManager>();
            }
            else
            {
                Debug.LogWarning("No se asignó un UIManager en UIIngameManager");
            }
        }

        if (itemPanel != null)
        {
            canvasGroup = itemPanel.GetComponent<CanvasGroup>();
        }

        HideVisualContentImmediate();
    }

    public void ResetMaterial()
    {
        if (itemVideoPlayer != null && OriginalMat != null)
        {
            itemVideoPlayer.material = OriginalMat;
        }
    }

    public void ShowObjLoader(bool show)
    {
        if (objLoader != null)
            objLoader.SetActive(show);
    }

    public void ShowInteractPrompt(bool isDoor)
    {
        if (isDoor)
        {
            if (doorInteractionText != null)
                doorInteractionText.SetActive(true);
        }
        else
        {
            if (interactionText != null)
                interactionText.SetActive(true);
        }
    }

    public void HideInteractPrompt(bool isDoor)
    {
        if (isDoor)
        {
            if (doorInteractionText != null)
                doorInteractionText.SetActive(false);
        }
        else
        {
            if (interactionText != null)
                interactionText.SetActive(false);
        }
    }

    public void ShowItemPanel(string name, string description)
    {
        OpenDisplayPanel();

        if (itemNameText != null)
            itemNameText.text = name;

        if (itemDescriptionText != null)
            itemDescriptionText.text = description;
    }

    public void HideItemPanel()
    {
        HideDisplayPanel();
    }

    private void OpenDisplayPanel()
    {
        if (uiManager != null)
            uiManager.showCursor();

        if (itemPanel != null && canvasGroup == null)
            canvasGroup = itemPanel.GetComponent<CanvasGroup>();

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (canvasGroup != null && itemPanel != null)
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, true, itemPanel));
        else if (itemPanel != null)
            itemPanel.SetActive(true);
    }

    private void CloseDisplayPanel()
    {
        if (uiManager != null)
            uiManager.hideCursor();

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (canvasGroup != null && itemPanel != null)
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, false, itemPanel));
        else if (itemPanel != null)
            itemPanel.SetActive(false);
    }

    public void ShowVideoPanel(
        VideoClip videoClip,
        bool isOscilating,
        VideoClip reverseClip = null,
        Vector2 videoPosition = default,
        Vector3 videoScale = default
    )
    {
        if (videoClip == null)
        {
            Debug.LogWarning("[UIIngameManager] No se asignó VideoClip.");
            return;
        }

        if (videoPlayer == null || itemVideoPlayer == null)
        {
            Debug.LogWarning("[UIIngameManager] Faltan referencias de VideoPlayer o RawImage.");
            return;
        }

        OpenDisplayPanel();

        OscilationMode = isOscilating;
        _videoClipForward = videoClip;
        _videoClipReverse = reverseClip;
        _playForwardNext = true;
        _isFirstVideoPlay = true;

        ShowObjLoader(true);
        itemVideoPlayer.gameObject.SetActive(true);

        if (itemVideoCanvasGroup != null)
        {
            itemVideoCanvasGroup.alpha = 0f;
            itemVideoCanvasGroup.interactable = false;
            itemVideoCanvasGroup.blocksRaycasts = false;
        }

        itemVideoPlayer.enabled = false;
        itemVideoPlayer.color = Color.white;

        ApplyVideoTransform(videoPosition, videoScale);

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.frameReady -= OnVideoFrameReady;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.loopPointReached -= OnVideoFinished;

        videoPlayer.waitForFirstFrame = true;
        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.playOnAwake = false;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.frameReady += OnVideoFrameReady;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.loopPointReached += OnVideoFinished;

        var rtExistente = itemVideoPlayer.texture as RenderTexture;
        UseExistingRenderTextureForVideo(rtExistente);

        PrepareAndPlayCurrent();
    }

    public void ShowImagePanel(List<Sprite> images, bool showSlideOnly)
    {
        if (images == null || images.Count == 0)
        {
            Debug.LogWarning("[UIIngameManager] No hay imágenes para mostrar.");
            return;
        }

        OpenDisplayPanel();
        HideVideoDisplay();

        _currentImages = new List<Sprite>(images);
        _currentImageIndex = 0;
        _showSlideOnly = showSlideOnly;

    }    


    private void ApplyVideoTransform(Vector2 videoPosition, Vector3 videoScale)
    {
        RectTransform rt = itemVideoPlayer.rectTransform;
        rt.anchoredPosition = videoPosition;

        if (videoScale == Vector3.zero)
            videoScale = Vector3.one;

        rt.localScale = videoScale;
    }

    private void PrepareAndPlayCurrent()
    {
        _waitingFirstFrame = true;
        _showLoaderThisCycle = _isFirstVideoPlay;
        _isFirstVideoPlay = false;

        if (_showLoaderThisCycle)
        {
            if (itemVideoCanvasGroup != null)
                itemVideoCanvasGroup.alpha = 0f;

            itemVideoPlayer.enabled = false;
            ShowObjLoader(true);
        }

        VideoClip nextClip = (_playForwardNext || _videoClipReverse == null)
            ? _videoClipForward
            : _videoClipReverse;

        if (nextClip == null)
        {
            Debug.LogWarning("[UIIngameManager] No hay clip para reproducir.");
            ShowObjLoader(false);
            return;
        }

        videoPlayer.clip = nextClip;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.Play();
    }

    private void OnVideoFrameReady(VideoPlayer vp, long frameIdx)
    {
        if (_waitingFirstFrame)
        {
            _waitingFirstFrame = false;

            itemVideoPlayer.enabled = true;

            if (itemVideoCanvasGroup != null)
                itemVideoCanvasGroup.alpha = 1f;

            if (_showLoaderThisCycle)
                ShowObjLoader(false);
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        if (OscilationMode && _videoClipReverse != null)
        {
            _playForwardNext = !_playForwardNext;
            PrepareAndPlayCurrent();
        }
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("Video error: " + message);
        ShowObjLoader(false);
    }

    private void UseExistingRenderTextureForVideo(RenderTexture rt)
    {
        if (rt == null)
        {
            Debug.LogError("No hay RenderTexture asignada para el video.");
            return;
        }

        bool needRelease = false;

        if (rt.antiAliasing != 1)
        {
            rt.antiAliasing = 1;
            needRelease = true;
        }

#if UNITY_2020_2_OR_NEWER
        if (rt.bindTextureMS)
        {
            rt.bindTextureMS = false;
            needRelease = true;
        }
#endif

        if (needRelease)
        {
            if (rt.IsCreated())
                rt.Release();

            rt.Create();
        }

        itemVideoPlayer.texture = rt;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = rt;
    }

    private void HideVideoDisplay()
    {
        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying)
                videoPlayer.Stop();

            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.frameReady -= OnVideoFrameReady;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.loopPointReached -= OnVideoFinished;

            videoPlayer.clip = null;
        }

        if (itemVideoPlayer != null)
        {
            itemVideoPlayer.enabled = false;
            itemVideoPlayer.gameObject.SetActive(false);
        }

        if (itemVideoCanvasGroup != null)
            itemVideoCanvasGroup.alpha = 0f;

        _videoClipForward = null;
        _videoClipReverse = null;
    }

   
    private void HideVisualContentImmediate()
    {
        HideVideoDisplay();
        ShowObjLoader(false);
    }

    public void HideDisplayPanel()
    {
        HideVisualContentImmediate();
        CloseDisplayPanel();
    }

    public void SetItemImage(Image container, Sprite sprite, Vector3 scale)
    {
        if (container != null)
        {
            container.sprite = sprite;
            container.enabled = (sprite != null);
            container.preserveAspect = true;
            container.transform.localScale = scale;
        }
    }

    public void CloseCurrentPanel()
    {
        bool isAnyPanelOpen = (itemPanel != null && itemPanel.activeSelf);

        if (!isAnyPanelOpen)
            return;

        CustomClose?.Invoke();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        bool isAnyPanelOpen = (itemPanel != null && itemPanel.activeSelf);

        if (isAnyPanelOpen)
        {
            CloseCurrentPanel();
        }
    }

   
 

    private IEnumerator FadeCanvasGroup(
        CanvasGroup group,
        float startAlpha,
        float endAlpha,
        bool activate,
        GameObject panelObject
    )
    {
        if (activate)
            panelObject.SetActive(true);

        float elapsed = 0f;
        group.alpha = startAlpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            yield return null;
        }

        group.alpha = endAlpha;

        if (!activate)
            panelObject.SetActive(false);
    }

    public void changeMainMenuStatus()
    {
        cursorVisible = !cursorVisible;
    }
}