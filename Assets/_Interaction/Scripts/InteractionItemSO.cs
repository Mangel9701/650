using System.IO;
using UnityEngine;

public enum InteractionMediaType
{
    Image,
    Video
}

public enum InteractionMediaExtensionMode
{
    Png,
    Jpg,
    Mp4,
    Webm,
    Custom
}

[CreateAssetMenu(fileName = "InteractionItem", menuName = "Config/Interaction Item")]
public class InteractionItemSO : ScriptableObject
{
    [Header("Identificador")]
    public string id;

    [Header("Datos")]
    public string nombre;

    [TextArea(3, 6)]
    public string descripcion;

    [Header("Media remota")]
    public InteractionMediaType mediaType = InteractionMediaType.Image;

    public string remoteMediaName;

    public InteractionMediaExtensionMode extensionMode = InteractionMediaExtensionMode.Png;
    public string customExtension = "png";

    [Header("Imagen")]
    public bool showSlideOnly = false;

    [Header("Video")]
    public bool oscilate = false;
    public Vector2 videoPosition = Vector2.zero;
    public Vector3 videoScale = Vector3.one;

    [Header("Posiciones UI")]
    public Vector2 uiPosition;
    public Vector3 interactivePointPosition;

    public string GetSanitizedMediaBaseName()
    {
        if (string.IsNullOrWhiteSpace(remoteMediaName))
            return string.Empty;

        string value = remoteMediaName.Trim();
        value = value.Replace(" ", string.Empty);

        string extension = Path.GetExtension(value);
        if (!string.IsNullOrEmpty(extension))
            value = Path.GetFileNameWithoutExtension(value);

        value = value.Replace(" ", string.Empty);
        return value;
    }

    public string GetSelectedExtension()
    {
        switch (extensionMode)
        {
            case InteractionMediaExtensionMode.Png:
                return "png";

            case InteractionMediaExtensionMode.Jpg:
                return "jpg";

            case InteractionMediaExtensionMode.Mp4:
                return "mp4";

            case InteractionMediaExtensionMode.Webm:
                return "webm";

            case InteractionMediaExtensionMode.Custom:
                if (string.IsNullOrWhiteSpace(customExtension))
                    return string.Empty;

                string ext = customExtension.Trim().ToLowerInvariant();

                if (ext.StartsWith("."))
                    ext = ext.Substring(1);

                ext = ext.Replace(" ", string.Empty);
                return ext;

            default:
                return "png";
        }
    }

    public string GetFinalMediaFileName()
    {
        string baseName = GetSanitizedMediaBaseName();
        string ext = GetSelectedExtension();

        if (string.IsNullOrEmpty(baseName))
            return string.Empty;

        if (string.IsNullOrEmpty(ext))
            return baseName;

        return $"{baseName}.{ext}";
    }

    public string GetFullMediaUrl(string baseUrl)
    {
        string fileName = GetFinalMediaFileName();

        if (string.IsNullOrEmpty(fileName))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(baseUrl))
            return fileName;

        string normalizedBaseUrl = baseUrl.Trim();

        while (normalizedBaseUrl.EndsWith("/"))
            normalizedBaseUrl = normalizedBaseUrl.Substring(0, normalizedBaseUrl.Length - 1);

        return $"{normalizedBaseUrl}/{fileName}";
    }

    public void NormalizeMediaFields()
    {
        remoteMediaName = GetSanitizedMediaBaseName();

        if (extensionMode == InteractionMediaExtensionMode.Custom)
            customExtension = GetSelectedExtension();
    }

    public InteractionDto ToDto()
    {
        return new InteractionDto
        {
            id = id,
            nombre = nombre,
            descripcion = descripcion,

            mediaType = mediaType.ToString(),
            mediaName = GetSanitizedMediaBaseName(),
            extension = GetSelectedExtension(),

            showSlideOnly = showSlideOnly,

            oscilate = oscilate,
            videoPosition = new SerializableVector2(videoPosition),
            videoScale = new SerializableVector3(videoScale),

            uiPosition = new SerializableVector2(uiPosition),
            interactivePointPosition = new SerializableVector2(interactivePointPosition)
        };
    }

    private void OnValidate()
    {
        if (mediaType == InteractionMediaType.Image)
        {
            if (extensionMode == InteractionMediaExtensionMode.Mp4 ||
                extensionMode == InteractionMediaExtensionMode.Webm)
            {
                extensionMode = InteractionMediaExtensionMode.Png;
            }
        }

        if (mediaType == InteractionMediaType.Video)
        {
            if (extensionMode == InteractionMediaExtensionMode.Png ||
                extensionMode == InteractionMediaExtensionMode.Jpg)
            {
                extensionMode = InteractionMediaExtensionMode.Mp4;
            }
        }
    }
}