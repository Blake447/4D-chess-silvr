
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerController : UdonSharpBehaviour
{
    Chess4DBoard board;
    
    bool isInitialized = false;
    public GameObject zCast_right;
    public GameObject zCast_left;
    public void InitializePlayerController(Chess4DBoard init_board)
    {
        board = init_board;
        isInitialized = true;
    }

    public void Update()
    {
        MoveCursorForPlayer();
    }


    public void SnapToPlayerHand()
    {
        VRCPlayerApi player_local = Networking.LocalPlayer;
        if (player_local != null)
        {
            Vector3 hand_pos_right = player_local.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
            Quaternion hand_rot_right = player_local.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
            Vector3 target_pos_right = hand_pos_right + hand_rot_right * new Vector3(0.025f, 0.0f, 0.025f); ;

            Vector3 hand_pos_left = player_local.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
            Quaternion hand_rot_left = player_local.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
            Vector3 target_pos_left = hand_pos_left + hand_rot_left * new Vector3(0.025f, 0.0f, 0.025f);

            

        }
    }


    public void MoveCursorForPlayer()
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
                Vector3 hand_pos_right = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                Quaternion hand_rot_right = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                Vector3 target_pos_right = hand_pos_right + hand_rot_right * new Vector3(0.030f, 0.0f, 0.030f); ;

                Vector3 hand_pos_left = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                Quaternion hand_rot_left = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
                Vector3 target_pos_left = hand_pos_left + hand_rot_left * new Vector3(0.030f, 0.0f, 0.030f);

                position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                rotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                CastingDirection = Vector3.right;
                Vector3 hand_pos = player.GetBonePosition(HumanBodyBones.RightHand);
                Vector3 fing_pos = player.GetBonePosition(HumanBodyBones.RightIndexProximal);
                offset = fing_pos - hand_pos;

                zCast_left.transform.localPosition = target_pos_left;
                zCast_left.transform.position -= new Vector3(0.0f, 0.0325f, 0.0f);
                //SnapCursor();
                if (!SnapCursor(zCast_left))
                {
                    zCast_left.transform.localPosition = Vector3.zero;
                }
                zCast_left.transform.position += new Vector3(0.0f, 0.0325f, 0.0f);


                zCast_right.transform.localPosition = target_pos_right;
                zCast_right.transform.position -= new Vector3(0.0f, 0.0325f, 0.0f);
                //SnapCursor();
                if (!SnapCursor(zCast_right))
                {
                    zCast_right.transform.localPosition = Vector3.zero;
                }
                zCast_right.transform.position += new Vector3(0.0f, 0.0325f, 0.0f);
            }
            else
            {

                Vector3 cast_dir = rotation * CastingDirection;
                RaycastHit hit;
                if (Physics.Raycast(position, rotation*CastingDirection, out hit, Mathf.Infinity, layermask))
                {
                    zCast_right.transform.position = hit.point;
                    //SnapCursor();
                }
                else
                {
                    zCast_right.transform.localPosition = Vector3.zero;
                }
            }
        }

    }

    public bool SnapCursor(GameObject cursor)
    {
        if (isInitialized)
        {
            Vector4 coordinate = board.SnapToCoordinateVerbose(cursor.transform.position);
            bool isValidCoord = (!board.IsVectorOutOfBounds(coordinate));
            int x = Mathf.RoundToInt(coordinate.x);
            int y = Mathf.RoundToInt(coordinate.y);
            int z = Mathf.RoundToInt(coordinate.z);
            int w = Mathf.RoundToInt(coordinate.w);

            if (isValidCoord)
            {
                cursor.transform.position = board.PosFromCoord(x, y, z, w);
                int square = (x << 0) + (y << 2) + (z << 4) + (w << 6);
            }
            return isValidCoord;
        }
        return false;
    }

    void Start()
    {
        
    }
}
