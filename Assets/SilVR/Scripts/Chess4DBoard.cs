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
    public Chess4DVisualizer visualizer;


    public GameObject selection_cursor;
    public GameObject SquareTemplate;

    public GameObject ReferencePieces;
    public MeshFilter[] referenceMeshes;
    public MeshRenderer[] referenceRenderers;

    public Chess4DEvaluator ai_player;
    public TVBotRig tv_bot;

    public Text white_field;
    public Text black_field;

    int MESH_OFFSET = 0;
    int MESH_COUNT = 6;

    int RENDERER_OFFSET = 6;
    int RENDERER_COUNT = 2;

    int NULL = 1024;
    int selected_square = 1024;


    GameObject squares_root;
    int[] squares = new int[256];

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


    public bool queued_ai_as_white = true;
    public int turn_ai_queued = 0;


    public void SetTVBot(int position, bool isThinking)
    {
        if (tv_bot != null)
        {
            tv_bot_state = encode_tv_bot_state(position, isThinking);
            tv_bot.SetStateFromEncoded(tv_bot_state);
            PushToNetwork();
        }
        
    }

    private int encode_tv_bot_state(int position, bool isThinking)
    {
        int pos = position;
        int thonk = isThinking ? 1 : 0;
        return (pos & 3) + (thonk << 2);
    }


    private bool IsHost()
    {
        return Networking.IsMaster;
        //bool isJoined = IsPlayerJoined();
        //bool isMaster = Networking.IsMaster;
        //if (isJoined)
        //{
        //    return true;
        //}
        //else if (IsAIBlack() && IsAIWhite())
        //{
        //    return isMaster;
        //}
        //return false;
    }


    // Update is called once per frame
    void Update()
    {
        
        if (ai_player != null && !ai_player.IsEvaluatorBusy())
        {
            int current_turn = move_history.Length;
            bool isWhite = DetermineTurnFromHistory() == 1;
            if (( isWhite && IsAIWhite() ) || ( !isWhite && IsAIBlack() ))
            {
                if (IsHost())
                {
                    turn_ai_queued = current_turn;
                    queued_ai_as_white = isWhite;
                    ai_player.QueueMove(current_turn, isWhite);
                }
            }
        }
        if (has_target)
        {
            GameObject square = squares_root.transform.GetChild(target_piece).gameObject; ;
            Vector3 new_pos = Vector3.MoveTowards(square.transform.position, PosFromIndex(target_piece), move_speed * (Time.deltaTime));
            //if (Vector3.Distance(square.transform.position, new_pos) < 0.01f)
            //{
            //    has_target = false;
            //}
            square.transform.position = new_pos;
        }
    }
    public void ProcessAIMove(int encoded_move, int turn_searching_for, bool searching_for_white)
    {
        int from = DecodeFrom(encoded_move);
        int to = DecodeTo(encoded_move);
        bool move_made = false;
        if (turn_searching_for == turn_ai_queued && searching_for_white == queued_ai_as_white)
        {
            if ( (queued_ai_as_white && IsAIWhite()) || (!queued_ai_as_white && IsAIBlack()) )
            {
                if (IsValidMove(from, to))
                {
                    if ( IsHost() )
                    {
                        move_made = true;
                        MakeMove(from, to);
                        PushToNetwork();
                    }
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
        if (!move_made)
        {
            SetTVBot(0, false);
        }

    }

    public void OnMoveMade()
    {
        int current_turn = DetermineTurnFromHistory();
        if (current_turn == 0)
        {
            IndicateBlacksTurn();
            if (IsAIBlack())
            {
                turn_ai_queued = move_history.Length;
                queued_ai_as_white = false;
                ai_player.QueueMove(turn_ai_queued, queued_ai_as_white);
            }
        }
        else
        {
            IndicateWhitesTurn();
            if (IsAIWhite())
            {
                turn_ai_queued = move_history.Length;
                queued_ai_as_white = true;
                ai_player.QueueMove(turn_ai_queued, queued_ai_as_white);
            }
        }


    }
    public void IndicateWhitesTurn()
    {

    }
    public void IndicateBlacksTurn()
    {

    }


    public void StartGame()
    {
        OnMoveMade();
    }
    public bool IsPlayerWhite()
    {
        if (Networking.LocalPlayer != null)
        {
            return white_name == Networking.LocalPlayer.displayName;
        }
        return false;
    }
    public bool IsPlayerBlack()
    {
        if (Networking.LocalPlayer != null)
        {
            return black_name == Networking.LocalPlayer.displayName;
        }
        return false;
    }
    public bool IsPlayerJoined()
    {
        if (Networking.LocalPlayer != null)
        {
            return (black_name == Networking.LocalPlayer.displayName) || (white_name == Networking.LocalPlayer.displayName);
        }
        return false;
    }


    public bool IsAIPlaying()
    {
        return white_name == "TV-Bot-9000" || black_name == "TV-Bot-9000";
    }
    public bool IsAIWhite()
    {
        return white_name == "TV-Bot-9000";
    }
    public bool IsAIBlack()
    {
        return black_name == "TV-Bot-9000";
    }

    public void UpdateNames()
    {
        white_field.text = white_name;
        black_field.text = black_name;
    }

    public void ClearWhite()
    {
        if (Networking.LocalPlayer != null)
        {
            if ( (tv_bot_state & 3) == 2)
            {
                tv_bot_state = (tv_bot_state & 4) + 0;
                tv_bot.SetStateFromEncoded(tv_bot_state);
            }
            white_name = "";
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

    public void JoinAsWhite()
    {
        if (Networking.LocalPlayer != null)
        {
            if ((tv_bot_state & 3) == 2)
            {
                tv_bot_state = (tv_bot_state & 4) + 0;
                tv_bot.SetStateFromEncoded(tv_bot_state);
            }
            white_name = Networking.LocalPlayer.displayName;
            UpdateNames();
            PushToNetwork();
        }
    }
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
    public void AddAIWhite()
    {
        if (ai_player != null)
        {
            white_name = "TV-Bot-9000";
            UpdateNames();
            PushToNetwork();
        }
    }
    public void AddAIBlack()
    {
        if (ai_player != null)
        {
            black_name = "TV-Bot-9000";
            UpdateNames();
            PushToNetwork();
        }
    }




    public int DetermineTurnFromHistory()
    {
        return (move_history.Length + 1) & 1;
    }

    public void UndoMoveNetworked()
    {
        UndoMove();
        PushToNetwork();
    }
    public void PushToNetwork()
    {
        Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        network_history = (int[])move_history.Clone();
        RequestSerialization();
    }
    public override void OnDeserialization()
    {
        tv_bot.SetStateFromEncoded(tv_bot_state);
        MergeToNetworkHistory();
        UpdateNames();
    }
    public void MergeToNetworkHistory()
    {
        int agreement = DetermineAgreement();
        RevertBoardTo(agreement);
        while (move_history.Length < network_history.Length)
        {
            int encoded_move = network_history[move_history.Length];
            int from = DecodeFrom(encoded_move);
            int to = DecodeTo(encoded_move);
            int captured = DecodeCaptured(encoded_move);
            MakeMove(from, to);
        }
    }
    public int DetermineAgreement()
    {
        int i = 0;
        // If either history length is 0, then i < 0 will be 0 < 0, terminating early
        while (i < move_history.Length && i < network_history.Length && move_history[i] == network_history[i]) { i++; }
        return i;
    }
    public void RevertBoardTo(int target_index)
    {
        int valid_index = Mathf.Clamp(target_index + 1, 0, target_index + 2);
        while (move_history.Length > valid_index) { UndoMove(); }
    }

    public void UndoMove()
    {
        if (move_history.Length > 0)
        {
            int encoded_move = move_history[move_history.Length - 1];
            int from = DecodeFrom(encoded_move);
            int to = DecodeTo(encoded_move);
            int captured = DecodeCaptured(encoded_move);
            UnmakeMove(from, to, captured);
            move_buffer = new int[move_history.Length - 1];
            Array.Copy(move_history, 0, move_buffer, 0, move_buffer.Length);
            move_history = (int[])move_buffer.Clone();
        }
    }

    public void MakeMove(int from, int to)
    {
        int captured = squares[to];
        squares[to] = squares[from];
        squares[from] = 0;
        WriteHistory(from, to, captured);

        SetPieceFromSquares(from);
        SetPieceFromSquares(to);
        if (has_target)
        {
            GameObject old_target = squares_root.transform.GetChild(target_piece).gameObject;
            old_target.transform.position = PosFromIndex(target_piece);
        }
        target_piece = to;
        has_target = true;
        GameObject square = squares_root.transform.GetChild(target_piece).gameObject;
        square.transform.position = PosFromIndex(from);

    }

    public void WriteHistory(int from, int to, int captured)
    {
        move_buffer = new int[move_history.Length + 1];
        Array.Copy(move_history, 0, move_buffer, 0, move_history.Length);
        move_buffer[move_buffer.Length - 1] = EncodeMove(from, to, captured);
        move_history = (int[])move_buffer.Clone();
    }

    public int EncodeMove(int from, int to, int captured)
    {
        return ((from & 255) << 0) + ((to & 255) << 8) + ((captured & 255) << 16);
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


    public void OnInterfaceInteract(Vector3 position)
    {
        Vector4 coord = SnapToCoordinateVerbose(position);
        bool isValidCoord = !IsVectorOutOfBounds(coord);
        bool isSquareSelected = selected_square != NULL;

        if ( isValidCoord )
        {
            int square = VectorToIndex(coord);
            int piece = GetPiece(square);
            int color = PieceColor(piece);
            bool square_empty = piece == 0;
            int piece_type = PieceTypeColorless(piece);

            if (!isSquareSelected && !square_empty)
            {
                visualizer.SetVisualizerState(coord, piece_type, color);
                selected_square = square;
            }
            else if (isSquareSelected)
            {
                if (IsValidMove(selected_square, square))
                {
                    MakeMove(selected_square, square);
                    selected_square = NULL;
                    visualizer.SetVisualizerState(coord, 0, 0);
                    PushToNetwork();
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

    }
    public int PieceTypeColorless(int piece_type)
    {
        if (piece_type == 0) { return 0; }
        return ((piece_type - 1) % 6) + 1;
    }
    public bool isBlackPiece(int piece_id)
    {
        return (piece_id < 7);
    }
    public int PieceColor(int piece_type)
    {
        return piece_type / 7;
    }

    public bool IsPieceSelected()
    {
        return selected_square != NULL;
    }

    public void DeselectPiece()
    {
        selected_square = NULL;
    }

    public int CurrentTurn()
    {
        return move_history.Length;
    }
    //public Chess4DEvaluator evaluator;
    public bool SelectPieceByIndex(int square)
    {
        if (0 <= square && square < squares.Length)
        {
            if (squares[square] != 0)
            {
                selected_square = square;
                return true;
            }
        }

        DeselectPiece();
        return false;
    }

    public int VectorToIndex(Vector4 coordinate)
    {
        return Mathf.RoundToInt(Vector4.Dot(coordinate, new Vector4(1.0f, 4.0f, 16.0f, 64.0f)));
    }

    public bool SelectPieceByVector(Vector4 coord)
    {
        if (!IsVectorOutOfBounds(coord))
        {
            int square = VectorToIndex(coord);
            if (squares[square] != 0)
            {
                selected_square = square;
                return true;
            }
        }
        DeselectPiece();
        return false;
    }

    public int GetPiece(int square)
    {
        return squares[square];
    }

    public bool IsValidMove(int from, int to)
    {
        bool isActuallyMove = from != to;
        if (!isActuallyMove) { return false; }

        int piece_id_from = squares[from];
        int piece_id_to = squares[to];

        bool isMovingPiece = piece_id_from != 0;
        if (!isMovingPiece) { return false; }

        int piece_type = PieceTypeColorless(piece_id_from);

        int color_from = PieceColor(piece_id_from);
        int color_to = PieceColor(piece_id_to);

        bool isPlayersTurn = color_from == DetermineTurnFromHistory();
        if (!isPlayersTurn) { return false; }

        bool isToEmpty = piece_id_to == 0;
        bool isDifferentColor = color_from != color_to && !isToEmpty;
        bool isTargetValid = isToEmpty || isDifferentColor;

        if (!isTargetValid) { return false; }

        int xf = (from >> 0) & 3;
        int yf = (from >> 2) & 3;
        int zf = (from >> 4) & 3;
        int wf = (from >> 6) & 3;

        int xt = (to >> 0) & 3;
        int yt = (to >> 2) & 3;
        int zt = (to >> 4) & 3;
        int wt = (to >> 6) & 3;

        int[] offsets = new int[4];
        offsets[0] = xt - xf;
        offsets[1] = yt - yf;
        offsets[2] = zt - zf;
        offsets[3] = wt - wf;

        bool violates_forward_forward = (offsets[1] * offsets[3]) != 0;
        bool violates_lateral_lateral = (offsets[0] * offsets[2]) != 0;

        bool violates_forward_lateral_rule = violates_forward_forward || violates_lateral_lateral;
        if (violates_forward_lateral_rule) { return false; }

        int[] offsets_sorted = new int[4];

        // encode the index of the offsets into the two least significant bits
        offsets_sorted[0] = (Mathf.Abs(offsets[0]) << 2) + 0;
        offsets_sorted[1] = (Mathf.Abs(offsets[1]) << 2) + 1;
        offsets_sorted[2] = (Mathf.Abs(offsets[2]) << 2) + 2;
        offsets_sorted[3] = (Mathf.Abs(offsets[3]) << 2) + 3;

        int min = Mathf.Min(offsets_sorted);
        int max = Mathf.Max(offsets_sorted);

        int min_index = min & 3;
        int max_index = max & 3;

        int buffer = offsets_sorted[0];
        offsets_sorted[0] = offsets_sorted[min_index];
        offsets_sorted[min_index] = buffer;

        if (max_index == 0) { max_index = min_index; }

        buffer = offsets_sorted[3];
        offsets_sorted[3] = offsets_sorted[max_index];
        offsets_sorted[max_index] = buffer;

        int a = offsets_sorted[1];
        int b = offsets_sorted[2];

        offsets_sorted[1] = Mathf.Min(a, b);
        offsets_sorted[2] = Mathf.Max(a, b);

        // Strip out the encoded indices
        offsets_sorted[0] = offsets_sorted[0] >> 2;
        offsets_sorted[1] = offsets_sorted[1] >> 2;
        offsets_sorted[2] = offsets_sorted[2] >> 2;
        offsets_sorted[3] = offsets_sorted[3] >> 2;
        // Note that since triagonals and quadragonals must necessarily violate the forward-lateral rule, we dont need to check to see that the third-max is zero in any case

        Debug.Log("Offsets  : ( " + offsets_sorted[0] + ", " + offsets_sorted[1] + ", " + offsets_sorted[2] + ", " + offsets_sorted[3] + " )");

        int offset_length = offsets_sorted[3];
        
        int[] offset_scaled = new int[4];
        offset_scaled[0] = offsets[0] / offset_length;
        offset_scaled[1] = offsets[1] / offset_length;
        offset_scaled[2] = offsets[2] / offset_length;
        offset_scaled[3] = offsets[3] / offset_length;

        // This should never be out of bounds, the reason being that the max offset must still be on the board, and this will only ever go towards that direction, or in a diagonal
        // if we've gotten this far, we must be moving along a monagonal or diagonal. The scaling effect on the knights move will only strip out the second-max, and diagonals
        // remain on the intended path. If a diagonal is on the board, than moving along that diagonal in either monagonal composing it must also be on the board. Additionally
        // we dont ever need to check the target destination, nor do we want to check the starting destination, since we know whats there wont occlude the move
        bool isOccluded = false;
        for (int i = 1; i < offset_length; i++)
        {
            int piece_at = squares[CoordToIndex(xf + offset_scaled[0] * i, yf + offset_scaled[1] * i, zf + offset_scaled[2] * i, wf + offset_scaled[3] * i)];
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
            if (color_from == 1)
            {
                forward_offset = -forward_offset;
            }

            if (isDifferentColor)
            {
                return offsets_sorted[2] == 1 && offsets_sorted[3] == 1 && forward_offset == 1;
            }
            else
            {
                return offsets_sorted[2] == 0 && offsets_sorted[3] == 1 && forward_offset == 1 && isToEmpty;
            }
        }

        return from != to;
    }






    public void ResetBoard()
    {
        if (has_target)
        {
            GameObject old_target = squares_root.transform.GetChild(target_piece).gameObject;
            old_target.transform.position = PosFromIndex(target_piece);
        }
        InitializeSquares();
        SetPiecesFromSquares();
        move_history = new int[0];
        move_buffer = new int[0];
        tv_bot_state = (tv_bot_state & 4) + 0;
        tv_bot.SetStateFromEncoded(tv_bot_state);
        PushToNetwork();
    }

    public int[] GetSquareArray()
    {
        return (int[])squares.Clone();
    }


    public void UnmakeMove(int from, int to, int captured)
    {
        squares[from] = squares[to];
        squares[to] = captured;

        SetPieceFromSquares(from);
        SetPieceFromSquares(to);

        if (has_target)
        {
            GameObject old_target = squares_root.transform.GetChild(target_piece).gameObject;
            old_target.transform.position = PosFromIndex(target_piece);
        }
        target_piece = from;
        has_target = true;
        GameObject square = squares_root.transform.GetChild(target_piece).gameObject;
        square.transform.position = PosFromIndex(to);
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

    void set_test_state_transpose()
    {
        int[] new_state = (int[])test_state0_transposed.Clone();
        for (int i = 0; i < new_state.Length; i++)
        {
            TransposeInverse(test_state0_transposed, new_state, i);
        }
        SetPieces(new_state);
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

    public void SetPieces(int[] state)
    {
        squares = (int[])state.Clone();
        SetPiecesFromSquares();
    }

    void SetPieceFromSquares(int square_index)
    {
        GameObject square = squares_root.transform.GetChild(square_index).gameObject;
        square.transform.position = PosFromIndex(square_index);

        MeshFilter mesh_filter = square.GetComponent<MeshFilter>();
        MeshRenderer mesh_renderer = square.GetComponent<MeshRenderer>();

        mesh_filter.sharedMesh = GetMesh(squares[square_index]);
        mesh_renderer.sharedMaterial = GetMaterial(squares[square_index]);
    }


    void SetPiecesFromSquares()
    {
        for (int i = 0; i < squares.Length; i++)
        {
            GameObject square = squares_root.transform.GetChild(i).gameObject;
            square.transform.position = PosFromIndex(i);

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

        //set_test_state_transpose();
        SetPiecesFromSquares();
        if (ai_player != null)
        {
            ai_player.InitializeEvaluator(this);
        }

    }


    // Start is called before the first frame update
    void Start()
    {
        InitializeBoard();
        visualizer.InitializeVisualizer();
        
        //if (evaluator != null)
        //{
        //    evaluator.InitializeEvaluator(this);
        //}
    }
    // Logic for snapping to a coordinate. Returns true if the snap is on the board and successful
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

    public bool IsVectorOutOfBounds(Vector4 vector)
    {
        int min = Mathf.RoundToInt(Mathf.Min(vector.x, vector.y, vector.z, vector.w));
        int max = Mathf.RoundToInt(Mathf.Max(vector.x, vector.y, vector.z, vector.w));
        return min < 0 || max > 3;
    }

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

}
