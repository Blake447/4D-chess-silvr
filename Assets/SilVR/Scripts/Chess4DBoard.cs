﻿using UnityEngine;

public class Chess4DBoard : MonoBehaviour
{
    int[] squares = new int[256];
    GameObject squares_root;

    public GameObject ReferencePieces;
    public MeshFilter[] referenceMeshes;
    public MeshRenderer[] referenceRenderers;

    public GameObject SquareTemplate;

    int MESH_OFFSET = 0;
    int MESH_COUNT = 6;

    int RENDERER_OFFSET = 6;
    int RENDERER_COUNT = 2;

    public GameObject BasisOrigin;

    private float xy_offset = 0.0825f;
    private float z_offset = 0.1235f;
    private float z_scaling = 0.9f;
    private float w_offset = 0.4125f;
    private float t_offset = 1.0f;
    private float m_offset = 2.5f;

    Vector3 root;
    Vector3 rt = new Vector3(1, 0, 0);
    Vector3 up = new Vector3(0, 1, 0);
    Vector3 fw = new Vector3(0, 0, 1);


    public Chess4DEvaluator evaluator;

    public int[] GetSquareArray()
    {
        return (int[])squares.Clone();
    }

    int[] start_state = new int[256] {
                                        5, 4, 4, 5,  6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,
                                        3, 1, 2, 3,  6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,
                                        3, 2, 6, 3,  6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,
                                        5, 4, 4, 5,  6, 6, 6, 6,  0, 0, 0, 0,  6, 0, 1, 0,
                                        6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
                                        6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
                                        6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
                                        6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,  0, 1, 0, 0,
                                        0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 6, 0, 12,12,12,12,
                                        0, 0, 0, 0,  0, 0, 0, 0,  0, 1, 0, 0, 12,12,12,12,
                                        0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0, 12,12,12,12,
                                        0, 0, 0, 0,  0, 0, 0, 0,  0, 6, 0, 0, 12,12,12,12,
                                        0, 0, 0, 0,  0, 0, 0, 0, 12,12,12,12, 11,10,10,11,
                                        0, 0, 0, 0,  0, 0, 1, 0, 12,12,12,12,  9, 7, 8, 9,
                                        0, 0, 0, 0,  0, 0, 0, 6, 12,12,12,12,  9, 8,12, 9,
                                        0, 0, 0, 0,  0, 0, 0, 0, 12,12,12,12, 11,10,10,11
                                      };


    void FillReferenceMeshes(GameObject ReferenceRoot, int start, int length)
    {
        referenceMeshes = new MeshFilter[length];
        for (int i = 0; i < length; i++)
        {
            referenceMeshes[i] = ReferenceRoot.transform.GetChild(start + i).GetComponent<MeshFilter>();
        }
    }
    void FillReferenceRenderers(GameObject ReferenceRoot, int start, int length)
    {
        referenceRenderers = new MeshRenderer[length];
        for (int i = 0; i < length; i++)
        {
            referenceRenderers[i] = ReferenceRoot.transform.GetChild(start + i).GetComponent<MeshRenderer>();
        }
    }
    void FindAndSetSquaresRoot()
    {
        squares_root = this.transform.GetChild(0).gameObject;
    }
    void InstantiatePieces()
    {
        for (int i = 0; i < squares.Length; i++)
        {
            Vector3 position = PosFromIndex(i);
            GameObject square_instance = Instantiate(SquareTemplate, position, Quaternion.identity);
            square_instance.transform.parent = squares_root.transform;
            square_instance.name = "Square" + (i / 100 % 10).ToString() + (i / 10 % 10).ToString() + (i % 10).ToString(); 
        }
    }

    public void SetPieces(int[] state)
    {
        squares = (int[])state.Clone();
        SetPiecesFromSquares();
    }

    void SetPiecesFromSquares()
    {
        for (int i = 0; i < squares.Length; i++)
        {
            GameObject square = squares_root.transform.GetChild(i).gameObject;

            MeshFilter mesh_filter = square.GetComponent<MeshFilter>();
            MeshRenderer mesh_renderer = square.GetComponent<MeshRenderer>();

            mesh_filter.sharedMesh = GetMesh(squares[i]);
            mesh_renderer.sharedMaterial = GetMaterial(squares[i]);
        }
    }

