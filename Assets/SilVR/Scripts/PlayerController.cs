
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerController : UdonSharpBehaviour
{
    Chess4DBoard board;
    
    bool isInitialized = false;
    public GameObject zCast;

    public void InitializePlayerController(Chess4DBoard init_board)
    {
        board = init_board;
        isInitialized = true;
    }

    public void Update()
    {
        CastCursorDesktop();
    }

    public void CastCursorDesktop()
    {
        VRCPlayerApi player = Networking.LocalPlayer;
        int layermask = 1 << 14;

        //Vector3 position = player.GetBonePosition(HumanBodyBones.Head);
        //Quaternion rotation = player.GetBoneRotation(HumanBodyBones.Head);
        if (player != null)
        {
            Vector3 offset = Vector3.zero;
            Vector3 position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            Quaternion rotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
            Vector3 CastingDirection = Vector3.forward;
            if (player.IsUserInVR())
            {
                position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                rotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                CastingDirection = Vector3.right;
                Vector3 hand_pos = player.GetBonePosition(HumanBodyBones.RightHand);
                Vector3 fing_pos = player.GetBonePosition(HumanBodyBones.RightIndexProximal);
                offset = fing_pos - hand_pos;
            }
            Vector3 cast_dir = rotation * CastingDirection;
            if (player.IsUserInVR())
            {
                cast_dir = offset;
            }


            RaycastHit hit;
            if (Physics.Raycast(position, rotation*CastingDirection, out hit, Mathf.Infinity, layermask))
            {
                zCast.transform.position = hit.point;
                SnapCursor();
            }
            else
            {
                zCast.transform.localPosition = Vector3.zero;
            }
        }

    }

    public void SnapCursor()
    {
        if (isInitialized)
        {
            Vector4 coordinate = board.SnapToCoordinateVerbose(zCast.transform.position);
            bool isValidCoord = (!board.IsVectorOutOfBounds(coordinate));
            int x = Mathf.RoundToInt(coordinate.x);
            int y = Mathf.RoundToInt(coordinate.y);
            int z = Mathf.RoundToInt(coordinate.z);
            int w = Mathf.RoundToInt(coordinate.w);

            if (isValidCoord)
            {
                zCast.transform.position = board.PosFromCoord(x, y, z, w);
                int square = (x << 0) + (y << 2) + (z << 4) + (w << 6);
            }
        }
    }

    void Start()
    {
        
    }
}
