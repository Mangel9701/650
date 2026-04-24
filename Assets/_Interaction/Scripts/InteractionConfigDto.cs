using System;
using UnityEngine;

[Serializable]
public class InteractionConfigDto
{
    public string mediaBaseUrl;
    public InteractionDto[] interactions;
}

[Serializable]
public class InteractionDto
{
    public string id;
    public string nombre;
    public string descripcion;

    public string mediaType;
    public string mediaName;
    public string extension;

    public bool showSlideOnly;

    public bool oscilate;
    public SerializableVector2 videoPosition;
    public SerializableVector3 videoScale;

    public SerializableVector2 uiPosition;
    public SerializableVector2 interactivePointPosition;
}

[Serializable]
public class SerializableVector2
{
    public float x;
    public float y;

    public SerializableVector2() { }

    public SerializableVector2(Vector2 value)
    {
        x = value.x;
        y = value.y;
    }

    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
}

[Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3() { }

    public SerializableVector3(Vector3 value)
    {
        x = value.x;
        y = value.y;
        z = value.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}