    Material GetMaterial(int piece_id)
    {
        if (piece_id == 0) { return null; }
        return referenceRenderers[((piece_id - 1) / 6) & 1].sharedMaterial;
    }
    Mesh GetMesh(int piece_id)
    {
        if (piece_id == 0) { return null; }
        return referenceMeshes[((piece_id - 1) % 6)].mesh;
    }

    void InitializeSquares()
    {
        squares = (int[])start_state.Clone();
    }

    public void InitializeBoard()
    {
        FillReferenceMeshes(ReferencePieces, MESH_OFFSET, MESH_COUNT);
        FillReferenceRenderers(ReferencePieces, RENDERER_OFFSET, RENDERER_COUNT);
        FindAndSetSquaresRoot();
        UpdateBasis();
        InitializeSquares();
        SetPiecesFromSquares();
    }


    // Start is called before the first frame update
    void Start()
    {
        InitializeBoard();
        if (evaluator != null)
        {
            evaluator.InitializeEvaluator(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    // Convert a 4-Vector to an index ranging 0-255
    public int CoordToIndex(int xi, int yi, int zi, int wi)
    {
        return xi + yi * 4 + zi * 16 + wi * 64;
    }

    public Vector3 PosFromIndex(int i)
    {
        int x = (i >> 0) & 3;
        int y = (i >> 2) & 3;
        int z = (i >> 4) & 3;
        int w = (i >> 6) & 3;

        return PosFromCoord(x, y, z, w);
    }

    private Vector3 PosFromCoord(int xi, int yi, int zi, int wi)
    {
        // Okay, recall our chess set has 4 coordinates. The xy by chess in the chess convention is scaled the same, 
        // and represents our traditional chess board. The z direction is up and down, and the board scales by some
        // factor each time it goes up. The w direction is "Hyper-forward", and is a fixed offset.
        // To get from the bottom corner to any other given one given a coordinate, we need to that into account.

        // Easy, z coordinate multiplied the scale of the offset, times the up vector.
        Vector3 z = zi * z_offset * up;

        // Since the z coordinate scaleds by a factor as we go up, we want to move this offset to the center before scaling
        Vector3 center = xy_offset * 1.5f * (rt + fw);

        // Determine the scaling factor. Scales by a fixed factor each time up, so we'll use a for loop. On the zeroth layer,
        // coordinate.z = 0, the loop doesnt run. On the first, coordinate.z = 1 so it runs once. Start at one
        float scaler = 1.0f;

        // Each time we go up, multiply by our scaling factor.
        for (int i = 0; i < zi; i++)
        {
            scaler *= z_scaling;
        }

        // Next we want to undo our center vector, but multiplied by the scaling we specified.
        Vector3 un_center = -center * scaler;

        // XY offset is pretty easy. Just make sure to account for the scaling.
        Vector3 xy = (xi * rt + yi * fw) * xy_offset * scaler;

        // W offset is likewise easy, just apply a fixed offset
        Vector3 w = fw * wi * w_offset;

        Vector3 position_from_coordinate = root + center + z + un_center + xy + w;

        // Now lets go through the whole process. Move to center, move up, uncenter, and apply the rest of the coordinates
        return position_from_coordinate;

        // There we go. Now once the values are fine tuned, all the hypersquares should line themselves up based on their coordinates.
    }

    public void UpdateBasis()
    {
        // so long as we have a root object to reference
        if (BasisOrigin != null)
        {
            // Get the objects transform
            Transform t = BasisOrigin.transform;

            // Set our origin for the coordinate systems
            root = t.position;

            // Update our basis to its local basis
            rt = t.right;
            up = t.up;
            fw = t.forward;

            // Scale our offsets by its global scale
            xy_offset = 0.0825f * t.lossyScale.z;
            z_offset = 0.1235f * t.lossyScale.z;
            z_scaling = 0.9f;
            w_offset = 0.4125f * t.lossyScale.z;
            t_offset = 1.0f * t.lossyScale.z;
            m_offset = 2.5f * t.lossyScale.z;

        }
    }

}