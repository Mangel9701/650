using System.Collections.Generic;
using UnityEngine;

public class RuntimeInteractionConfig
{
    public string mediaBaseUrl;
    public List<RuntimeInteractionItem> interactions = new List<RuntimeInteractionItem>();

    public static RuntimeInteractionConfig FromDto(InteractionConfigDto dto)
    {
        var config = new RuntimeInteractionConfig();

        if (dto == null)
            return config;

        config.mediaBaseUrl = dto.mediaBaseUrl;

        if (dto.interactions != null)
        {
            foreach (var interactionDto in dto.interactions)
            {
                if (interactionDto == null)
                    continue;

                config.interactions.Add(RuntimeInteractionItem.FromDto(interactionDto, dto.mediaBaseUrl));
            }
        }

        return config;
    }
}

public class RuntimeInteractionItem
{

    public string id;
    public string nombre;
    public string descripcion;

    public InteractionMediaType mediaType;
    public string mediaName;
    public string extension;
    public string fullMediaUrl;

    public bool showSlideOnly;

    public bool oscilate;
    public Vector2 videoPosition;
    public Vector3 videoScale;

    public Vector2 uiPosition;
    public Vector2 interactivePointPosition;

    public static RuntimeInteractionItem FromDto(InteractionDto dto, string baseUrl)
    {
        InteractionMediaType parsedType = InteractionMediaType.Image;

        if (!string.IsNullOrWhiteSpace(dto.mediaType))
            System.Enum.TryParse(dto.mediaType, true, out parsedType);

        string normalizedBase = string.IsNullOrWhiteSpace(baseUrl)
            ? string.Empty
            : baseUrl.TrimEnd('/');

        string fileName = string.IsNullOrWhiteSpace(dto.extension)
            ? dto.mediaName
            : $"{dto.mediaName}.{dto.extension}";

        string fullUrl = string.IsNullOrWhiteSpace(normalizedBase)
            ? fileName
            : $"{normalizedBase}/{fileName}";

        return new RuntimeInteractionItem
        {
            id = dto.id,
            nombre = dto.nombre,
            descripcion = dto.descripcion,

            mediaType = parsedType,
            mediaName = dto.mediaName,
            extension = dto.extension,
            fullMediaUrl = fullUrl,

            showSlideOnly = dto.showSlideOnly,

            oscilate = dto.oscilate,
            videoPosition = dto.videoPosition != null ? dto.videoPosition.ToVector2() : Vector2.zero,
            videoScale = dto.videoScale != null ? dto.videoScale.ToVector3() : Vector3.one,

            uiPosition = dto.uiPosition != null ? dto.uiPosition.ToVector2() : Vector2.zero,
            interactivePointPosition = dto.interactivePointPosition != null
                ? dto.interactivePointPosition.ToVector2()
                : Vector2.zero
        };
    }
}