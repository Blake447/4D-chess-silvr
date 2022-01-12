
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class FloorColliderManager : UdonSharpBehaviour
{
    public GameObject[] FloorColliders;

    public void SetFloorColliders0()
    {
        SetFloorColliders(0);
    }
    public void SetFloorColliders1()
    {
        SetFloorColliders(1);
    }
    public void SetFloorColliders2()
    {
        SetFloorColliders(2);
    }
    public void SetFloorColliders3()
    {
        SetFloorColliders(3);
    }
    public void SetFloorColliders4()
    {
        SetFloorColliders(4);
    }

    public void SetFloorColliders(int index)
    {
        for (int i = 0; i < FloorColliders.Length; i++)
        {
            FloorColliders[i].SetActive(i < index);
        }
    }

    void Start()
    {
        
    }
}
