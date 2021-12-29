
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Chess4DVisualizer : UdonSharpBehaviour
{
    public int x;
    public int y;
    public int z;
    public int w;


    public Chess4DBoard chess_board;
    public Material visualizer_mat;
    public bool isInitialized = false;

    public void SetVisualizerState(Vector4 coordinate, int piece_type, int color)
    {
        int pawn_mod = piece_type == 6 ? color : 0;
        for (int i = 0; i < this.transform.childCount; i++)
        {
            this.transform.GetChild(i).gameObject.SetActive(i + 1 == piece_type + pawn_mod);
        }
        visualizer_mat.SetVector("_Coordinate", coordinate);
    }

    public void InitializeVisualizer()
    {
        isInitialized = true;
    }

    void Start()
    {
        
    }
}
