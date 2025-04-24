using System;
using System.Numerics;

namespace SimpleVoiceChat;

public static class AudioProcessor
{
    public static float CalculateLogarithmicVolume(float distance, float maxRange)
    {
        float attenuation = 1f - MathF.Log(distance + 1) / MathF.Log(maxRange + 1);
        return Math.Clamp(attenuation, 0f, 1f);
    }

    public static byte[] ApplyEffects(byte[] audioData, float distance, float maxRange)
    {
        // Implementacja filtrów audio w zależności od odległości
        return audioData;
    }

    public static (float leftVolume, float rightVolume) CalculateStereoPan(Vector3 source, Vector3 listener, Vector3 listenerDirection)
    {
        Vector3 directionToSource = Vector3.Normalize(source - listener);
        float angle = Vector3.Dot(directionToSource, listenerDirection);
        float pan = (angle + 1f) / 2f; // Map from -1..1 to 0..1

        float left = 1f - pan;
        float right = pan;
        return (left, right);
    }
}