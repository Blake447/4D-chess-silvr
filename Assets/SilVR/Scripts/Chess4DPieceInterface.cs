
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Chess4DPieceInterface : UdonSharpBehaviour
{
    public Chess4DBoard board;
    public bool isInitialized; 
   
    public void InitializeInterface(Chess4DBoard init_board)
    {
        board = init_board;
        isInitialized = true;
    }


    public override void Interact()
    {
        board.OnInterfaceInteract(this.transform.position);
    }
}
