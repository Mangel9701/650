using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class RuntimeInteractionConfigLoader : MonoBehaviour
{
    [Header("JSON remoto")]
    [SerializeField] private string remoteConfigUrl;

    [Header("Fallback local")]
    [SerializeField] private TextAsset localFallbackJson;

    public InteractionConfigDto CurrentDto { get; private set; }
    public RuntimeInteractionConfig CurrentConfig { get; private set; }

    public Action<RuntimeInteractionConfig> OnConfigLoaded;
    public Action<string> OnConfigLoadFailed;

    private IEnumerator Start()
    {
        yield return LoadConfigCoroutine();
    }

    public IEnumerator LoadConfigCoroutine()
    {
        bool loaded = false;

        if (!string.IsNullOrWhiteSpace(remoteConfigUrl))
        {
            using UnityWebRequest request = UnityWebRequest.Get(remoteConfigUrl);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                if (TryParseConfig(json, out InteractionConfigDto dto))
                {
                    SetConfig(dto);
                    loaded = true;
                }
                else
                {
                    Debug.LogError("[RuntimeInteractionConfigLoader] No se pudo leer el JSON remoto.");
                }
            }
            else
            {
                Debug.LogWarning("[RuntimeInteractionConfigLoader] Error cargando JSON remoto: " + request.error);
            }
        }

        if (!loaded && localFallbackJson != null)
        {
            if (TryParseConfig(localFallbackJson.text, out InteractionConfigDto fallbackDto))
            {
                SetConfig(fallbackDto);
                loaded = true;
            }
            else
            {
                Debug.LogError("[RuntimeInteractionConfigLoader] No se pudo leer el JSON local.");
            }
        }

        if (!loaded)
        {
            string error = "No se pudo cargar ninguna configuración.";
            Debug.LogError("[RuntimeInteractionConfigLoader] " + error);
            OnConfigLoadFailed?.Invoke(error);
        }
    }

    private void SetConfig(InteractionConfigDto dto)
    {
        CurrentDto = dto;
        CurrentConfig = RuntimeInteractionConfig.FromDto(dto);
        OnConfigLoaded?.Invoke(CurrentConfig);

        Debug.Log("[RuntimeInteractionConfigLoader] Config cargada correctamente.");
    }

    private bool TryParseConfig(string json, out InteractionConfigDto dto)
    {
        dto = null;

        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            dto = JsonUtility.FromJson<InteractionConfigDto>(json);
            return dto != null;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }
}