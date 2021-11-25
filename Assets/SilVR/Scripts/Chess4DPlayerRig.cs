using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chess4DPlayerRig : MonoBehaviour
{
    public bool isPlayerWhite;
    
    public float rotation_speed;

    public float min_height;
    public float max_height;

    public GameObject cursor_object;
    public Camera camera;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public Vector4 RaycastCursor()
    {
        float discriminator = 0;

        Vector3 target_pos = Vector3.zero;

        LayerMask layerMask = 1 << 0;
        RaycastHit hit;

        Vector3 raycast_dir = camera.ScreenPointToRay(Input.mousePosition).direction;

        if (Physics.Raycast(camera.transform.position, raycast_dir, out hit, 2.0f, layerMask))
        {
            cursor_object.SetActive(true);
            target_pos = hit.point;
            //Debug.Log("Found Object");
            discriminator = 1;
        }
        else
        {
            cursor_object.SetActive(false);
        }

        cursor_object.transform.position = target_pos;

        return new Vector4(target_pos.x, target_pos.y, target_pos.z, discriminator);
    }

    // Update is called once per frame
    void Update()
    {
        Vector4 hit_info = RaycastCursor();
        
        bool did_hit = hit_info.w > 0.5f;
        Vector3 hit_pos = hit_info;

    }
}
