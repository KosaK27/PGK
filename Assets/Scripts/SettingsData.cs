using System;

[Serializable]
public class SettingsData
{
    public bool fullscreen = true;
    public int resolutionIndex = 0;
    public float musicVolume = 1f;
    public float gameSoundVolume = 1f;
    public float menuSoundVolume = 1f;
    public float cameraZoom = 5f;
}