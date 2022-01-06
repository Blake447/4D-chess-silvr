
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BenchmarkerB : UdonSharpBehaviour
{
    public int value = 0;
    public int steps_per_frame = 10000;
    private void Update()
    {
        for (int i = 0; i < steps_per_frame; i++)
        {
            int a = Random.Range(0, 8192);
            int b = Random.Range(0, 8192);
            value = Mathf.Max(a, b);
        }
    }
}
