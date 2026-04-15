using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class DeviceDetector : MonoBehaviour
{
    public static DeviceDetector Instance { get; private set; }

    public bool IsMobile { get; private set; }

    [SerializeField] private bool detectedMobile;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern IntPtr DS_GetDeviceString();

    [DllImport("__Internal")]
    private static extern int DS_IsTouchDevice(); 
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsMobile = DetectMobile();
        detectedMobile = IsMobile;
    }

    private bool DetectMobile()
    {
        // Android/iOS nativo
        if (Application.isMobilePlatform)
            return true;

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string device = "";

            IntPtr ptr = DS_GetDeviceString();
            if (ptr != IntPtr.Zero)
            {
#if UNITY_2021_2_OR_NEWER
                device = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(ptr);
#else
                device = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
#endif
            }

            device = (device ?? "").ToLowerInvariant();

            if (device.Contains("ios") || device.Contains("iphone") || device.Contains("ipad"))
                return true;

            if (device.Contains("android"))
                return true;

            if (device.Contains("mobile"))
                return true;

            if (device.Contains("tablet"))
                return true;
        }
        catch
        {
            // si falla, asumimos desktop
        }
#endif

        return false;
    }
}
