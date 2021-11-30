
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AdvanceBoard : UdonSharpBehaviour
{
    public Chess4DEvaluator evaluator;


    public override void Interact()
    {
        evaluator.SearchForMove();
    }


    void Start()
    {
        
    }
}
