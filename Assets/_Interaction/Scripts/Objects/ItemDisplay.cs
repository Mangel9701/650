using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Video;

public class ItemDisplay : MonoBehaviour
{
    [Header("Modo local")]
    [SerializeField] private bool useRuntimeData = false;

    [Header("Display local")]
    [SerializeField] private bool isVideoDisplay = true;
    [SerializeField] private bool oscilate = false;

    [Header("Video local")]
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private VideoClip reverseVideoClip;
    [SerializeField] private Vector2 videoPosition = Vector2.zero;
    [SerializeField] private Vector3 videoScale = Vector3.one;

    [Header("Eventos propios del display")]
    public UnityEvent onDisplayStart;
    public UnityEvent onDisplayEnd;

    private RuntimeInteractionItem runtimeItem;
    private bool isUIOpen = false;
    private Coroutine imageLoadCoroutine;

    private void OnEnable()
    {
        UIIngameManager.CustomClose += HandleUIClose;
    }

    private void OnDisable()
    {
        UIIngameManager.CustomClose -= HandleUIClose;
    }

    public void SetRuntimeItem(RuntimeInteractionItem item)
    {
        runtimeItem = item;
        isVideoDisplay = item.mediaType == InteractionMediaType.Video;
       
        oscilate = item.oscilate;
        videoPosition = item.uiPosition;
        videoScale = item.videoScale;


        useRuntimeData = item != null;
    }

    private void HandleUIClose()
    {
        if (!isUIOpen)
            return;

        CloseDisplayUI();
    }

    public void OnInteract()
    {
        if (!isUIOpen)
            ShowDisplayUI();
        else
            CloseDisplayUI();
    }

    public void ShowDisplayUI()
    {
        if (UIIngameManager.Instance == null)
        {
            Debug.LogWarning("[ItemDisplay] UIIngameManager.Instance es null.");
            return;
        }

        Debug.Log($"[ItemDisplay] Mostrando display para {name} (Runtime: {useRuntimeData})");

        isUIOpen = true;
        onDisplayStart?.Invoke();

        if (useRuntimeData && runtimeItem != null)
        {
            ShowRuntimeDisplay();
        }
        else
        {
            ShowLocalDisplay();
        }
    }

    private void ShowRuntimeDisplay()
    {
        UIIngameManager.Instance.ShowItemPanel(runtimeItem.nombre, runtimeItem.descripcion);

        if (runtimeItem.mediaType == InteractionMediaType.Video)
        {
            UIIngameManager.Instance.ShowVideoPanelFromUrl(
                runtimeItem.fullMediaUrl,
                runtimeItem.oscilate,
                runtimeItem.videoPosition,
                runtimeItem.videoScale
            );
        }
        else
        {
            if (imageLoadCoroutine != null)
                StopCoroutine(imageLoadCoroutine);

            imageLoadCoroutine = StartCoroutine(
                LoadImageFromUrl(runtimeItem.fullMediaUrl, runtimeItem.showSlideOnly)
            );
        }
    }

    private void ShowLocalDisplay()
    {
        if (isVideoDisplay)
        {
            if (videoClip == null)
            {
                Debug.LogWarning($"[ItemDisplay:{name}] No hay VideoClip asignado.");
                isUIOpen = false;
                return;
            }

            UIIngameManager.Instance.ShowVideoPanel(
                videoClip,
                oscilate,
                reverseVideoClip,
                videoPosition,
                videoScale
            );
        }
    }

    private IEnumerator LoadImageFromUrl(string url, bool slideOnly)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning("[ItemDisplay] URL de imagen vacía.");
            yield break;
        }

        UIIngameManager.Instance.HideImageDisplayPublic();
        UIIngameManager.Instance.ShowObjLoader(true);

        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            UIIngameManager.Instance.ShowObjLoader(false);
            Debug.LogError("[ItemDisplay] Error cargando imagen: " + request.error);
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        UIIngameManager.Instance.ShowObjLoader(false);
        UIIngameManager.Instance.ShowImagePanel(new List<Sprite> { sprite }, slideOnly);
    }

    public void CloseDisplayUI()
    {
        if (imageLoadCoroutine != null)
        {
            StopCoroutine(imageLoadCoroutine);
            imageLoadCoroutine = null;
        }

        if (UIIngameManager.Instance != null)
        {
            UIIngameManager.Instance.HideDisplayPanel();
        }

        isUIOpen = false;
        onDisplayEnd?.Invoke();
    }
}