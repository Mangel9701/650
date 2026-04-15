using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Readme", menuName = "Tools/Readme Asset")]
public class ReadmeData : ScriptableObject
{
    public Texture2D icon;
    public Texture2D banner;
    public Color accentColor = new Color(0.20f, 0.60f, 1f);

    public List<IntroBlock> introBlocks = new List<IntroBlock>();
    public List<Section> sections = new List<Section>();

    [Serializable]
    public class IntroBlock
    {
        public string title;

        [TextArea(2, 6)]
        public string subtitle;

        public Texture2D image;
        public bool imageFullWidth = true;
    }

    [Serializable]
    public class Section
    {
        public string heading;

        [TextArea(3, 12)]
        public string text;

        public Texture2D image;
        public bool imageFullWidth = true;

        public string linkText;
        public string url;
    }
}