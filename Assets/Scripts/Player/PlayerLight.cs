using UnityEngine;

public class PlayerLight : MonoBehaviour
{
    [Min(0f)] public float strength = 0.6f;
    [Min(0f)] public float radius = 8f;
    public Color lightColor = Color.white;

    /*void Start()
    {
        var ls = gameObject.AddComponent<LightSource>();
        ls.LightColor = lightColor;
        ls.Strength = strength;
        ls.radius = radius;
        ls.isDynamic = true;
        ls.useSimpleRadial = true;
    }*/
}