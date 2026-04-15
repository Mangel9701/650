using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class ItemDisplay : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private bool isVideoDisplay = true;
    [SerializeField] private bool oscilate = false;
    [SerializeField] public Vector3 eyeOffset = Vector3.zero;

    [Header("Video")]
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private VideoClip reverseVideoClip;
    [SerializeField] private Vector2 videoPosition = Vector2.zero;
    [SerializeField] private Vector3 videoScale = Vector3.one;

    [Header("Imagenes")]
    [SerializeField] private bool showSlideOnly = false;
    [SerializeField] private List<Sprite> images = new List<Sprite>();

    [Header("Eventos")]
    public UnityEvent onDisplayStart;
    public UnityEvent onDisplayEnd;

    private bool isUIOpen = false;

    public Vector3 EyeOffset => eyeOffset;

    private void OnEnable()
    {
        UIIngameManager.CustomClose += HandleUIClose;
    }

    private void OnDisable()
    {
        UIIngameManager.CustomClose -= HandleUIClose;
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

    private void ShowDisplayUI()
    {
        if (UIIngameManager.Instance == null)
        {
            Debug.LogWarning("[ItemDisplay] UIIngameManager.Instance es null.");
            return;
        }

        isUIOpen = true;
        onDisplayStart?.Invoke();

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
        else
        {
            if (images == null || images.Count == 0)
            {
                Debug.LogWarning($"[ItemDisplay:{name}] No hay imágenes asignadas.");
                isUIOpen = false;
                return;
            }

            UIIngameManager.Instance.ShowImagePanel(
                images,
                showSlideOnly
            );
        }
    }

    private void CloseDisplayUI()
    {
        if (UIIngameManager.Instance != null)
        {
            UIIngameManager.Instance.HideDisplayPanel();
        }

        isUIOpen = false;
        onDisplayEnd?.Invoke();
    }
}