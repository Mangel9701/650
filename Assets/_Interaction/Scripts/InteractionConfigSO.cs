using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractionConfig", menuName = "Config/Interaction Database")]
public class InteractionConfigSO : ScriptableObject
{
    public string mediaBaseUrl = "https://tuenlace.com/MediaResources/";
    public List<InteractionItemSO> interactions = new List<InteractionItemSO>();

    public string GetNormalizedBaseUrl()
    {
        if (string.IsNullOrWhiteSpace(mediaBaseUrl))
            return string.Empty;

        string value = mediaBaseUrl.Trim();

        while (value.EndsWith("/"))
            value = value.Substring(0, value.Length - 1);

        return value + "/";
    }

    public InteractionConfigDto ToDto()
    {
        InteractionDto[] items = new InteractionDto[interactions.Count];

        for (int i = 0; i < interactions.Count; i++)
        {
            if (interactions[i] != null)
                items[i] = interactions[i].ToDto();
        }

        return new InteractionConfigDto
        {
            mediaBaseUrl = GetNormalizedBaseUrl(),
            interactions = items
        };
    }
}