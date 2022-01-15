using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
#if UDON
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
public class Chess4DBoard : UdonSharpBehaviour
#else
public class Chess4DBoard : MonoBehaviour
#endif
{
    public PlayerController playerController;
    public Chess4DEvaluator ai_player;

    public Chess4DVisualizer visualizer;
    public GameObject selection_cursor;
    public GameObject SquareTemplate;
    public TVBotRig tv_bot;
    public GameObject piece_arrow;
    public GameObject turn_indicator_white;
    public GameObject turn_indicator_black;



    public GameObject[] LevelIndicators;
    public Text white_field;
    public Text black_field;
    public InputField human_log;
    public InputField comps_log;

    int MESH_OFFSET = 0;
    int MESH_COUNT = 6;

    int MAT_OFFSET = 6;
    int MAT_COUNT = 2;

    int NULL = 1024;
    int selected_square = 1024;

    public GameObject ReferencePieces;
    public GameObject ReferenceReflections;

    public Mesh[] ref_meshes_00;
    Mesh[] ref_meshes_01;
    Mesh[] ref_meshes_02;
    Mesh[] ref_meshes_03;
    Mesh[] ref_meshes_04;
    Mesh[] ref_meshes_05;
    Mesh[] ref_meshes_06;
    Mesh[] ref_meshes_07;

    public Material[] ref_mats_00;
    Material[] ref_mats_01;
    Material[] ref_mats_02;
    Material[] ref_mats_03;
    Material[] ref_mats_04;
    Material[] ref_mats_05;
    Material[] ref_mats_06;
    Material[] ref_mats_07;

    GameObject squares_root;
    int[] squares = new int[256];
    int[] squares_buffer = new int[256];

    public GameObject BasisOrigin;
    private float xy_offset = 0.0825f;
    private float z_offset = 0.1235f * 1.33333333f;
    private float z_scaling = 0.9f;
    private float w_offset = 0.4125f;
    private float t_offset = 1.0f;
    private float m_offset = 2.5f;

    Vector3 root;
    Vector3 rt = new Vector3(1, 0, 0);
    Vector3 up = new Vector3(0, 1, 0);
    Vector3 fw = new Vector3(0, 0, 1);

    public bool has_target = false;
    public int target_piece = 0;

    public float move_speed = 0.1f;

    bool isWhitesTurn;

    [UdonSynced]
    public int[] network_history;

    public int[] move_history;
    public int[] move_buffer;

    [UdonSynced]
    public string white_name = "";

    [UdonSynced]
    public string black_name = "";

    [UdonSynced]
    public int tv_bot_state = 0;

    [UdonSynced]
    public int AI_level = 1;


    public bool queued_ai_as_white = true;
    public int turn_ai_queued = 0;

    int[] target_squares = new int[8];
    int target_squares_count = 0;

    GameObject[] target_objects = new GameObject[8];
    Vector3[] target_vectors = new Vector3[8];
    bool[] target_flags = new bool[8];

    public Material[] material_list = new Material[2];

    ///////////////////////////////////////
    ///                                 ///
    ///      Setting Object Meshes      ///
    ///                                 ///
    ///////////////////////////////////////

    public void InitializeReferenceSets()
    {
        ref_meshes_00 = new Mesh[MESH_COUNT];
        ref_mats_00 = new Material[MAT_COUNT];

        ref_meshes_01 = new Mesh[MESH_COUNT];
        ref_mats_01 = new Material[MAT_COUNT];

        ref_meshes_02 = new Mesh[MESH_COUNT];
        ref_mats_02 = new Material[MAT_COUNT];

        ref_meshes_03 = new Mesh[MESH_COUNT];
        ref_mats_03 = new Material[MAT_COUNT];
    }

    // Piece setting methods.
    // Set all piece meshes and materials based on supplied integer array reference
    public void SetPiecesToState(int[] state)
    {
        squares = (int[])state.Clone();

        // Set pieces from the squares array, grabbing references from ref_mesh array 0 and ref_mats array 0
        SetAllChildrenRendering(squares_root, ref_meshes_00, ref_mats_00);
    }

    public void FillReferenceSet(Mesh[] mesh_set, Material[] material_set, GameObject reference_root)
    {
        //mesh_set = new Mesh[MESH_COUNT];
        for (int i = 0; i < MESH_COUNT; i++)
        {
            mesh_set[i] = reference_root.transform.GetChild(i + MESH_OFFSET).gameObject.GetComponent<MeshFilter>().sharedMesh;
        }
        //material_set = new Material[RENDERER_COUNT];
        for (int i = 0; i < MAT_COUNT; i++)
        {
            material_set[i] = reference_root.transform.GetChild(i + MAT_OFFSET).gameObject.GetComponent<MeshRenderer>().sharedMaterial;
        }
    }

    // Get the material based on the supplied piece id. Ex. if we supply a white rook, (id = 10) we will get Material 0 for black
    Material GetMaterialFromRefSet(Material[] material_set, int piece_id)
    {
        if (piece_id == 0) { return null; }
        int color = ((piece_id - 1) / 6) & 1;
        return material_set[color];
    }
    // Get the mesh based on the supplied piece id. Ex. if we supply a white rook, (colorless id = 5, full id = 10) we will get Mesh 4 for rook
    Mesh GetMeshFromRefSet(Mesh[] mesh_set, int piece_id)
    {
        if (piece_id == 0) { return null; }
        return mesh_set[((piece_id - 1) % 6)];
    }

    void SetAllChildrenRendering(GameObject root, Mesh[] mesh_set, Material[] material_set)
    {
        for (int i = 0; i < squares.Length; i++)
        {
            GameObject piece_object = root.transform.GetChild(i).gameObject;

            // TODO: Factor this setting out since we dont always need to do this
            piece_object.transform.position = PosFromIndex(i);

            MeshFilter mesh_filter = piece_object.GetComponent<MeshFilter>();
            MeshRenderer mesh_renderer = piece_object.GetComponent<MeshRenderer>();

            mesh_filter.sharedMesh = GetMeshFromRefSet(mesh_set, squares[i]);
            mesh_renderer.sharedMaterial = GetMaterialFromRefSet(material_set, squares[i]);
        }
    }

    void SetChildObjectsRendering(GameObject root, int square, Mesh[] mesh_set, Material[] material_set)
    {
        GameObject piece_object = root.transform.GetChild(square).gameObject;

        // TODO: Factor this setting out since we dont always need to do this
        piece_object.transform.position = PosFromIndex(square);

        MeshFilter mesh_filter = piece_object.GetComponent<MeshFilter>();
        MeshRenderer mesh_renderer = piece_object.GetComponent<MeshRenderer>();

        mesh_filter.sharedMesh = GetMeshFromRefSet(mesh_set, squares[square]);
        mesh_renderer.sharedMaterial = GetMaterialFromRefSet(material_set, squares[square]);
    }

    ///////////////////////////////////////
    ///                                 ///
    ///        Animating Pieces         ///
    ///                                 ///
    ///////////////////////////////////////

    GameObject FetchGameobjectsChild(GameObject root, int square)
    {
        return root.transform.GetChild(square).gameObject;
    }

    public void InitializeTargets()
    {
        for (int i = 0; i < target_squares.Length; i++)
        {
            target_squares[i] = NULL;
            target_flags[i] = false;
        }
    }
    public void AddTarget(int target_index, int square, GameObject target_object)
    {
        if ( target_flags[target_index] )
        {
            target_objects[target_index].transform.position = target_vectors[target_index];
        }

        target_objects[target_index] = target_object;
        target_squares[target_index] = square;
        target_vectors[target_index] = PosFromIndex(square);
        target_flags[target_index] = true;
    }
    public void ClearTarget(int target_index)
    {
        target_squares[target_index] = NULL;
        target_flags[target_index] = false;
    }
    public void UpdateTarget(int target_index, float speed)
    {
        target_objects[target_index].transform.position = Vector3.MoveTowards(target_objects[target_index].transform.position, target_vectors[target_index], speed);
    }
    public void UpdateTargetIfNotNull(int target_index, float speed)
    {
        if (target_flags[target_index])
        {
            target_objects[target_index].transform.position = Vector3.MoveTowards(target_objects[target_index].transform.position, target_vectors[target_index], speed);
        }
    }


    ///////////////////////////////////////
    ///                                 ///
    ///         Gizmo Control           ///
    ///                                 ///
    ///////////////////////////////////////

    public void SetTurnIndicator()
    {
        int color = DetermineTurnFromHistory();
        turn_indicator_white.SetActive(color == 1);
        turn_indicator_black.SetActive(color == 0);
    }

    public void SetArrow(GameObject arrow, int from, int to)
    {
        arrow.transform.position = PosFromIndex(from) + Vector3.up * z_offset * 0.125f;
        PointArrow(arrow, PosFromIndex(to) + Vector3.up * z_offset * 0.125f);
    }

    public void ResetArrow(GameObject arrow)
    {
        arrow.SetActive(false);
    }

    public void PointArrow(GameObject arrow, Vector3 target)
    {
        float main_scale = this.gameObject.transform.lossyScale.x;

        arrow.SetActive(true);

        Vector3 offset = (target - arrow.transform.position);
        float distance = (offset.magnitude - xy_offset*0.5f) * 2 / main_scale;

        Quaternion rotation = Quaternion.LookRotation(offset.normalized, Vector3.up);

        arrow.transform.rotation = rotation;
        GameObject arrow_stem = arrow.transform.GetChild(0).gameObject;
        GameObject arrow_tip = arrow.transform.GetChild(1).gameObject;

        if (arrow_stem != null)
        {
            arrow_stem.transform.localScale = new Vector3(25, 25 * (distance * 4.0f), 25);
        }
        if (arrow_tip != null)
        {
            arrow_tip.transform.localPosition = Vector3.forward * distance;
        }
    }



    ///////////////////////////////////////
    ///                                 ///
    ///      Board Initialization       ///
    ///                                 ///
    ///////////////////////////////////////

    // Start is called before the first frame update
    void Start()
    {
        // Initialize this board first
        InitializeBoard();

        // Initialize the visualizer
        visualizer.InitializeVisualizer();

        // If we have a player controller assigned, initialize it
        if (playerController != null)
        {
            playerController.InitializePlayerController(this);
        }
        else
        {
            Debug.Log("Warning, no player controller set");
        }

        // If we have an AI player, initialize it
        if (ai_player != null)
        {
            ai_player.InitializeEvaluator(this);
        }
        else
        {
            Debug.Log("Board has been initialized without AI player");
        }

        // Initialize Targets for animating pieces
        InitializeTargets();
    }


    public void InitializeBoard()
    {
        // Initialize reference sets to be of size defined by global variables MESH_COUNT and MAT_COUNT.
        InitializeReferenceSets();

        // Fill the reference set 00 based on the children of ReferencePieces. Lifts the first MESH_COUNT meshes from the children of
        // ReferencePieces, then offsets by MAT_OFFSET to get the next MAT_COUNT materials from those same children, storing
        // the results in ref_meshes_xx and ref_renderers_xx specified for later, faster access.
        FillReferenceSet(ref_meshes_00, ref_mats_00, ReferencePieces);

        // Find the parent object of all square game objects
        FindAndSetSquaresRoot();

        // Update our coordinate space so that the board can operate at arbitrary scales and rotations
        UpdateBasis();

        // Initialize the squares array that keeps track of the board state
        InitializeSquares();

        // Set all the children of squares root to the correct mesh and material
        SetAllChildrenRendering(squares_root, ref_meshes_00, ref_mats_00);
        
        // Update the pieces on the board to match the state stored in the board state array.
        //SetPiecesFromSquares();

    }

    // Locate and save the reference of the parent child of all square objects.
    void FindAndSetSquaresRoot()
    {
        squares_root = this.transform.GetChild(0).gameObject;
    }


    // Update our coordinate space so that we can operate the board at arbitrary transforms.
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
            z_offset = 0.1235f * t.lossyScale.z * 1.33333333f;
            z_scaling = 0.9f;
            w_offset = 0.4125f * t.lossyScale.z;
            t_offset = 1.0f * t.lossyScale.z;
            m_offset = 2.5f * t.lossyScale.z;

        }
    }


    // Initialize the squares array to its starting state. Note that this is not in a very human readable format
    void InitializeSquares()
    {
        squares = (int[])start_state.Clone();
    }


    int[] start_state = new int[256] {
                                        5, 4, 4, 5,  6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,
                                        3, 1, 2, 3,  6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,
                                        3, 2, 6, 3,  6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,
                                        5, 4, 4, 5,  6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,
                                        6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
                                        6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
                                        6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
                                        6, 6, 6, 6,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
                                        0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0, 12,12,12,12,
                                        0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0, 12,12,12,12,
                                        0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0, 12,12,12,12,
                                        0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0, 12,12,12,12,
                                        0, 0, 0, 0,  0, 0, 0, 0, 12,12,12,12, 11,10,10,11,
                                        0, 0, 0, 0,  0, 0, 0, 0, 12,12,12,12,  9, 7, 8, 9,
                                        0, 0, 0, 0,  0, 0, 0, 0, 12,12,12,12,  9, 8,12, 9,
                                        0, 0, 0, 0,  0, 0, 0, 0, 12,12,12,12, 11,10,10,11
                                      };





    ///////////////////////////////////////
    ///                                 ///
    ///         Update Function         ///
    ///                                 ///
    ///////////////////////////////////////

    // Update is called once per frame. TODO: Lighten up on update logic.
    // ai_player != null will early terminate on not having ai player assigned, but the moving of the target piece
    // on every update is kind of math heavy. Might be best to store target location rather than recalculating every time.
    // Move towards is a native c# call so that probabably has very little impact on frame time.
    void Update()
    {
        // If we have an ai player, and the ai player is not already searching for a move.
        // Note that if the ai player is not assigned, it skips any reference to ai_player.
        if (ai_player != null && !ai_player.IsEvaluatorBusy())
        {
            // record the current turn
            int current_turn = move_history.Length;

            // determine the color that needs to move
            bool isWhite = DetermineTurnFromHistory() == 1;

            // Determine if the ai is joined as a color that can move right now
            if ((isWhite && IsAIWhite()) || (!isWhite && IsAIBlack()))
            {
                // Determine if we are hosting AI
                if (IsHost())
                {
                    // If the ai is joined correctly and we are hosting AI
                    // Queue up a move on the current turn, as the current color for the ai to search for.
                    turn_ai_queued = current_turn;
                    queued_ai_as_white = isWhite;
                    ai_player.QueueMove(current_turn, isWhite);
                }
            }
        }

        // Move the target piece towards its target position, if its not null.
        // 0 specifices the first target, and move speed sets how far to move the piece per frame.
        UpdateTargetIfNotNull(0, move_speed * Time.deltaTime);

        // Set the turn indicator to show whos turn it is to play.
        // TODO: Get this out of update function
        SetTurnIndicator();
    }





    ///////////////////////////////////////
    ///                                 ///
    ///       Game Related Logic        ///
    ///                                 ///
    ///////////////////////////////////////

    // TODO: Implement a check search
    public void CheckForCheck(int square, int color)
    {




    }


    // Handle the logic for what interacting the board should be like. Called by piece_interface, which
    // supplies its own global position as input.
    public void OnInterfaceInteract(Vector3 position)
    {
        // Snap the incoming position to the nearest coordinate
        Vector4 coord = SnapToCoordinateVerbose(position);

        // determine if that coordinate is valid
        bool isValidCoord = !IsVectorOutOfBounds(coord);

        // Determine if we have a square selected. Not to be confused with targeted. Selected squares are
        // select for the player to make a move and see visualizer, not for animating the piece.
        bool isSquareSelected = selected_square != NULL;

        // If we interfaced with a valid coordinate
        if (isValidCoord)
        {
            // Calculate the index that the coordinate links to
            int square = VectorToIndex(coord);

            // Get various piece information like piece id, color, if the square was empty, and what kind of piece it was.
            int piece = squares[square];
            int color = PieceColor(piece);
            bool square_empty = piece == 0;
            int piece_type = PieceTypeColorless(piece);

            // If we havent selected a square before, and the square we just interfaced with was not empty
            if (!isSquareSelected && !square_empty)
            {
                // Set the visualizer accordingly
                visualizer.SetVisualizerState(coord, piece_type, color);

                // and select the square we interfaced with
                selected_square = square;
            }
            // if we did have a square selected
            else if (isSquareSelected)
            {
                // determine if the move from the selected square, to the square we just interfaced with is valid.
                if (IsValidMove(selected_square, square))
                {
                    // If it is valid, but we dont have a local player (for editor testing)
                    if (Networking.LocalPlayer == null)
                    {
                        MakeMove(selected_square, square);
                        selected_square = NULL;
                        visualizer.SetVisualizerState(coord, 0, 0);
                        PushToNetwork();
                    }
                    // Otherwise, if its valid and the network is working properly
                    else
                    {
                        // Determine if the local player is joined as white or black. Note that they can be joined as both
                        bool isWhite = IsPlayerWhite();
                        bool isBlack = IsPlayerBlack();

                        // Determine the color of the selected piece.
                        int selected_color = PieceColor(squares[selected_square]);

                        // Determine if the player is joined in as the same color of the selected piece
                        bool joined_correctly = ((selected_color == 0) && isBlack) || ((selected_color == 1) && isWhite);
                        if (joined_correctly)
                        {
                            // If the move was valid, we have a valid network connection, and the player is joined as the right color
                            // make the move
                            MakeMove(selected_square, square);

                            // Clear the selected square
                            selected_square = NULL;

                            // clear the visualizer
                            visualizer.SetVisualizerState(coord, 0, 0);

                            // and our changes to the network.
                            PushToNetwork();
                        }
                        // If anything fails, deselect the current piece and clear the visualizer state.
                        else
                        {
                            selected_square = NULL;
                            visualizer.SetVisualizerState(coord, 0, 0);
                        }
                    }
                }
                else
                {
                    selected_square = NULL;
                    visualizer.SetVisualizerState(coord, 0, 0);
                }
            }
            else
            {
                selected_square = NULL;
                visualizer.SetVisualizerState(coord, 0, 0);
            }

        }
        else
        {
            selected_square = NULL;
            visualizer.SetVisualizerState(coord, 0, 0);
        }

        if (selected_square != NULL)
        {
            selection_cursor.SetActive(true);
            selection_cursor.transform.position = PosFromIndex(selected_square);
        }
        else
        {
            selection_cursor.SetActive(false);
        }




    }


    // Determine if we have a valid move
    public bool IsValidMove(int from, int to)
    {
        // Terminate if the piece is not actually moving at all. I believe the main reason for this is to prevent empty
        // network pushes that would take up the players turn, and might trigger when attempting to deselect pieces.
        bool isActuallyMove = from != to;
        if (!isActuallyMove) { return false; }

        // Get the pieces on the to and from square
        int piece_id_from = squares[from];
        int piece_id_to = squares[to];

        // terminate if the piece we are moving is an empty square
        bool isMovingPiece = piece_id_from != 0;
        if (!isMovingPiece) { return false; }

        // Determine the type of piece it is (rook, queen, pawn, etc.)
        int piece_type = PieceTypeColorless(piece_id_from);

        // get the color of the moving piece, and the piece at the square it lands on.
        int color_from = PieceColor(piece_id_from);
        int color_to = PieceColor(piece_id_to);

        // terminate early if the color of piece we are moving is not the color of the piece that needs to move this turn
        bool isPlayersTurn = color_from == DetermineTurnFromHistory();
        if (!isPlayersTurn) { return false; }

        // determine if we are moving to an empty square
        bool isToEmpty = piece_id_to == 0;

        // if we are not moving to an empty square, determine if we are moving to a piece of a different color
        bool isDifferentColor = color_from != color_to && !isToEmpty;

        // if the target square is empty, or the square is not empty but a different colored piece, flag as valid target.
        bool isTargetValid = isToEmpty || isDifferentColor;

        // if we are attempting to move to an invalid target (not empty, same color), terminate early
        if (!isTargetValid) { return false; }

        // unpack the to and from coordinates into integers ranging from 0-3.
        int xf = (from >> 0) & 3;
        int yf = (from >> 2) & 3;
        int zf = (from >> 4) & 3;
        int wf = (from >> 6) & 3;

        int xt = (to >> 0) & 3;
        int yt = (to >> 2) & 3;
        int zt = (to >> 4) & 3;
        int wt = (to >> 6) & 3;

        // store the true offsets into an array of length 4.
        int[] offsets = new int[4];
        offsets[0] = xt - xf;
        offsets[1] = yt - yf;
        offsets[2] = zt - zf;
        offsets[3] = wt - wf;

        // determine if we are moving two forward directions, or moving two lateral directions
        bool violates_forward_forward = (offsets[1] * offsets[3]) != 0;
        bool violates_lateral_lateral = (offsets[0] * offsets[2]) != 0;

        // terminate early if we violate the forward-lateral rule.
        // TODO: As my decision to seperate directions into forward / lateral is somewhat controversial,
        // setup a toggle for this check, and add extra checks later, since we assume this is done in the
        // next couple checks.
        bool violates_forward_lateral_rule = violates_forward_forward || violates_lateral_lateral;
        if (violates_forward_lateral_rule) { return false; }

        // Setup an array of length 4.
        int[] offsets_sorted = new int[4];

        // encode the index of the offsets into the two least significant bits. This is done so that when
        // we use Mathf.Min or Mathf.Max, we know which of the originals is chosen as the result by simply
        // checking the least significant bits, which only matter for ties.
        offsets_sorted[0] = (Mathf.Abs(offsets[0]) << 2) + 0;
        offsets_sorted[1] = (Mathf.Abs(offsets[1]) << 2) + 1;
        offsets_sorted[2] = (Mathf.Abs(offsets[2]) << 2) + 2;
        offsets_sorted[3] = (Mathf.Abs(offsets[3]) << 2) + 3;

        // Get the largest and smallest absolute value of the coordinate offsets
        int min = Mathf.Min(offsets_sorted);
        int max = Mathf.Max(offsets_sorted);

        // Get the index of the largest and smallest values
        int min_index = min & 3;
        int max_index = max & 3;

        // set up an int buffer for swapping entries
        int buffer = offsets_sorted[0];

        // swap the 0th element for the mininmum. Note that if the 0th is the minimum, this changes nothing
        offsets_sorted[0] = offsets_sorted[min_index];
        offsets_sorted[min_index] = buffer;

        // if the maximum index was 0, then it has been swapped to what the minimum index was previously, so update its reference
        if (max_index == 0) { max_index = min_index; }

        // swap the last element with the maximum element
        buffer = offsets_sorted[3];
        offsets_sorted[3] = offsets_sorted[max_index];
        offsets_sorted[max_index] = buffer;


        // grab the middle two elements of the array
        int a = offsets_sorted[1];
        int b = offsets_sorted[2];

        // and reorganize them so they are sorted correctly
        offsets_sorted[1] = Mathf.Min(a, b);
        offsets_sorted[2] = Mathf.Max(a, b);

        // Strip out the encoded indices and rescale the elements so that they simply represent components 0-3 once again (but sorted!)
        offsets_sorted[0] = offsets_sorted[0] >> 2;
        offsets_sorted[1] = offsets_sorted[1] >> 2;
        offsets_sorted[2] = offsets_sorted[2] >> 2;
        offsets_sorted[3] = offsets_sorted[3] >> 2;


        // Note that since triagonals and quadragonals must necessarily violate the forward-lateral rule, we dont need to check to see that the third-max is zero in any case
        // TODO: Add extra checks incase this rule is disabled
        // WARNING: If you have disabled my forward-lateral rule, make sure you understand and update this next section of logic

        // Get the absolute value of the maximum offset the piece is trying to make
        int offset_length = offsets_sorted[3];

        // Create a new int array of length 4 to store what directions the piece is moving in
        int[] offset_scaled = new int[4];

        // divide the offsets by the largest offset. Note that since these are ints, our offsets will result in 0 for offset[n] < offset_length, and 1 for offset[n] = offset_length.
        // This will only be used for pieces that need to check to see if they are occluded by other pieces.
        offset_scaled[0] = offsets[0] / offset_length;
        offset_scaled[1] = offsets[1] / offset_length;
        offset_scaled[2] = offsets[2] / offset_length;
        offset_scaled[3] = offsets[3] / offset_length;

        // setup an empty occlusion flag
        bool isOccluded = false;

        // Cast from the starting coordinate along our scaled offsets searching for pieces that occlude its motion
        for (int i = 1; i < offset_length; i++)
        {
            // This should never be out of bounds, the reason being that the max offset must still be on the board, and this will only ever go towards that direction, or in a diagonal
            // if we've gotten this far, we must be moving along a monagonal or diagonal. The scaling effect on the knights move will only strip out the second-max, and diagonals
            // remain on the intended path. If a diagonal is on the board, then moving along that diagonal in either direction its composed of must also be on the board. Additionally
            // we dont ever need to check the target destination, nor do we want to check the starting destination, since we know whats on either square wont occlude the move
            int piece_at = squares[CoordToIndex(xf + offset_scaled[0] * i, yf + offset_scaled[1] * i, zf + offset_scaled[2] * i, wf + offset_scaled[3] * i)];

            // If we arent already occluded, occlude the piece if we found a square blocking it.
            isOccluded = (piece_at != 0) || isOccluded;
        }

        // King move: make sure the two directions of travel are at most 1
        if (piece_type == 1)
        {
            return (offsets_sorted[2] <= 1 && offsets_sorted[3] <= 1);
        }
        // Queen move: make sure the second-max offset is zero, or the same as the first-max offset
        else if (piece_type == 2)
        {
            return (offsets_sorted[2] == offsets_sorted[3] || offsets_sorted[2] == 0) && !isOccluded;
        }
        // Bishop move: make sure the two max offsets are equal. Note they cant be zero otherwise from == to outputs true
        else if (piece_type == 3)
        {
            return (offsets_sorted[2] == offsets_sorted[3]) && !isOccluded;
        }
        // Knights move: make sure first-max is 2, second-max is 1
        else if (piece_type == 4)
        {
            return (offsets_sorted[2] == 1 && offsets_sorted[3] == 2);
        }
        // Rooks move: make sure second-max is 0
        else if (piece_type == 5)
        {
            return (offsets_sorted[2] == 0) && !isOccluded;
        }
        else if (piece_type == 6)
        {
            // again, one of these forward offsets must be zero at this point, so to get either one we can simply add them
            int forward_offset = offsets[1] + offsets[3];

            // flip the forward direction if we are looking at white pawns
            if (color_from == 1)
            {
                forward_offset = -forward_offset;
            }

            // Determine if attacking motions are valid
            if (isDifferentColor)
            {
                return offsets_sorted[2] == 1 && offsets_sorted[3] == 1 && forward_offset == 1;
            }
            // otherwise, allow moving only forward, only if the square moving to is empty.
            else
            {
                return offsets_sorted[2] == 0 && offsets_sorted[3] == 1 && forward_offset == 1 && isToEmpty;
            }
        }

        // If all else fails for some reason, allow the move as long as its actually moving a piece
        return from != to;
    }





    ///////////////////////////////////////
    ///                                 ///
    ///      Board State Methods        ///
    ///                                 ///
    ///////////////////////////////////////


    public void ForcePushToNetwork()
    {
        PushToNetwork();
    }

    // Push board changes to the network.
    // TODO: Try to seperate names + tv-bot status from this to reduce unnecessary network traffic.
    public void PushToNetwork()
    {
        // Claim ownership of the board
        Networking.SetOwner(Networking.LocalPlayer, this.gameObject);

        // Set the networked history to be our current local history
        network_history = (int[])move_history.Clone();

        // print the history to logs for fun
        PrintHistoryToLogs();

        // And request serialization.
        RequestSerialization();
    }


    // Upon receiving new network information
    public override void OnDeserialization()
    {
        // Set tv-bots state from our networked state
        tv_bot.SetStateFromEncoded(tv_bot_state);

        // If we have an ai player
        if (ai_player != null)
        {
            // indicate its networked level
            IndicateLevel();

            // Set the AI itself to the correct value for any master-ship transfers.
            ai_player.SetLevel(AI_level);
        }
        // Merge to the networked history
        MergeToNetworkHistory();

        // print the history to the logs
        PrintHistoryToLogs();

        // Update the names displayed for people joined.
        UpdateNames();
    }


    // Reset the board
    public void ResetBoard()
    {
        // If a piece is currently in motion
        if (has_target)
        {
            // Set its position to the position it should be in
            GameObject old_target = squares_root.transform.GetChild(target_piece).gameObject;
            old_target.transform.position = PosFromIndex(target_piece);
        }

        // Copy the starting state into the squares array
        Array.Copy(start_state, 0, squares, 0, squares.Length);

        // Set pieces from the squares array, grabbing references from ref_mesh array 0 and ref_mats array 0
        SetAllChildrenRendering(squares_root, ref_meshes_00, ref_mats_00);

        // Clear the move history and buffer
        move_history = new int[0];
        move_buffer = new int[0];

        // reset part of tv-bots state. Not sure what this is supposed to be as I think & 4 is incorrect.
        // TODO: Determine correctness of "& 4"
        tv_bot_state = (tv_bot_state & 4) + 0;

        // Set tv bots state from the encoded value
        tv_bot.SetStateFromEncoded(tv_bot_state);

        // And push all the changes to the network.
        PushToNetwork();

        ResetArrow(piece_arrow);
    }


    // Perform a hard sync with the last known network push
    public void HardSync()
    {
        // Reset the board to its starting state
        InitializeSquares();

        // Set pieces from the squares array, grabbing references from ref_mesh array 0 and ref_mats array 0
        SetAllChildrenRendering(squares_root, ref_meshes_00, ref_mats_00);

        // clear the move history and buffer
        move_history = new int[0];
        move_buffer = new int[0];

        // for every move int the networked history
        for (int i = 0; i < network_history.Length; i++)
        {
            // Decode the move info
            int from = DecodeFrom(network_history[i]);
            int to = DecodeTo(network_history[i]);

            // Apply the move (writing to history)
            MakeMove(from, to);
        }
        // When finished, enforce that the move history and move buffer match the networked history. Should actually be unneccesary.
        move_history = (int[])network_history.Clone();
        move_buffer = (int[])network_history.Clone();
    }


    // Attempt to merge states with the networked history.
    public void MergeToNetworkHistory()
    {
        // Figure out the latest index that both arrays agree with one another
        int agreement = DetermineAgreement();

        // Undo moves until we reached the agreement state
        RevertBoardTo(agreement);

        // While the networked move array is longer than (ahead of) our local move history
        while (move_history.Length < network_history.Length)
        {
            // Keep applying moves
            int encoded_move = network_history[move_history.Length];
            int from = DecodeFrom(encoded_move);
            int to = DecodeTo(encoded_move);
            int captured = DecodeCaptured(encoded_move);

            if (network_history.Length != 0)
            {
                MakeMove(from, to);
            }
        }
    }


    // Determing the last time in the array the networked history agrees with the local history
    public int DetermineAgreement()
    {
        int i = 0;
        // If either history length is 0, then i < 0 will be 0 < 0, terminating early.
        // while we still have moves in the networked and move history arrays, and the moves are the same in each, keep incrementing.
        while ((i < move_history.Length) && (i < network_history.Length) && (move_history[i] == network_history[i])) { i++; }

        // return how far we incremented
        return i;
    }


    // Revert board to the specified turn
    public void RevertBoardTo(int target_index)
    {
        // clamp the index to be a valid turn
        int valid_index = Mathf.Clamp(target_index, 0, target_index + 1);

        // undo moves until we reach that index
        while (move_history.Length > valid_index) { UndoMove(); }
    }


    // Master method for Undoing the last move, then pushing to network.
    public void UndoMoveNetworked()
    {
        // If master
        if (Networking.IsMaster)
        {
            // undo the last move and push to network
            UndoMove();
            PushToNetwork();
        }
    }


    // Locally undo a move on the board
    public void UndoMove()
    {
        // If there are moves to be undone
        if (move_history.Length > 0)
        {
            // Get the last move made
            int encoded_move = move_history[move_history.Length - 1];

            // Decode its starting and ending positions, if it captured a piece, and if the piece was promoted
            int from = DecodeFrom(encoded_move);
            int to = DecodeTo(encoded_move);
            int captured = DecodeCaptured(encoded_move);
            int promoted = DecodePromoted(encoded_move);

            // Unmake the move based on above information
            UnmakeMove(from, to, captured, promoted);

            // Shorten the move buffer by one move
            move_buffer = new int[move_history.Length - 1];

            // Fill the move_buffer with the move history (except the last move)
            Array.Copy(move_history, 0, move_buffer, 0, move_buffer.Length);

            // and clone the move_buffer over history, removing the last move made.
            move_history = (int[])move_buffer.Clone();
        }
        if (move_history.Length > 0)
        {
            int move = move_history[move_history.Length - 1];
            int from = DecodeFrom(move);
            int to = DecodeTo(move);
            SetArrow(piece_arrow, from, to);
        }
        else
        {
            ResetArrow(piece_arrow);
        }

    }


    // Make a move on the board
    public void MakeMove(int from, int to)
    {
        // record the piece and color of the piece we are moving
        int piece_moved = squares[from];
        int piece_color = PieceColor(piece_moved);

        // record the piece we are capturing in case we ever need to put it back
        int captured = squares[to];

        // move the piece we are moving to its target coordinate, and clear its starting coordinate
        squares[to] = squares[from];
        squares[from] = 0;

        // calculate the y and w coordinates to check for pawn promotion
        int yt = (to >> 2) & 3;
        int wt = (to >> 6) & 3;

        // Determine if the piece we moved was a pawn.
        bool isPawn = PieceTypeColorless(piece_moved) == 6;

        // If the pawn is white, its promoting coord is 0. Otherwise, it is 3.
        int target_coord = piece_color == 1 ? 0 : 3;

        // check to see the pawn is at its promoting coord along both forward directions
        int isPromoting = (isPawn && (yt == target_coord) && (wt == target_coord)) ? 1 : 0;

        // If we are promoting the piece
        if (isPromoting == 1)
        {
            // Set the piece to be a queen of the specified color. Note that as of right now changing
            // colors can be accomplished by adding an offset of 6.
            squares[to] = 2 + 6 * piece_color;
        }

        // Write our move to the networked history
        WriteHistory(from, to, captured, isPromoting);

        // Update just the to and from squares to save on performance

        // Set pieces from the squares array, grabbing references from ref_mesh array 0 and ref_mats array 0
        SetChildObjectsRendering(squares_root, from, ref_meshes_00, ref_mats_00);
        SetChildObjectsRendering(squares_root, to, ref_meshes_00, ref_mats_00);


        SetArrow(piece_arrow, from, to);

        // Set our new target piece
        target_piece = to;
        AddTarget(0, to, FetchGameobjectsChild(squares_root, to));

        // Grab the square gameobject of the piece we need to grab
        GameObject square = squares_root.transform.GetChild(target_piece).gameObject;

        // and move it to where it is moving from, so we can automatically move it towards its new position
        square.transform.position = PosFromIndex(from);
    }


    // Unmake a move on the board. Basically, undo all the changes made when moving a piece
    public void UnmakeMove(int from, int to, int captured, int promoted)
    {
        // Move the piece back to its starting position
        squares[from] = squares[to];
        if (promoted == 1)
        {
            // if the piece was promoted, demote it to pawn
            int piece_color = PieceColor(squares[from]);
            squares[from] = 6 + 6 * piece_color;
        }

        // Add the captured piece onto the square the capturing piece was
        squares[to] = captured;

        // update the to and from squares on the board
        SetChildObjectsRendering(squares_root, from, ref_meshes_00, ref_mats_00);
        SetChildObjectsRendering(squares_root, to, ref_meshes_00, ref_mats_00);

        // If we are in the process of moving a piece
        if (has_target)
        {
            // Set it to its final destination
            GameObject old_target = squares_root.transform.GetChild(target_piece).gameObject;
            old_target.transform.position = PosFromIndex(target_piece);
        }
        // Target the new piece, that should be the piece that moved on this encoded move.
        target_piece = from;
        has_target = true;

        // and move it to where its moving from (the move's "to coordinate"), so it can be auto moved in update.
        GameObject square = squares_root.transform.GetChild(target_piece).gameObject;
        square.transform.position = PosFromIndex(to);
    }


    // Write a move to history by encoding it into an int, bitwise.
    public void WriteHistory(int from, int to, int captured, int isPromoting)
    {
        // Increase the length of the move buffer by one
        move_buffer = new int[move_history.Length + 1];

        // copy the move history into the move_buffer
        Array.Copy(move_history, 0, move_buffer, 0, move_history.Length);

        // Add the encoded move as the final entry in the move buffer
        move_buffer[move_buffer.Length - 1] = EncodeMove(from, to, captured, isPromoting);

        // clone the buffer over the move history
        move_history = (int[])move_buffer.Clone();
    }





    ///////////////////////////////////////
    ///                                 ///
    ///    Low Level Board Functions    ///
    ///                                 ///
    ///////////////////////////////////////

    // These functions assign references, initialize memory, etc.

    // Instantiate the pieces onto the board. Not really neccesary as I include them already created with the
    // prefab, but leaving this in as it makes for updating the board slightly easier. Recommend hooking up
    // to a button, running, copying new squares, and pasting it into the board rig. Might destroy prefab though
    void InstantiatePieces()
    {
        for (int i = 0; i < squares.Length; i++)
        {
            Vector3 position = PosFromIndex(i);

#if UDON
            //GameObject square_instance = VRCInstantiate(SquareTemplate), position, Quaternion.identity);
            GameObject square_instance = VRCInstantiate(SquareTemplate);
            square_instance.transform.position = position;
            square_instance.transform.rotation = Quaternion.identity;
#else
            GameObject square_instance = Instantiate(SquareTemplate, position, Quaternion.identity);
#endif

            square_instance.transform.parent = squares_root.transform;
            square_instance.name = "Square" + (i / 100 % 10).ToString() + (i / 10 % 10).ToString() + (i % 10).ToString();
        }
    }


    ///////////////////////////////////////
    ///                                 ///
    ///           UI Methods            ///
    ///                                 ///
    ///////////////////////////////////////

    public void UpdateNames()
    {
        white_field.text = white_name;
        black_field.text = black_name;
    }


    // If we have valid text to dump our encoded history into dump the encoded history into the valid text.
    public void PrintHistoryToLogs()
    {
        if (human_log != null)
        {
            human_log.text = encode_history_to_string(true);
        }
        if (comps_log != null)
        {
            comps_log.text = encode_history_to_string(false);
        }
    }


    // Encode the move history into a string to be output.
    public string encode_history_to_string(bool isHumanReadable)
    {
        // We'll be taking values 0-3 and converting them into something resembling chess notation. Easiest
        // way to do this is to make looking up that input as an index return the letter we need.

        // define regular letters
        string letters = "abcd";

        // define reverse letters (for disagreements between chess coordinates and my in-code board coordinates
        string rletters = "dcba";

        // define numbers for forward directions
        string numbers = "4321";

        // Define a string of piece letters to use the same trick as above, but with pieces.
        string piece_chars = "xkqbnrpKQBNRP";

        // Declare an empty string to store output into
        string history = "";

        // Copy the starting state of the board into an array so we can replay the game as we go to get what kind
        // of piece ended up moving.
        Array.Copy(start_state, 0, squares_buffer, 0, start_state.Length);

        // If we are outputting human readable text
        if (isHumanReadable)
        {
            // For every recorded move in history
            for (int i = 0; i < network_history.Length; i++)
            {
                // Get the encoded move
                int encoded = network_history[i];

                // decode the to and from coordinates
                int from = DecodeFrom(encoded);
                int to = DecodeTo(encoded);

                // Get the components of the coordinates
                int xf = (from >> 0) & 3;
                int yf = (from >> 2) & 3;
                int zf = (from >> 4) & 3;
                int wf = (from >> 6) & 3;

                int xt = (to >> 0) & 3;
                int yt = (to >> 2) & 3;
                int zt = (to >> 4) & 3;
                int wt = (to >> 6) & 3;

                // Get the piece that moved, and where it's moving to
                int pf = squares_buffer[from];
                int pt = squares_buffer[to];

                // Update the simulated board state to keep things consistent
                // TODO: Implement pawn promotion here. Doesnt need undo capabilities.
                squares_buffer[to] = squares_buffer[from];
                squares_buffer[from] = 0;

                // Define a string to pad out single and double digits to match triple digit lengths
                string digit_padding = "";

                // Chess groups white / black into a single turn, so we do i / 2. Since chess is 1 indexed instead of 0 indexed, add one.
                // If that is single digit, add a space as padding.
                if (((i / 2) + 1) < 10)
                {
                    digit_padding = digit_padding + " ";
                }
                // If that is signle or double digit, add a nother space as padding.
                if (((i / 2) + 1) < 100)
                {
                    digit_padding = digit_padding + " ";
                }

                // Calculate the white / black turn-pair we are one as a string
                string breaker = digit_padding + ((i / 2) + 1) + ". ";

                // and discard that if we are printing blacks turn, since it should already have been handled on whites
                if (i % 2 == 1)
                {
                    breaker = "";
                }

                // String together the piece that moved, the piece it captured, and its from coordinate.
                string coord1 = "" + piece_chars[pf] + piece_chars[pt] + " " + rletters[xf] + numbers[yf] + letters[zf] + numbers[wf];

                // add in its to coordinate
                string coord2 = "" + rletters[xt] + numbers[yt] + letters[zt] + numbers[wt];

                // add the whole thing to total history, listing the current turn number as breaker if displaying whites.
                history = history + breaker + coord1 + " " + coord2;

                // If we are printing out a black turn, add a newline afterwards
                if (i % 2 == 1)
                {
                    history = history + '\n';
                }
                // Otherwise, print out a slash to seperate black and whites turns.
                else
                {
                    history = history + " / ";
                }
            }
        }
        // If we are not printing human readable text
        else
        {
            // For every encoded move
            for (int i = 0; i < network_history.Length; i++)
            {
                // Get the encoded move
                int encoded = network_history[i];

                // Decode its to and from coordinates
                int from = DecodeFrom(encoded);
                int to = DecodeTo(encoded);

                // break them into components
                int xf = (from >> 0) & 3;
                int yf = (from >> 2) & 3;
                int zf = (from >> 4) & 3;
                int wf = (from >> 6) & 3;

                int xt = (to >> 0) & 3;
                int yt = (to >> 2) & 3;
                int zt = (to >> 4) & 3;
                int wt = (to >> 6) & 3;

                // fetch the correct letters for the chess notation
                string coord1 = "" + rletters[xf] + numbers[yf] + letters[zf] + numbers[wf];
                string coord2 = "" + rletters[xt] + numbers[yt] + letters[zt] + numbers[wt];

                // and tack the from and to coordinates onto the history as strings
                history = history + coord1 + coord2;
            }
        }
        // return the final history string.
        return history;
    }





    ///////////////////////////////////////
    ///                                 ///
    ///      Networking Functions       ///
    ///                                 ///
    ///////////////////////////////////////

    // Determine if we should be hosting the AI. As of right now, just check for mastership.
    // TODO: Find a more clever way of doing this that wont break.
    private bool IsHost()
    {
        return Networking.IsMaster;
    }


    // Check to see if the player is white
    public bool IsPlayerWhite()
    {
        if (Networking.LocalPlayer != null)
        {
            return white_name == Networking.LocalPlayer.displayName;
        }
        return false;
    }


    // Check to see if the player is black
    public bool IsPlayerBlack()
    {
        if (Networking.LocalPlayer != null)
        {
            return black_name == Networking.LocalPlayer.displayName;
        }
        return false;
    }


    // Check to see if the player is white or black
    public bool IsPlayerJoined()
    {
        if (Networking.LocalPlayer != null)
        {
            return (black_name == Networking.LocalPlayer.displayName) || (white_name == Networking.LocalPlayer.displayName);
        }
        return false;
    }


    // Add the player as white
    public void JoinAsWhite()
    {
        if (Networking.LocalPlayer != null)
        {
            // Technically tv_bot_state shouldnt be 2 (mod 4) unless tv_bot is not null, but check just in case. If the bot is in the way of white
            if (tv_bot != null && (tv_bot_state & 3) == 2)
            {
                // move him out of the way.
                tv_bot_state = (tv_bot_state & 4) + 0;

                // Update his local state (the network push comes later)
                tv_bot.SetStateFromEncoded(tv_bot_state);
            }

            // Add the player name as white
            white_name = Networking.LocalPlayer.displayName;

            // display both names
            UpdateNames();

            // push to network
            PushToNetwork();
        }
    }


    // Clear the white namefield so it is empty
    public void ClearWhite()
    {
        if (Networking.LocalPlayer != null)
        {
            // again, the state shouldnt be 2 if tv_bot is null, but just in case. If TV bot is in the way, move him
            if (tv_bot != null && (tv_bot_state & 3) == 2)
            {

                tv_bot_state = (tv_bot_state & 4) + 0;
                tv_bot.SetStateFromEncoded(tv_bot_state);
            }

            // clear the joined as white name
            white_name = "";

            // update the displayed names
            UpdateNames();

            // Push the changes to the network
            PushToNetwork();
        }
    }


    // See above two methods for the next two
    public void JoinAsBlack()
    {
        if (Networking.LocalPlayer != null)
        {
            if ((tv_bot_state & 3) == 1)
            {
                tv_bot_state = (tv_bot_state & 4) + 0;
                tv_bot.SetStateFromEncoded(tv_bot_state);
            }
            black_name = Networking.LocalPlayer.displayName;
            UpdateNames();
            PushToNetwork();
        }
    }


    public void ClearBlack()
    {
        if (Networking.LocalPlayer != null)
        {
            if ((tv_bot_state & 3) == 1)
            {
                tv_bot_state = (tv_bot_state & 4) + 0;
                tv_bot.SetStateFromEncoded(tv_bot_state);
            }
            black_name = "";
            UpdateNames();
            PushToNetwork();

        }
    }





    ///////////////////////////////////////
    ///                                 ///
    ///           AI Control            ///
    ///                                 ///
    ///////////////////////////////////////

    // Process a move request made by the AI. Gets called by chess4DEvalutor upon finding a move.
    public void ProcessAIMove(int encoded_move, int turn_searching_for, bool searching_for_white)
    {
        // Decode the move information
        int from = DecodeFrom(encoded_move);
        int to = DecodeTo(encoded_move);

        // record a boolean to tell whether our move was successful or not.
        bool move_made = false;
        // If we are still on the turn we queued the AI on, and the AI is searching for the correct color
        if (turn_searching_for == turn_ai_queued && searching_for_white == queued_ai_as_white)
        {
            // If the above passed, and the AI is joined as the color its trying to make a move as
            if ((queued_ai_as_white && IsAIWhite()) || (!queued_ai_as_white && IsAIBlack()))
            {
                // And the move its making is valid
                if (IsValidMove(from, to))
                {
                    // And the AI is being run on the hosts client
                    if (IsHost())
                    {
                        // record a succesful move
                        move_made = true;
                        
                        // Make that move
                        MakeMove(from, to);

                        // And push the changes on the board to the network.
                        PushToNetwork();
                    }
                    // Otherwise, tell the player the first thing that went wrong
                    else
                    {
                        Debug.Log("Warning, player requesting AI move is no longer hosting");
                    }
                }
                else
                {
                    Debug.Log("Warning, AI has chosen invalid move. If the state has not updated, then something went wrong");
                }
            }
            else
            {
                Debug.Log("Warning, AI is trying to play when not joined as correct color");
            }
        }
        else
        {
            Debug.Log("Warning, state of game has some how changed while ai was processing");
        }
        
        // If we failed to make a move, put TV bot back to the neutral state and indicate he has stopped thinking.
        // This shouldnt actually be neccesary, since anything that interrupts tv bot should also move him back to the right spot.
        
        // TODO: Fix potential network spam issue with this conditional statement. If TV bot gets locked into an invalid
        // state where it thinks it can move, it could repeatable send failed move requests. SetTVBot calls PushToNetwork(),
        // resulting in potetinal network spamming.
        //if (!move_made)
        //{
        //    SetTVBot(0, false);
        //}

    }


    // Add the AI in as white.
    public void AddAIWhite()
    {
        // If the AI player is not null
        if (ai_player != null)
        {
            // Add his name to white
            white_name = "TV-Bot-9000";

            // Update the displayed names
            UpdateNames();

            // Push to network
            PushToNetwork();
        }
    }


    // See above
    public void AddAIBlack()
    {
        if (ai_player != null)
        {
            black_name = "TV-Bot-9000";
            UpdateNames();
            PushToNetwork();
        }
    }


    // Determine if the AI is playing. Currently done with string name. If username TV-Bot-9000 is taken as
    // a display name, sorry lol. You're an AI player now.
    public bool IsAIPlaying()
    {
        return white_name == "TV-Bot-9000" || black_name == "TV-Bot-9000";
    }


    // Determine if the Ai is white
    public bool IsAIWhite()
    {
        return white_name == "TV-Bot-9000";
    }


    // Determine if the AI is black
    public bool IsAIBlack()
    {
        return black_name == "TV-Bot-9000";
    }


    // Set level of AI. No input paramters as its meant to be called from UI button events.
    // If we are the master of the instance, set the ai level as desired, dispaly the level of the AI,
    // then push our changes to the network.
    public void SetLevel1()
    {
        if (Networking.IsMaster && ai_player != null)
        {
            AI_level = 1;
            ai_player.SetLevel(AI_level);
            IndicateLevel();
            PushToNetwork();
        }
    }


    public void SetLevel2()
    {

        if (Networking.IsMaster && ai_player != null)
        {
            AI_level = 2;
            ai_player.SetLevel(AI_level);
            IndicateLevel();
            PushToNetwork();
        }
    }


    public void SetLevel3()
    {

        if (Networking.IsMaster && ai_player != null)
        {
            AI_level = 3;
            ai_player.SetLevel(AI_level);
            IndicateLevel();
            PushToNetwork();
        }
    }


    public void SetLevel4()
    {

        if (Networking.IsMaster && ai_player != null)
        {
            AI_level = 4;
            ai_player.SetLevel(AI_level);
            IndicateLevel();
            PushToNetwork();
        }
    }


    // For each level indicator, disable the ones that are wrong, enable the right one.
    // Handles out of bounds by terminating early, and invalid id's by disabling all.
    public void IndicateLevel()
    {
        for (int i = 0; i < LevelIndicators.Length; i++)
        {
            LevelIndicators[i].SetActive(AI_level == i + 1);
        }
    }


    // Set TV bot (the AI character)'s state. takes in a position 0-2 (neutral, black, white) and a thinking state.
    // the thinking state enables a loading wheel to let the player know he is thinking.
    public void SetTVBot(int position, bool isThinking)
    {
        // If tv bot exists
        if (tv_bot != null)
        {
            // Encode its state
            tv_bot_state = encode_tv_bot_state(position, isThinking);
            
            // pass the info onto TV bot itself
            tv_bot.SetStateFromEncoded(tv_bot_state);

            // And push to network.
            // TODO: determine if this sometimes causes infinite looping network spam.
            PushToNetwork();
        }

    }


    // Encode tv bots state into an integer, bitwise.
    private int encode_tv_bot_state(int position, bool isThinking)
    {
        int pos = position;
        int thonk = isThinking ? 1 : 0;
        return (pos & 3) + (thonk << 2);
    }





    ///////////////////////////////////////
    ///                                 ///
    ///     Small Helper Functions      ///
    ///                                 ///
    ///////////////////////////////////////

    // Determines the current turn color from history length
    public int DetermineTurnFromHistory()
    {
        return (move_history.Length + 1) & 1;
    }


    // Encodes and decodes info about a move into / from integers, bitwise. Encoding is self documenting.
    public int EncodeMove(int from, int to, int captured, int isPromoting)
    {
        return ((from & 255) << 0) + ((to & 255) << 8) + ((captured & 255) << 16) + ((isPromoting & 1) << 24);
    }
    public int DecodeFrom(int encoded)
    {
        return (encoded >> 0) & 255;
    }
    public int DecodeTo(int encoded)
    {
        return (encoded >> 8) & 255;
    }
    public int DecodeCaptured(int encoded)
    {
        return (encoded >> 16) & 255;
    }
    public int DecodePromoted(int encoded)
    {
        return (encoded >> 24) & 1;
    }


    // Calculates the index of each piece, being in array { none, king, queen, bishop, knight, rook, pawn }
    public int PieceTypeColorless(int piece_type)
    {
        if (piece_type == 0) { return 0; }
        return ((piece_type - 1) % 6) + 1;
    }


    // determines if it is a black piece. Note: counts empty squares as black.
    // Color encoding is as follows : 1-6 is black, 7-12 is white. 0 is empty but usually returns as black.
    public bool isBlackPiece(int piece_id)
    {
        return (piece_id < 7);
    }


    // determine the color of the piece. Note: counts empty squares as black
    public int PieceColor(int piece_type)
    {
        return piece_type / 7;
    }


    // Conversion between data types representing board coordinate. For this board, the coordinate is a 4-vector with values {0, 1, 2, 3}
    // Convert a vector4 coordinate to integer index 0-255.
    public int VectorToIndex(Vector4 coordinate)
    {
        return Mathf.RoundToInt(Vector4.Dot(coordinate, new Vector4(1.0f, 4.0f, 16.0f, 64.0f)));
    }
    

    // Get and return a fresh copy of the square state. Utilized for the AI.
    public int[] GetSquareArray()
    {
        return (int[])squares.Clone();
    }


    // Convert a 4-Vector to an index ranging 0-255
    public int CoordToIndex(int xi, int yi, int zi, int wi)
    {
        return xi + yi * 4 + zi * 16 + wi * 64;
    }


    // Convert an index ranging from 0-255 to a world space coordinate on the board
    public Vector3 PosFromIndex(int i)
    {
        int x = (i >> 0) & 3;
        int y = (i >> 2) & 3;
        int z = (i >> 4) & 3;
        int w = (i >> 6) & 3;

        return PosFromCoord(x, y, z, w);
    }


    // Determine if a Vector-coordinate is outside the bounds of the board.
    public bool IsVectorOutOfBounds(Vector4 vector)
    {
        // We only need to check the min and maximum of the vectors values, so we use native c# Mathf calls rather than checking all of them.
        int min = Mathf.RoundToInt(Mathf.Min(vector.x, vector.y, vector.z, vector.w));
        int max = Mathf.RoundToInt(Mathf.Max(vector.x, vector.y, vector.z, vector.w));
        return min < 0 || max > 3;
    }


    // Logic for snapping to a coordinate. Returns snapped coordinate regardless of success or failure
    public Vector4 SnapToCoordinateVerbose(Vector3 position)
    {
        int x = 0;
        int y = 0;
        int z = 0;
        int w = 0;

        Vector4 coordinate = Vector4.zero;
        Vector3 incoming = position;

        // Nab the previous coordinate in case we want to go back
        Vector4 prev_coord = coordinate;

        // Get the offset from our root object
        Vector3 vec = incoming - root;

        // parsing to integer acts as a floor value, so we'll divde by some offsets to get these two values.
        //int z_co = (int)(Vector3.Dot(vec, up) / z_offset + 0.5f);
        //int w_co = (int)((Vector3.Dot(vec, fw) - xy_offset) / w_offset + 0.5f);

        int z_co = Mathf.RoundToInt(Vector3.Dot(vec, up) / z_offset);
        int w_co = Mathf.RoundToInt((Vector3.Dot(vec, fw) - xy_offset) / w_offset);

        //int t_co = Mathf.RoundToInt(snapped_tm.x);
        //int m_co = Mathf.RoundToInt(snapped_tm.y);

        // Project the pieces coordinate into the zeroth cube
        Vector3 projection = vec - fw * w_co * w_offset;

        // The board starts at a scale of 1, and shrinks by a fixed factor each time. Start at one
        float scaler = 1.0f;

        // On the zeroth level, z_co = 0, and the loop doesnt run. On the first level, it runs once.
        // Each time we go up a board vertical
        for (int i = 0; i < z_co; i++)
        {
            // scale by the constant factor
            scaler *= z_scaling;
        }

        // Project the 3D board in the zeroth w volume to the bottom 2D board
        projection = (projection - 1.5f * (rt + fw) * xy_offset) / scaler + 1.5f * (rt + fw) * xy_offset;

        // Snap the x and y coordinates
        //int x_co = (int)(Vector3.Dot(projection, rt) / xy_offset + 0.5f);
        //int y_co = (int)(Vector3.Dot(projection, fw) / xy_offset + 0.5f);

        int x_co = Mathf.RoundToInt(Vector3.Dot(projection, rt) / xy_offset);
        int y_co = Mathf.RoundToInt(Vector3.Dot(projection, fw) / xy_offset);


        // get all the coordinates into one vector4, and update it into the global variable
        coordinate = new Vector4(x_co, y_co, z_co, w_co);

        // Determine how far away we are from the center of the board
        Vector4 snapping = coordinate - new Vector4(1f, 1f, 1f, 1f) * 1.5f;

        x = x_co;
        y = y_co;
        z = z_co;
        w = w_co;

        return coordinate;
    }


    // Calculate a worldspace coorinate from a supplied set of xyzw coordinates on the board
    public Vector3 PosFromCoord(int xi, int yi, int zi, int wi)
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









    // WIP Section








    ///////////////////////////////////////
    ///                                 ///
    ///     Board Editing Functions     ///
    ///                                 ///
    ///////////////////////////////////////

    // Functions that mostly remain unused, but are helpful for playing around with custom scenarios. Includes an example starting
    // state that is transposed into a human readable layout (with axes indicated in comment), as well as method for converting
    // this layout into a program-readable integer array.

    // Example test state
    int[] test_state0_transposed = new int[256] {



                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,

                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        
                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        
                                        6, 0, 0, 0,  8, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
                                        6, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 12,
                                        6, 0, 0, 0,  0, 0, 6, 0,  0, 0, 0, 0,  0, 0, 0, 0,
                                        6, 0, 0, 0,  0, 0, 0, 6,  0, 0, 0, 0,  0, 0, 0, 12

        
                                        //       w ----->
                                        //       y ->
                                        // z  x 
                                        // |  | 
                                        // |  v 
                                        // |
                                        // | 
                                        // v
                                      };


    // Set the board state to the state defined by test_state0_transposed
    void set_test_state_transpose()
    {
        int[] new_state = (int[])test_state0_transposed.Clone();
        for (int i = 0; i < new_state.Length; i++)
        {
            TransposeInverse(test_state0_transposed, new_state, i);
        }
        SetPiecesToState(new_state);
    }


    int[] Transpose(int[] state, int index)
    {
        int[] new_state = (int[])state.Clone();

        int x = ( index >> 0 ) & 3;
        int y = ( index >> 2 ) & 3;
        int z = ( index >> 4 ) & 3;
        int w = ( index >> 6 ) & 3;

        int x0 = y;
        int y0 = w;
        int z0 = x;
        int w0 = z;

        int index1 = ( ((x & 3) << 0 ) +
                       ((y & 3) << 2 ) +
                       ((z & 3) << 4 ) +
                       ((w & 3) << 6 ) ) ;

        int index0 = ( ((x0 & 3) << 0 ) +
                       ((y0 & 3) << 2 ) +
                       ((z0 & 3) << 4 ) +
                       ((w0 & 3) << 6 ) ) ;

        new_state[index0] = state[index1];

        return new_state;
    }


    int[] TransposeInverse(int[] state_old, int[] state_new, int index)
    {
        int[] new_state = state_new;

        //int x0 = y;
        //int y0 = w;
        //int z0 = x;
        //int w0 = z;

        int x = (index >> 0) & 3;
        int y = (index >> 2) & 3;
        int z = (index >> 4) & 3;
        int w = (index >> 6) & 3;

        int x0 = z;
        int y0 = x;
        int z0 = w;
        int w0 = y;

        int index1 = (((x & 3) << 0) +
                       ((y & 3) << 2) +
                       ((z & 3) << 4) +
                       ((w & 3) << 6));

        int index0 = (((x0 & 3) << 0) +
                       ((y0 & 3) << 2) +
                       ((z0 & 3) << 4) +
                       ((w0 & 3) << 6));

        new_state[index0] = state_old[index1];

        return new_state;
    }




    ///////////////////////////////////////
    ///                                 ///
    ///   Logic Pulled from Evaluator   ///
    ///                                 ///
    ///////////////////////////////////////

    // In order for the board to determine if a player is in check, it makes sense for it to have access to code specific to Chess 4D Evaluator. Ideally,
    // I'd split these into static methods, but I'm unsure if Udon# supports it, and at the moment this makes sense until I can figure out how to seperate.

    // Piece Movement encoding
    Vector4[] pawn_moves = new Vector4[20]
                       {
                                    new Vector4( 0, 1, 0, 0),
                                    new Vector4( 0,-1, 0, 0),
                                    new Vector4( 0, 0, 0, 1),
                                    new Vector4( 0, 0, 0,-1),
                                    new Vector4( 1, 1, 0, 0),
                                    new Vector4( 1,-1, 0, 0),
                                    new Vector4(-1, 1, 0, 0),
                                    new Vector4(-1,-1, 0, 0),
                                    new Vector4( 0, 1, 1, 0),
                                    new Vector4( 0,-1, 1, 0),
                                    new Vector4( 0, 1,-1, 0),
                                    new Vector4( 0,-1,-1, 0),
                                    new Vector4( 1, 0, 0, 1),
                                    new Vector4( 1, 0, 0,-1),
                                    new Vector4(-1, 0, 0, 1),
                                    new Vector4(-1, 0, 0,-1),
                                    new Vector4( 0, 0, 1, 1),
                                    new Vector4( 0, 0, 1,-1),
                                    new Vector4( 0, 0,-1, 1),
                                    new Vector4( 0, 0,-1,-1)
                       };
    Vector4[] knight_moves = new Vector4[32]
                            {
                                    new Vector4( 1, 2, 0, 0),
                                    new Vector4( 1,-2, 0, 0),
                                    new Vector4(-1, 2, 0, 0),
                                    new Vector4(-1,-2, 0, 0),

                                    new Vector4( 2, 1, 0, 0),
                                    new Vector4( 2,-1, 0, 0),
                                    new Vector4(-2, 1, 0, 0),
                                    new Vector4(-2,-1, 0, 0),

                                    new Vector4( 1, 0, 0, 2),
                                    new Vector4( 1, 0, 0,-2),
                                    new Vector4(-1, 0, 0, 2),
                                    new Vector4(-1, 0, 0,-2),

                                    new Vector4( 2, 0, 0, 1),
                                    new Vector4( 2, 0, 0,-1),
                                    new Vector4(-2, 0, 0, 1),
                                    new Vector4(-2, 0, 0,-1),

                                    new Vector4( 0, 2, 1, 0),
                                    new Vector4( 0, 2,-1, 0),
                                    new Vector4( 0,-2, 1, 0),
                                    new Vector4( 0,-2,-1, 0),

                                    new Vector4( 0, 1, 2, 0),
                                    new Vector4( 0, 1,-2, 0),
                                    new Vector4( 0,-1, 2, 0),
                                    new Vector4( 0,-1,-2, 0),

                                    new Vector4( 0, 0, 1, 2),
                                    new Vector4( 0, 0, 1,-2),
                                    new Vector4( 0, 0,-1,-2),
                                    new Vector4( 0, 0, 1,-2),

                                    new Vector4( 0, 0, 2, 1),
                                    new Vector4( 0, 0, 2,-1),
                                    new Vector4( 0, 0,-2, 1),
                                    new Vector4( 0, 0,-2,-1)


                            };
    Vector4[] rook_moves = new Vector4[24]
                            {
                                    new Vector4( 1, 0, 0, 0),
                                    new Vector4( 2, 0, 0, 0),
                                    new Vector4( 3, 0, 0, 0),
                                    new Vector4(-1, 0, 0, 0),
                                    new Vector4(-2, 0, 0, 0),
                                    new Vector4(-3, 0, 0, 0),

                                    new Vector4( 0, 1, 0, 0),
                                    new Vector4( 0, 2, 0, 0),
                                    new Vector4( 0, 3, 0, 0),
                                    new Vector4( 0,-1, 0, 0),
                                    new Vector4( 0,-2, 0, 0),
                                    new Vector4( 0,-3, 0, 0),

                                    new Vector4( 0, 0, 1, 0),
                                    new Vector4( 0, 0, 2, 0),
                                    new Vector4( 0, 0, 3, 0),
                                    new Vector4( 0, 0,-1, 0),
                                    new Vector4( 0, 0,-2, 0),
                                    new Vector4( 0, 0,-3, 0),

                                    new Vector4( 0, 0, 0, 1),
                                    new Vector4( 0, 0, 0, 2),
                                    new Vector4( 0, 0, 0, 3),
                                    new Vector4( 0, 0, 0,-1),
                                    new Vector4( 0, 0, 0,-2),
                                    new Vector4( 0, 0, 0,-3)
                            };
    Vector4[] bishop_moves = new Vector4[48]
                        {
                                    new Vector4( 1, 1, 0, 0),
                                    new Vector4( 2, 2, 0, 0),
                                    new Vector4( 3, 3, 0, 0),

                                    new Vector4(-1, 1, 0, 0),
                                    new Vector4(-2, 2, 0, 0),
                                    new Vector4(-3, 3, 0, 0),

                                    new Vector4( 1,-1, 0, 0),
                                    new Vector4( 2,-2, 0, 0),
                                    new Vector4( 3,-3, 0, 0),

                                    new Vector4(-1,-1, 0, 0),
                                    new Vector4(-2,-2, 0, 0),
                                    new Vector4(-3,-3, 0, 0),



                                    new Vector4( 1, 0, 0, 1),
                                    new Vector4( 2, 0, 0, 2),
                                    new Vector4( 3, 0, 0, 3),

                                    new Vector4(-1, 0, 0, 1),
                                    new Vector4(-2, 0, 0, 2),
                                    new Vector4(-3, 0, 0, 3),

                                    new Vector4( 1, 0, 0,-1),
                                    new Vector4( 2, 0, 0,-2),
                                    new Vector4( 3, 0, 0,-3),

                                    new Vector4(-1, 0, 0,-1),
                                    new Vector4(-2, 0, 0,-2),
                                    new Vector4(-3, 0, 0,-3),



                                    new Vector4( 0, 1, 1, 0),
                                    new Vector4( 0, 2, 2, 0),
                                    new Vector4( 0, 3, 3, 0),

                                    new Vector4( 0,-1, 1, 0),
                                    new Vector4( 0,-2, 2, 0),
                                    new Vector4( 0,-3, 3, 0),

                                    new Vector4( 0, 1,-1, 0),
                                    new Vector4( 0, 2,-2, 0),
                                    new Vector4( 0, 3,-3, 0),

                                    new Vector4( 0,-1,-1, 0),
                                    new Vector4( 0,-2,-2, 0),
                                    new Vector4( 0,-3,-3, 0),



                                    new Vector4( 0, 0, 1, 1),
                                    new Vector4( 0, 0, 2, 2),
                                    new Vector4( 0, 0, 3, 3),

                                    new Vector4( 0, 0,-1, 1),
                                    new Vector4( 0, 0,-2, 2),
                                    new Vector4( 0, 0,-3, 3),

                                    new Vector4( 0, 0, 1,-1),
                                    new Vector4( 0, 0, 2,-2),
                                    new Vector4( 0, 0, 3,-3),

                                    new Vector4( 0, 0,-1,-1),
                                    new Vector4( 0, 0,-2,-2),
                                    new Vector4( 0, 0,-3,-3)
                        };
    Vector4[] king_moves = new Vector4[24]
                        {
                                    new Vector4( 1, 0, 0, 0),
                                    new Vector4(-1, 0, 0, 0),

                                    new Vector4( 0, 1, 0, 0),
                                    new Vector4( 0,-1, 0, 0),

                                    new Vector4( 0, 0, 1, 0),
                                    new Vector4( 0, 0,-1, 0),

                                    new Vector4( 0, 0, 0, 1),
                                    new Vector4( 0, 0, 0,-1),

                                    new Vector4( 1, 1, 0, 0),
                                    new Vector4(-1, 1, 0, 0),
                                    new Vector4( 1,-1, 0, 0),
                                    new Vector4(-1,-1, 0, 0),

                                    new Vector4( 1, 0, 0, 1),
                                    new Vector4(-1, 0, 0, 1),
                                    new Vector4( 1, 0, 0,-1),
                                    new Vector4(-1, 0, 0,-1),

                                    new Vector4( 0, 1, 1, 0),
                                    new Vector4( 0,-1, 1, 0),
                                    new Vector4( 0, 1,-1, 0),
                                    new Vector4( 0,-1,-1, 0),

                                    new Vector4( 0, 0, 1, 1),
                                    new Vector4( 0, 0,-1, 1),
                                    new Vector4( 0, 0, 1,-1),
                                    new Vector4( 0, 0,-1,-1),
                        };



    // HEAVY logic stuff
    public GameObject CheckRequestCursor;


    public int[] move_array = new int[256];
    int move_count = 0;
    public int[] board_buffer0 = new int[256];
    int bbCount0 = 0;

    public void SendCheckRequestWhite()
    {
        SendCheckRequest(CheckRequestCursor.transform.position, 1);
    }


    public void SendCheckRequest(Vector3 position, int color)
    {
        Vector4 coord = SnapToCoordinateVerbose(position);
        bool isValidCoord = !IsVectorOutOfBounds(coord);
        LoadBoardBuffer(VectorToIndex(coord), color);
        Debug.Log("Board buffer has been filled");
        PrintMoveArray();
    }

   

    public void PrintMoveArray()
    {
        for (int i = 0; i < move_count; i++)
        {
            int target_index = DecodeTo(move_array[i]);
            int x = (target_index >> 0) & 3;
            int y = (target_index >> 2) & 3;
            int z = (target_index >> 4) & 3;
            int w = (target_index >> 6) & 3;

            


            Debug.Log("Piece " + board_buffer0[target_index] + " found at "+ "search coord: (" + x + ", " + y + ", " + z + ", " + w + ")");
        }
    }

    // Encodes info about moves into an integer for easier storage and manipulation
    // first 8 bits are starting square, next 8 are target square, and after that is the piece it captured (including 0 = empty)
    public int encode_movement(int from, int to, int piece_captured)
    {
        return ((from & 255) << 0) + ((to & 255) << 8) + ((piece_captured & 255) << 16);
    }

    // extract and return the bits in which the starting square of a movement are encoded into
    public int decode_from(int movement)
    {
        return ((movement >> 0) & 255);
    }

    // extract and return the bits in which the landing square of a movement are encoded into
    public int decode_to(int movement)
    {
        return ((movement >> 8) & 255);
    }

    // extract and return the bits in which the captured piece of a movement are encoded into
    public int decode_captured(int movement)
    {
        return ((movement >> 16) & 255);
    }

    private void ClearMoveBuffer()
    {
        move_count = 0;
    }

    private void FillMoveBuffer(int[] state, int square)
    {
        //pieces_count = 0;

        move_count = 0;
        move_array[0] = NULL;


        int piece = state[square];
        int piece_type = PieceTypeColorless(piece);
        int piece_color = PieceColor(piece);

        int px = (square >> 0) & 3;
        int py = (square >> 2) & 3;
        int pz = (square >> 4) & 3;
        int pw = (square >> 6) & 3;
        int target_index = 0;

        int sx = px;
        int sy = py;
        int sz = pz;
        int sw = pw;

        if (piece_type == 1)
        {
            //int stop_watch_start = GetTime();

            Vector4 pv = new Vector4(px, py, pz, pw);
            Vector4 sv;
            for (int i = 0; i < 24; i++)
            {
                sv = pv + king_moves[i];
                if (!IsVectorOutOfBounds(sv))
                {
                    target_index = VectorToIndex(sv);
                    int result = state[target_index];
                    if (result == 0 || isBlackPiece(result) != isBlackPiece(piece))
                    {
                        int encoded_movement = encode_movement(square, target_index, result);
                        move_array[move_count] = encoded_movement;
                        move_count++;
                    }
                }
            }


            //int stop_watch_end = GetTime();
            //time_spent_kings += (stop_watch_end - stop_watch_start);
        }
        else if (piece_type == 2)
        {
            //int stop_watch_start = GetTime();

            Vector4 pv = new Vector4(px, py, pz, pw);
            Vector4 sv;
            for (int i = 0; i < 48; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    sv = pv + bishop_moves[i + j];
                    if (!IsVectorOutOfBounds(sv))
                    {
                        target_index = VectorToIndex(sv);
                        int result = state[target_index];
                        if (result == 0)
                        {
                            int encoded_movement = encode_movement(square, target_index, result);
                            move_array[move_count] = encoded_movement;
                            move_count++;
                        }
                        else
                        {
                            if (isBlackPiece(result) != isBlackPiece(piece))
                            {
                                int encoded_movement = encode_movement(square, target_index, result);
                                move_array[move_count] = encoded_movement;
                                move_count++;
                            }
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            for (int i = 0; i < 24; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    sv = pv + rook_moves[i + j];
                    if (!IsVectorOutOfBounds(sv))
                    {
                        target_index = VectorToIndex(sv);
                        int result = state[target_index];
                        if (result == 0)
                        {
                            int encoded_movement = encode_movement(square, target_index, result);
                            move_array[move_count] = encoded_movement;
                            move_count++;
                        }
                        else
                        {
                            if (isBlackPiece(result) != isBlackPiece(piece))
                            {
                                int encoded_movement = encode_movement(square, target_index, result);
                                move_array[move_count] = encoded_movement;
                                move_count++;
                            }
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            //int stop_watch_end = GetTime();
            //time_spent_queens += (stop_watch_end - stop_watch_start);
        }
        else if (piece_type == 3)
        {
            //int stop_watch_start = GetTime();


            Vector4 pv = new Vector4(px, py, pz, pw);
            Vector4 sv;
            for (int i = 0; i < 48; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    sv = pv + bishop_moves[i + j];
                    if (!IsVectorOutOfBounds(sv))
                    {
                        target_index = VectorToIndex(sv);
                        int result = state[target_index];
                        if (result == 0)
                        {
                            int encoded_movement = encode_movement(square, target_index, result);
                            move_array[move_count] = encoded_movement;
                            move_count++;
                        }
                        else
                        {
                            if (isBlackPiece(result) != isBlackPiece(piece))
                            {
                                int encoded_movement = encode_movement(square, target_index, result);
                                move_array[move_count] = encoded_movement;
                                move_count++;
                            }
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            //int stop_watch_end = GetTime();
            //time_spent_bishops += (stop_watch_end - stop_watch_start);
        }
        else if (piece_type == 4)
        {
            //int stop_watch_start = GetTime();

            Vector4 pv = new Vector4(px, py, pz, pw);
            Vector4 sv;

            for (int i = 0; i < 32; i++)
            {
                sv = pv + knight_moves[i];
                if (!IsVectorOutOfBounds(sv))
                {
                    target_index = VectorToIndex(sv);
                    int result = state[target_index];
                    if (result == 0 || isBlackPiece(result) != isBlackPiece(piece))
                    {
                        // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                        int encoded_movement = encode_movement(square, target_index, result);
                        move_array[move_count] = encoded_movement;
                        move_count++;
                    }
                }
            }

            //int stop_watch_end = GetTime();
            //time_spent_knights += (stop_watch_end - stop_watch_start);
        }
        else if (piece_type == 5)
        {
            //int stop_watch_start = GetTime();

            Vector4 pv = new Vector4(px, py, pz, pw);
            Vector4 sv;
            for (int i = 0; i < 24; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    sv = pv + rook_moves[i + j];
                    if (!IsVectorOutOfBounds(sv))
                    {
                        target_index = VectorToIndex(sv);
                        int result = state[target_index];
                        if (result == 0)
                        {
                            int encoded_movement = encode_movement(square, target_index, result);
                            move_array[move_count] = encoded_movement;
                            move_count++;
                        }
                        else
                        {
                            if (isBlackPiece(result) != isBlackPiece(piece))
                            {
                                int encoded_movement = encode_movement(square, target_index, result);
                                move_array[move_count] = encoded_movement;
                                move_count++;
                            }
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }


            //int stop_watch_end = GetTime();
            //time_spent_rooks += (stop_watch_end - stop_watch_start);
        }
        else if (piece_type == 6)
        {
            //int stop_watch_start = GetTime();

            Vector4 pv = new Vector4(px, py, pz, pw);
            Vector4 sv;

            for (int i = 0; i < 2; i++)
            {
                sv = pv + pawn_moves[2 * i + piece_color];

                target_index = VectorToIndex(sv);
                if (!IsVectorOutOfBounds(sv) && state[target_index] == 0)
                {
                    int encoded_movement = encode_movement(square, target_index, state[target_index]);
                    move_array[move_count] = encoded_movement;
                    move_count++;
                }
            }

            for (int i = 2; i < 10; i++)
            {
                sv = pv + pawn_moves[2 * i + piece_color];
                target_index = VectorToIndex(sv);
                if (!IsVectorOutOfBounds(sv) && state[target_index] != 0 && isBlackPiece(state[target_index]) != isBlackPiece(piece))
                {
                    move_array[move_count] = encode_movement(square, target_index, state[target_index]);
                    move_count++;
                }
            }

            //int stop_watch_end = GetTime();
            //time_spent_pawns += (stop_watch_end - stop_watch_start);
        }
    }


    public void LoadBoardBuffer(int local_square, int color)
    {
        Array.Copy(squares, 0, board_buffer0, 0, squares.Length);
        board_buffer0[local_square] = 2 + 6 * color;
        FillMoveBuffer(board_buffer0, local_square);
    }





}
