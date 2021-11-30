
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ResetBoard : UdonSharpBehaviour
{
    public Chess4DEvaluator evaluator;
    public Chess4DBoard board;


    public override void Interact()
    {
        if (!evaluator.IsEvaluatorBusy())
        {
            board.ResetBoard();
        }
    }

    void Start()
    {
        
    }
}
