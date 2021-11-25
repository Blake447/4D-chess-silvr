using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chess4DEvaluator : MonoBehaviour
{
    public Chess4DMoveEncoded MoveTemplate;

    public int SYSTEM_VMEM = 64;
    public int SEARCH_DEPTH = 2;
    public int SEARCH_BREADTH = 16;
    public int VMEM_USAGE = 0;

    public int nodes_created = 0;
    public int nodes_deleted = 0;

    public int steps_per_frame = 1;

    int[] board_state;

    public int[] moves_head = new int[1];
    int[] moves_counter = new int[1];


    public int[] move_array;
    public int[] right_array;
    public int[] left_array;
    public int[] parent_array;

    public int[] value_array;
    public int[] next_array;
    public int[] child_array;
    public int[] exit_array;

    bool isAdvancing = false;
    bool isEvaluating = false;


    public int[] memory_stack_next;
    public int[] memory_stack_head = new int[1];
    
    int[] rand_buffer;

    public int[] move_buffer = new int[256];
    public int move_count = 0;

    int[] laterals_x = new int[4] { 1, -1, 0,  0 };
    int[] laterals_y = new int[4] { 0,  0, 0,  0 };
    int[] laterals_z = new int[4] { 0,  0, 1, -1 };
    int[] laterals_w = new int[4] { 0,  0, 0,  0 };

    int[] forwards_x = new int[4] { 0,  0, 0,  0 };
    int[] forwards_y = new int[4] { 1, -1, 0,  0 };
    int[] forwards_z = new int[4] { 0,  0, 0,  0 };
    int[] forwards_w = new int[4] { 0,  0, 1, -1 };


    // gets overwritten later
    int NULL = 52;
    int INVALID = -1;

    Chess4DBoard root_board;

    //int[] pieces_to_move_white = new int[256];
    //public int[] pieces_to_move_black = new int[256];

    //int pieces_count_white = 0;
    //public int pieces_count_black = 0;



    public int[] pieces_to_move = new int[256];
    int pieces_count = 0;
    int capturing_count = 0;


    int[] good_moves;
    int good_moves_count = 0;








    public int[] gbl_tree_root_ref = new int[1];
    public int[] gbl_sentinal_ref = new int[1];
    public int[] gbl_current_piece_ref = new int[1];
    public int[] gbl_current_move_ref = new int[1];
    public int[] gbl_current_depth_ref = new int[1];
    public int[] gbl_current_breadth_ref = new int[1];
    public int[] gbl_state_ref;

    int end_of_line_head;
    int end_of_line_tail;


    void CrashProgram()
    {
        GameObject obj = null;
        print(obj.name);
    }


    int[] point_table = new int[13]
    {
        0, -100000, -50, -25, -10, -20, -5, 100000, 50, 25, 10, 20, 5
    };


    int PieceTypeColorless(int piece_type)
    {
        return (piece_type - 1) % 6 + 1;
    }
    int PieceColor(int piece_type)
    {
        if (isBlackPiece(piece_type))
        {
            return 0;
        }
        return 1;
    }







    int SentinalNextTreeWalk(int[] previous_sentinal, int[] descending, int sentinal)
    {
        return 0;
    }

    int encode_movement(int from, int to, int piece_captured)
    {
        return ((from & 255) << 0) + ((to & 255) << 8) + ((piece_captured & 255) << 16);
    }
    int decode_from(int movement)
    {
        return ((movement >> 0) & 255);
    }
    int decode_to(int movement)
    {
        return ((movement >> 8) & 255);
    }
    int decode_captured(int movement)
    {
        return ((movement >> 16) & 255);
    }

    void ApplyEncodedMovement(int[] state, int encoded_movment)
    {
        int from = decode_from(encoded_movment);
        int to = decode_to(encoded_movment);
        int temp = state[from];
        state[from] = 0;
        state[to] = temp;
    }
    void RevertEncodedMovement(int[] state, int encoded_movment)
    {
        int from = decode_from(encoded_movment);
        int to = decode_to(encoded_movment);
        int captured = decode_captured(encoded_movment);
        int temp = state[to];
        state[to] = captured;
        state[from] = temp;
    }
    int EvaluateState(int[] state)
    {
        int value = 0;
        for (int i = 0; i < 256; i++)
        {
            value += point_table[state[i]];
        }
        return value;
    }

    public void FillPieceToMoveArrays(int[] board_state, bool isBlack)
    {
        ClearPieceToMoveBuffer();

        capturing_count = 0;

        for (int i = 0; i < board_state.Length; i++)
        {
            if (isBlackPiece(board_state[i]) == isBlack)
            {
                AddPieceRefToPieceBuffer(i);
                if (CanPieceCapture(board_state, i))
                {
                    //Debug.Log("Piece number: " + i + " can capture, swapping with " + capturing_count);
                    int original_index = pieces_count - 1;
                    int target_index = capturing_count;
                    int temp_ref = GetPieceRefFromPieceBuffer(original_index);
                    SetPieceRefToPieceBuffer(original_index, GetPieceRefFromPieceBuffer(target_index));
                    SetPieceRefToPieceBuffer(target_index, temp_ref);
                    capturing_count++;
                }
            }
        }
    }

    public void ClearPieceToMoveBuffer()
    {
        pieces_count = 0;
    }
    public int GetPieceRefFromPieceBuffer(int index)
    {
        return pieces_to_move[index];
    }
    public void SetPieceRefToPieceBuffer(int index, int piece_reference)
    {
        pieces_to_move[index] = piece_reference;
    }
    public void AddPieceRefToPieceBuffer(int piece_reference)
    {
        pieces_to_move[pieces_count] = piece_reference;
        pieces_count++;
    }
    public void ClearMoveArray()
    {
        move_count = 0;
    }
    public void AddMoveToBuffer(int move)
    {
        move_buffer[move_count] = move;
        move_count++;
    }
    public void ClearGoodMoves()
    {
        good_moves_count = 0;
    }
    public void AddToGoodMoves(int good_move)
    {
        good_moves[good_moves_count] = good_move;
        good_moves_count++;
    }
    public void FillGoodMoves(int tree_head, bool isMinning)
    {
        int sentinal = tree_head;
        if (child_array[tree_head] != NULL)
        {
            sentinal = child_array[tree_head];
            int target_value = value_array[sentinal];
            if (isMinning)
            {
                while (next_array[sentinal] != NULL)
                {
                    sentinal = next_array[sentinal];
                    target_value = Mathf.Min(target_value, target_value);
                }
            }
            else
            {
                while (next_array[sentinal] != NULL)
                {
                    sentinal = next_array[sentinal];
                    target_value = Mathf.Max(target_value, target_value);
                }
            }
            sentinal = child_array[tree_head];
            ClearGoodMoves();
            while (sentinal != NULL)
            {
                if (value_array[sentinal] == target_value)
                {
                    AddToGoodMoves(move_array[sentinal]);
                }
            }


        }

    }



    public void StartTreeConstruction()
    {
        LoadBoardState(root_board);
        AddNullRoot(gbl_tree_root_ref);
        gbl_sentinal_ref[0] = gbl_tree_root_ref[0];
        gbl_current_depth_ref[0] = 0;
        gbl_current_move_ref[0] = 0;
        gbl_current_piece_ref[0] = 0;
        gbl_current_breadth_ref[0] = 0;
        gbl_state_ref = (int[])board_state.Clone();
        FillPieceToMoveArrays(gbl_state_ref, true);
        FillMoveBuffer(gbl_state_ref, pieces_to_move[0]);
        isAdvancing = true;
    }
    public void AdvanceTreeConstruction()
    {
        if (isAdvancing)
        {
            isAdvancing = AddNextMoveToTreeAlt(gbl_sentinal_ref, gbl_current_piece_ref, gbl_current_move_ref, gbl_current_depth_ref, gbl_current_breadth_ref, gbl_state_ref, SEARCH_BREADTH, SEARCH_DEPTH);
        }
        if (!isAdvancing)
        {
            EndTreeConstruction();
        }
    
    }
    public void EndTreeConstruction()
    {
        //PrintMovesInEndOfLines();
        PrintTopMoveValues();

        //BeginEvaluating();
    }
    void BeginEvaluating()
    {
        isEvaluating = true;
    }
    void AdvanceEvaluating()
    {
        //isEvaluating = EvaluateNode();
    }

    private void Update()
    {
        if (isAdvancing)
        {
            for (int i = 0; i < steps_per_frame; i++)
            {
                AdvanceTreeConstruction();
            }
        }
        //else if (isEvaluating)
        //{
        //    AdvanceEvaluating();
        //    if (!isEvaluating)
        //    {
        //        Debug.Log("Finished Updating tree values without crashing");
        //        PrintTopMoveValues();
        //    }
        //}
    }

    // NOTE: Black is the maximizing player in whatever fucked up convention I've implemented
    private bool EvaluateNode(int eldest_child, int min_max_value)
    {
        int sentinal = eldest_child;
        // Get the value array from the head of the end of line head before freeing the memory with PopNode.
        // Shitty hack because I cant be fucked to do any fancy depth storing or modular arithmetic to determine this
        //int sentinal = end_of_line_head;
        bool is_min = (min_max_value == 0) ? true : false;

        // Get the actual address of the move given. This frees the memory address the pointer to the move was stored in.
        //sentinal = PopNodeFromEndOfLines();
        if (sentinal == NULL)
        {
            return false;
        }
        // If we are not at the top nor are we at the second from top
        if (parent_array[sentinal] != NULL && parent_array[parent_array[sentinal]] != NULL)
        {
            // Since our sentinal is at the end of the branch, we want to get to the first element in the branch, marked as the main child of the parent of it.
            // Shitty hack because I dont want to implement previous pointers
            sentinal = child_array[parent_array[sentinal]];

            int max_value = value_array[sentinal];
            if (is_min)
            {
                int counter = 0;
                while (next_array[sentinal] != NULL && counter < SEARCH_BREADTH + 1)
                {
                    sentinal = next_array[sentinal];
                    max_value = Mathf.Min(max_value, value_array[sentinal]);
                    counter++;
                }
            }
            else
            {
                int counter = 0;

                while (next_array[sentinal] != NULL && counter < SEARCH_BREADTH + 1)
                {
                    sentinal = next_array[sentinal];
                    max_value = Mathf.Max(max_value, value_array[sentinal]);
                    counter++;
                }
            }

            sentinal = parent_array[sentinal];
            value_array[sentinal] = value_array[sentinal] + max_value;
        }

        return true;
    }


    private void FreeNodesChildren(int address)
    {
        int counter = 0;
        while (child_array[address] != NULL)
        {
            FreeNodesChild(address);
            counter++;
        }
    }

    // WARNING: Do no use FreeNode on nodes with children, it will NOT free the nodes children recursively
    // and cause a memory leak
    private void FreeNodesChild(int address)
    {
        if (child_array[address] != NULL)
        {
            int child_address = child_array[address];
            child_array[address] = next_array[child_array[address]];

            nodes_deleted++;
            FreeMemoryAddress(child_address);
        }
        
    }


    private bool AddNextMoveToTreeAlt(int[] sentinal_ref, int[] current_piece_ref, int[] current_move_ref, int[] current_depth_ref, int[] current_breadth_ref, int[] state_ref, int breadth, int depth)
    {
        bool isStartingBlack = true;
        int sentinal = sentinal_ref[0];
        int current_piece = current_piece_ref[0];
        int current_move = current_move_ref[0];
        int current_depth = current_depth_ref[0];
        int current_breadth = current_breadth_ref[0];

        if (current_breadth < breadth)
        {
            if (current_breadth == 0 && current_piece == 0)
            {
                int color_parity = current_depth & 1;
                bool isSearchingWhite = (color_parity == 0) == isStartingBlack;

                FillPieceToMoveArrays(state_ref, isSearchingWhite);
            }
            if (current_move == 0 && current_piece < pieces_count)
            {
                FillMoveBuffer(state_ref, pieces_to_move[current_piece]);
            }
            if (current_move < move_count)
            {
                int move = move_buffer[current_move];
                int value = point_table[decode_captured(move)];

                AddChildToMoveTree(sentinal, move_buffer[current_move], value);
                ApplyEncodedMovement(state_ref, move_buffer[current_move]);
                root_board.SetPieces(state_ref);
                RevertEncodedMovement(state_ref, move_buffer[current_move]);
                
                current_breadth++;
                current_move++;
            }
            else
            {
                current_piece++;
                current_move = 0;
                ClearMoveArray();
            }
            if (current_piece >= pieces_count)
            {
                ClearMoveArray();
                ClearPieceToMoveBuffer();
                current_breadth = breadth;
            }
        }
        else
        {
            ClearMoveArray();
            ClearPieceToMoveBuffer();
            current_breadth = 0;
            current_move = 0;
            current_piece = 0;

            if (current_depth < depth && child_array[sentinal] != NULL)
            {
                sentinal = child_array[sentinal];
                ApplyEncodedMovement(state_ref, move_array[sentinal]);
                current_depth++;
            }
            else
            {
                while (parent_array[sentinal] != NULL && next_array[sentinal] == NULL)
                {
                    
                    int color_parity = current_depth & 1;
                    bool isSearchingWhite = (color_parity == 0) != isStartingBlack;
                    int min_max_value = isSearchingWhite ? 1 : 0;
                    //PushNodeToEndOfLines(sentinal, min_max_value);
                    EvaluateNode(sentinal, min_max_value);

                    RevertEncodedMovement(state_ref, move_array[sentinal]);
                    if (parent_array[sentinal] != NULL && parent_array[parent_array[sentinal]] != NULL)
                    {
                        FreeNodesChildren(sentinal);
                    }
                    
                    sentinal = parent_array[sentinal];
                    current_depth = current_depth - 1;
                }
                if (next_array[sentinal] != NULL)
                {
                    if (parent_array[sentinal] != NULL && parent_array[parent_array[sentinal]] != NULL)
                    {
                        FreeNodesChildren(sentinal);
                    }


                    RevertEncodedMovement(state_ref, move_array[sentinal]);
                    sentinal = next_array[sentinal];
                    ApplyEncodedMovement(state_ref, move_array[sentinal]);
                }
                else
                {
                    //root_board.SetPieces(state_ref);
                    return false;
                }
            }
        }

        sentinal_ref[0] = sentinal;
        current_piece_ref[0] = current_piece;
        current_move_ref[0] = current_move;
        current_depth_ref[0] = current_depth;
        current_breadth_ref[0] = current_breadth;
        return true;
    }


    public int deref(int reference)
    {
        return move_array[reference];
    }


    public void AddNullRoot(int[] parent_reference)
    {
        nodes_created++;
        int address = GetFreeMemoryAddress();
        parent_reference[0] = address;
        
        next_array[address] = NULL;
        parent_array[address] = NULL;
        exit_array[address] = NULL;
        child_array[address] = NULL;
        move_array[address] = 0;
        value_array[address] = 0;
    }

    

    public void AddChildToMoveTree(int parent, int move, int value)
    {
        if (parent != NULL)
        {
            nodes_created++;
            int address = GetFreeMemoryAddress();
            if (address != NULL)
            {
                move_array[address] = move;
                value_array[address] = value;
                if (child_array[parent] == NULL)
                {
                    child_array[parent] = address;
                    child_array[address] = NULL;
                    parent_array[address] = parent;
                    next_array[address] = NULL;
                    exit_array[address] = parent;
                }
                else
                {
                    next_array[address] = child_array[parent];
                    child_array[parent] = address;
                    parent_array[address] = parent;
                    exit_array[address] = NULL;
                    child_array[address] = NULL;

                }
            }
        }
    }

    //public void ConstructTreeHierarchy(int[] moves_head)
    //{
    //    int sentinal0 = moves_head[0];
    //    int sentinal1 = moves_head[0];

    //    int[] lowest_branch = new int[1];
    //    int[] descending = new int[1];
    //    lowest_branch[0] = sentinal0;

    //    bool isDescending = true;
    //    int counter = 0;
    //    while (sentinal0 != NULL && counter < SYSTEM_VMEM)
    //    {
    //        if (isDescending)
    //        {
    //            if (left_array[sentinal0] != NULL)
    //            {
    //                sentinal0 = left_array[sentinal0];
    //            }
    //            else if (right_array[sentinal0] != NULL && right_array[sentinal0] != sentinal1)
    //            {
    //                Debug.Log("descending (r): " + ExtractMoveValue(move_array[sentinal0]));
    //                sentinal0 = right_array[sentinal0];
    //            }
    //            else
    //            {
    //                Debug.Log("descending (t): " + ExtractMoveValue(move_array[sentinal0]));
    //                isDescending = false;
    //            }
    //        }
    //        if (!isDescending)
    //        {
    //            if (right_array[sentinal0] == NULL || right_array[sentinal0] == sentinal1)
    //            {

    //                sentinal1 = sentinal0;
    //                sentinal0 = parent_array[sentinal0];

    //                int sentinal0_val = ExtractMoveValue(move_array[sentinal0]);
    //                int sentinal1_val = ExtractMoveValue(move_array[sentinal1]);

    //                if (sentinal0 != NULL && sentinal0_val >= sentinal1_val)
    //                {
    //                    Debug.Log("ascending: " + ExtractMoveValue(move_array[sentinal0]));
    //                }

    //            }
    //            else
    //            {
    //                sentinal0 = right_array[sentinal0];
    //                isDescending = true;
    //            }
    //        }
    //        //Debug.Log(ExtractMoveValue(move_array[sentinal0]));
    //        counter++;
    //    }
    //    if (counter > SYSTEM_VMEM - 2)
    //    {
    //        Debug.Log("Warning, possible infinite loop detected");
    //    }
    //}

    public void InitializeEvaluator(Chess4DBoard initializing_board)
    {
        root_board = initializing_board;
        LoadBoardState(root_board);

        move_array = new int[SYSTEM_VMEM];
        right_array = new int[SYSTEM_VMEM];
        left_array = new int[SYSTEM_VMEM];
        parent_array = new int[SYSTEM_VMEM];

        value_array = new int[SYSTEM_VMEM];
        next_array = new int[SYSTEM_VMEM];
        child_array = new int[SYSTEM_VMEM];
        exit_array = new int[SYSTEM_VMEM];
        
        memory_stack_next = new int[SYSTEM_VMEM];

        good_moves = new int[SEARCH_BREADTH];
        good_moves_count = 0;

        NULL = SYSTEM_VMEM - 1;
        move_array[NULL] = NULL;
        right_array[NULL] = NULL;
        left_array[NULL] = NULL;
        parent_array[NULL] = NULL;

        value_array[NULL] = NULL;
        next_array[NULL] = NULL;
        child_array[NULL] = NULL;
        exit_array[NULL] = NULL;

        end_of_line_head = NULL;
        end_of_line_tail = NULL;

        moves_head[0] = NULL;

        memory_stack_head = new int[1];
        memory_stack_head[0] = NULL;


        InitializeMemory();

        VMEM_USAGE = 0;
        
        StartTreeConstruction();

    }

    void FreeMemoryAddress(int address)
    {
        if (address != NULL)
        {
            if (memory_stack_head[0] == NULL)
            {
                memory_stack_head[0] = address;
                memory_stack_next[address] = NULL;
            }
            else
            {
                memory_stack_next[address] = memory_stack_head[0];
                memory_stack_head[0] = address;
            }
            VMEM_USAGE -= 1;

        }

    }

    void InitializeMemory()
    {
        for (int i = 0; i < SYSTEM_VMEM - 1; i++)
        {
            FreeMemoryAddress(i);
        }
    }

    //void AddToTree(int[] moves_head, int encoded_move)
    //{
    //    //int encoded_value = ExtractMoveValue(encoded_move);
    //    int sentinal0 = moves_head[0];
    //    int sentinal1 = moves_head[0];
    //    if (sentinal0 == NULL)
    //    {
    //        int index = 0;

    //        move_array[index] = encoded_move;
    //        right_array[index] = NULL;
    //        left_array[index] = NULL;
    //        parent_array[index] = NULL;
    //        moves_head[0] = index;
    //    }
    //    else
    //    {
    //        bool is_going_left = true;
    //        while (sentinal1 != NULL)
    //        {
    //            int encoded_value = ExtractMoveValue(encoded_move);
    //            int sentinal_value = ExtractMoveValue(move_array[sentinal0]);
    //            if (encoded_value <= sentinal_value)
    //            {
    //                sentinal1 = left_array[sentinal0];
    //                is_going_left = true;
    //            }
    //            else if (encoded_value > sentinal_value)
    //            {
    //                sentinal1 = right_array[sentinal0];
    //                is_going_left = false;
    //            }
    //            if (sentinal1 != NULL)
    //            {
    //                sentinal0 = sentinal1;
    //            }
    //        }
    //        sentinal1 = GetFreeMemoryAddress();
    //        int next_node = sentinal1;
    //        if (is_going_left)
    //        {
    //            left_array[sentinal0] = sentinal1;
    //        }
    //        else
    //        {
    //            right_array[sentinal0] = sentinal1;
    //        }

    //        move_array[next_node] = encoded_move;
    //        right_array[next_node] = NULL;
    //        left_array[next_node] = NULL;
    //        parent_array[next_node] = sentinal0;
    //    }
    //}

    int GetFreeMemoryAddress()
    {
        int old_head = memory_stack_head[0];
        int new_head = memory_stack_next[old_head];
        memory_stack_next[old_head] = NULL;
        memory_stack_head[0] = new_head;
        if (new_head == NULL)
        {
            Debug.LogWarning("Warning, out of virtual memory. Check code for memory leaks");
        }
        VMEM_USAGE++;
        return old_head;
    }

    int Random_01()
    {
        return Random.Range(0, 2);
    }

    public void PrintTopMoveValues()
    {
        int sentinal = gbl_tree_root_ref[0];
        string values = "";
        if (child_array[sentinal] != NULL)
        {
            sentinal = child_array[sentinal];
            while (sentinal != NULL)
            {
                values = values + value_array[sentinal].ToString() + ": ";
                sentinal = next_array[sentinal];
            }
        }
        Debug.Log(" ");
        Debug.Log("Top Move values: ");
        Debug.Log(values);
    }

    //int SentinalNext(int entry, int sentinal)
    //{
    //    int entry_value = ExtractMoveValue(entry);
    //    int sentinal_value = ExtractMoveValue(move_array[sentinal]);
    //    if (entry_value <= sentinal_value)
    //    {
    //        int new_sentinal = left_array[sentinal];
    //        return new_sentinal;
    //    }
    //    else if (entry_value > sentinal_value)
    //    {
    //        int new_sentinal = right_array[sentinal];
    //        return new_sentinal;
    //    }
    //    else
    //    {
    //        int rand = Random_01();
    //        int new_sentinal = left_array[sentinal];
    //        if (rand == 0) { new_sentinal = right_array[sentinal]; }
    //        return new_sentinal;
    //    }

    //}



    int EncodeMove(int value)
    {
        return value;
    }
    int ExtractMoveValue(int move)
    {
        return move;
    }





    void LoadBoardState(Chess4DBoard board)
    {
        board_state = board.GetSquareArray();
    }

    void GenerateMovesForState(Chess4DBoard board, bool isBlackPlayer)
    {
        LoadBoardState(board);

        int[] laterals_x = new int[4] { 1, -1, 0,  0 };
        int[] laterals_y = new int[4] { 0,  0, 0,  0 };
        int[] laterals_z = new int[4] { 0,  0, 1, -1 };
        int[] laterals_w = new int[4] { 0,  0, 0,  0 };

        int[] forwards_x = new int[4] { 0,  0, 0,  0 };
        int[] forwards_y = new int[4] { 1, -1, 0,  0 };
        int[] forwards_z = new int[4] { 0,  0, 0,  0 };
        int[] forwards_w = new int[4] { 0,  0, 1, -1 };


        int piece_count = 0;
        int[] pieces_to_move = new int[board_state.Length + 1];
        for (int i = 0; i < board_state.Length; i++)
        {
            if (board_state[i] != 0 && (isBlackPiece(board_state[i]) == isBlackPlayer) )
            {
                pieces_to_move[piece_count] = i;
                piece_count++;
            }
        }


    }

    int EvaluateCapture(int square_to)
    {
        return point_table[board_state[square_to]];
    }
    //int EvaluateKingStatus(int king_square)
    //{
    //    int king_status = 0;
    //    bool is_king_black = isBlackPiece(board_state[king_square]);

    //    int[] laterals_x = new int[4] { 1, -1, 0,  0 };
    //    int[] laterals_y = new int[4] { 0,  0, 0,  0 };
    //    int[] laterals_z = new int[4] { 0,  0, 1, -1 };
    //    int[] laterals_w = new int[4] { 0,  0, 0,  0 };

    //    int[] forwards_x = new int[4] { 0,  0, 0,  0 };
    //    int[] forwards_y = new int[4] { 1, -1, 0,  0 };
    //    int[] forwards_z = new int[4] { 0,  0, 0,  0 };
    //    int[] forwards_w = new int[4] { 0,  0, 1, -1 };

    //    for (int i = 0; i < 4; i++)
    //    {
    //        int piece_found = CastPiece(king_square, laterals_x[i], laterals_y[i], laterals_z[i], laterals_w[i]);
    //        if ( isRook(piece_found, !is_king_black) )
    //        {
    //            king_status += -point_table[board_state[king_square]];
    //        }
    //    }

    //    return 0;
    //}
    bool isBlackPiece(int piece_id)
    {
        return (piece_id < 7);
    }

    bool isRook(int piece_id, bool isBlack)
    {
        if (isBlack)
        {
            return piece_id == 5 || piece_id == 2;
        }
        else
        {
            return piece_id == 11 || piece_id == 8;
        }
    }
    bool isBishop(int piece_id, bool isBlack)
    {
        if (isBlack)
        {
            return piece_id == 3 || piece_id == 2;
        }
        else
        {
            return piece_id == 9 || piece_id == 8;
        }
    }



    int CastPiece(int starting_square, int x_dir, int y_dir, int z_dir, int w_dir)
    {
        int x = (starting_square >> 0) & 3;
        int y = (starting_square >> 2) & 3;
        int z = (starting_square >> 4) & 3;
        int w = (starting_square >> 6) & 3;

        bool exitFlag = false;
        int piece_found = 0;

        while (!exitFlag)
        {
            x += x_dir;
            y += y_dir;
            z += z_dir;
            w += w_dir;

            if ( isOutOfBounds(x, y, z, w) )
            {
                exitFlag = true;
            }
            else
            {
                int index = CoordToIndex(x, y, z, w);
                int piece = board_state[index];
                if (piece != 0)
                {
                    piece_found = piece;
                    exitFlag = true;
                }
            }
        }

        return piece_found;
    }

    public bool isOutOfBounds(int xi, int yi, int zi, int wi)
    {
        bool x_out = xi < 0 || 3 < xi;
        bool y_out = yi < 0 || 3 < yi;
        bool z_out = zi < 0 || 3 < zi;
        bool w_out = wi < 0 || 3 < wi;

        return x_out || y_out || z_out || w_out;
    }

    public int CoordToIndex(int xi, int yi, int zi, int wi)
    {
        return ((xi & 3 ) << 0) + ((yi & 3) << 2) + ((zi & 3) << 4) + ((wi & 3) << 6);
    }

    bool CanPieceCapture(int[] state, int square)
    {
        move_count = 0;
        move_buffer[0] = NULL;


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

        }
        else if (piece_type == 2)
        {

        }
        else if (piece_type == 3)
        {

        }
        else if (piece_type == 4)
        {

        }
        else if (piece_type == 5)
        {

        }
        else if (piece_type == 6)
        {
            sx += forwards_x[0 + piece_color];
            sy += forwards_y[0 + piece_color];
            sz += forwards_z[0 + piece_color];
            sw += forwards_w[0 + piece_color];

            sx += laterals_x[0];
            sy += laterals_y[0];
            sz += laterals_z[0];
            sw += laterals_w[0];

            for (int i = 0; i < 4; i++)
            {
                sx += -laterals_x[i] + laterals_x[(i + 1) & 3];
                sy += -laterals_y[i] + laterals_y[(i + 1) & 3];
                sz += -laterals_z[i] + laterals_z[(i + 1) & 3];
                sw += -laterals_w[i] + laterals_w[(i + 1) & 3];

                target_index = CoordToIndex(sx, sy, sz, sw);
                if (!isOutOfBounds(sx, sy, sz, sw) && state[target_index] != 0 && isBlackPiece(state[target_index]) != isBlackPiece(piece))
                {
                    return true;
                }
            }

            sx = px;
            sy = py;
            sz = pz;
            sw = pw;

            sx += forwards_x[2 + piece_color];
            sy += forwards_y[2 + piece_color];
            sz += forwards_z[2 + piece_color];
            sw += forwards_w[2 + piece_color];

            sx += laterals_x[0];
            sy += laterals_y[0];
            sz += laterals_z[0];
            sw += laterals_w[0];

            for (int i = 0; i < 4; i++)
            {
                sx += -laterals_x[i] + laterals_x[(i + 1) & 3];
                sy += -laterals_y[i] + laterals_y[(i + 1) & 3];
                sz += -laterals_z[i] + laterals_z[(i + 1) & 3];
                sw += -laterals_w[i] + laterals_w[(i + 1) & 3];

                target_index = CoordToIndex(sx, sy, sz, sw);
                if (!isOutOfBounds(sx, sy, sz, sw) && state[target_index] != 0 && isBlackPiece(state[target_index]) != isBlackPiece(piece))
                {
                    return true;
                }
            }
        }
        return false;
    }


    void FillMoveBuffer(int[] state, int square)
    {
        //pieces_count = 0;

        move_count = 0;
        move_buffer[0] = NULL;


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

        }
        else if (piece_type == 2)
        {

        }
        else if (piece_type == 3)
        {

        }
        else if (piece_type == 4)
        {

        }
        else if (piece_type == 5)
        {

        }
        else if (piece_type == 6)
        {
            sx += forwards_x[0 + piece_color];
            sy += forwards_y[0 + piece_color];
            sz += forwards_z[0 + piece_color];
            sw += forwards_w[0 + piece_color];

            target_index = CoordToIndex(sx, sy, sz, sw);
            if (!isOutOfBounds(sx, sy, sz, sw) && state[target_index] == 0)
            {
                int encoded_movement = encode_movement(square, target_index, state[target_index]);
                move_buffer[move_count] = encoded_movement;
                move_count++;
            }

            sx += laterals_x[0];
            sy += laterals_y[0];
            sz += laterals_z[0];
            sw += laterals_w[0];

            for (int i = 0; i < 4; i++)
            {
                sx += -laterals_x[i] + laterals_x[(i + 1) & 3];
                sy += -laterals_y[i] + laterals_y[(i + 1) & 3];
                sz += -laterals_z[i] + laterals_z[(i + 1) & 3];
                sw += -laterals_w[i] + laterals_w[(i + 1) & 3];

                target_index = CoordToIndex(sx, sy, sz, sw);
                if (!isOutOfBounds(sx, sy, sz, sw) && state[target_index] != 0 && isBlackPiece(state[target_index]) != isBlackPiece(piece))
                {
                    move_buffer[move_count] = encode_movement(square, target_index, state[target_index]);
                    move_count++;
                }
            }


            //target_index = CoordToIndex(sx, sy, sz, sw);
            //if (!isOutOfBounds(sx, sy, sz, sw) && isBlackPiece(state[target_index]) != isBlackPiece(piece))
            //{
            //    move_buffer[move_count] = encode_movement(square, target_index, state[target_index]);
            //    move_count++;
            //}

            sx = px;
            sy = py;
            sz = pz;
            sw = pw;

            sx += forwards_x[2 + piece_color];
            sy += forwards_y[2 + piece_color];
            sz += forwards_z[2 + piece_color];
            sw += forwards_w[2 + piece_color];

            target_index = CoordToIndex(sx, sy, sz, sw);
            if (!isOutOfBounds(sx, sy, sz, sw) && state[target_index] == 0)
            {
                move_buffer[move_count] = encode_movement(square, target_index, state[target_index]);
                move_count++;
            }

            sx += laterals_x[0];
            sy += laterals_y[0];
            sz += laterals_z[0];
            sw += laterals_w[0];

            for (int i = 0; i < 4; i++)
            {
                sx += -laterals_x[i] + laterals_x[(i + 1) & 3];
                sy += -laterals_y[i] + laterals_y[(i + 1) & 3];
                sz += -laterals_z[i] + laterals_z[(i + 1) & 3];
                sw += -laterals_w[i] + laterals_w[(i + 1) & 3];

                target_index = CoordToIndex(sx, sy, sz, sw);
                if (!isOutOfBounds(sx, sy, sz, sw) && state[target_index] != 0 && isBlackPiece(state[target_index]) != isBlackPiece(piece))
                {
                    move_buffer[move_count] = encode_movement(square, target_index, state[target_index]);
                    move_count++;
                }
            }
        }
    }




}
