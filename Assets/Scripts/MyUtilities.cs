using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyUtilities
{
    static public Color RandomColor(Color min, Color max)
    {
        Color result;
        result.r = Random.Range(min.r, max.r);
        result.g = Random.Range(min.g, max.g);
        result.b = Random.Range(min.b, max.b);
        result.a = Random.Range(min.a, max.a);
        return result;
    }
}
