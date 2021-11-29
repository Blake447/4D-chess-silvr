using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chess4DEvaluator : MonoBehaviour
{
    ///////////////////////////////////////////
    ///        External References          ///
    ///////////////////////////////////////////
    

    // Reference to chess board AI is attached to
    Chess4DBoard root_board;


    ///////////////////////////////////////////
    ///         Startup Parameters          ///
    ///////////////////////////////////////////


    // User specifiable parameters. Shouldn't be changed at runtime.
    public int SYSTEM_VMEM = 524288;
    public int SEARCH_DEPTH = 1;
    public int SEARCH_BREADTH = 256;
    public int VMEM_USAGE = 0;
    public int STEPS_PER_FRAME = 8192;
    int NULL = 52;


    ///////////////////////////////////////////
    ///       Memory Block Managment        ///
    ///////////////////////////////////////////


    // Memory stack information. Gets set to length SYSTEM_VMEM
    int[] memory_stack_next;
    int[] memory_stack_head = new int[1];

    // Memory blocks for encoded movements and value of said movements 
    int[] move_array;
    int[] value_array;

    // Memory blocks for pointers required for constructing move tree
    int[] parent_array;
    int[] next_array;
    int[] child_array;


    ///////////////////////////////////////////
    ///      Pass by Reference Arrays       ///
    ///////////////////////////////////////////


    // Integer arrays of length one for udon compatible pass by reference. Shitty hack that will cause (and has caused) trouble down the line
    // Not really neccessary since they're all global variables anyway, but future refactoring may just allocated these as needed.
    int[] moves_head = new int[1];
    int[] gbl_tree_root_ref = new int[1];
    int[] gbl_sentinal_ref = new int[1];
    int[] gbl_current_piece_ref = new int[1];
    int[] gbl_current_move_ref = new int[1];
    int[] gbl_current_depth_ref = new int[1];
    int[] gbl_current_breadth_ref = new int[1];
    int[] gbl_color_ref = new int[1];
    int[] gbl_garbage_collector = new int[1];
    
    int[] gbl_state_ref;


    ///////////////////////////////////////////
    ///    Variable Count Storage Arrays    ///
    ///////////////////////////////////////////


    // Arrays of fixed length in which we store varying numbers of elements, keeping track of cardinality with a global int
    int[] cast_results = new int[3];
    int cast_count = 0;

    int[] move_buffer = new int[256];
    int move_count = 0;

    int[] pieces_to_move = new int[256];
    int pieces_count = 0;

    int[] good_moves;
    int good_moves_count = 0;


    ///////////////////////////////////////////
    ///         AI State Variables          ///
    ///////////////////////////////////////////


    // buffer for a virtual board state used for analysis
    int[] board_state;

    // is playing white
    bool isPlayingWhite = true;

    // advancing and clearing flags
    bool isAdvancing = false;
    bool isClearingTree = false;

    int move_chosen_buffer = 0;

    ///////////////////////////////////////////
    ///      Constant Look-Up Tables        ///
    ///////////////////////////////////////////

    int[] laterals_x = new int[4] { 1, -1, 0,  0 };
    int[] laterals_y = new int[4] { 0,  0, 0,  0 };
    int[] laterals_z = new int[4] { 0,  0, 1, -1 };
    int[] laterals_w = new int[4] { 0,  0, 0,  0 };

    int[] forwards_x = new int[4] { 0,  0, 0,  0 };
    int[] forwards_y = new int[4] { 1, -1, 0,  0 };
    int[] forwards_z = new int[4] { 0,  0, 0,  0 };
    int[] forwards_w = new int[4] { 0,  0, 1, -1 };

    int[] point_table = new int[13]
    {
        0, -100001, -50, -30, -10, -40, -5, 100000, 50, 30, 10, 40, 5
    };


    ///////////////////////////////////////////
    ///                                     ///
    ///     Simple Array Manipulations      ///
    ///                                     ///
    ///////////////////////////////////////////

    // Simple array manipulation for various arrays of fixed length used in the program.

    // All of these arrays are of fixed size, but can only be as large as the board itself. We allocate a spot for
    // every square on the board, then simply keep track of how many objects we are meaningfully storing in there.

    // to add a piece to the buffer, we'll simply place it into the next spot, and add to the count of the pieces
    // to "clear" out a buffer, there's no need to actually clear data, we just need to set the count to 0.

    // note this means all piece count variables global references
    


    // Adding and clearing methods for the move buffer. Stores possible moves as encoded values (not node adresses)
    private void AddMoveToBuffer(int move)
    {
        move_buffer[move_count] = move;
        move_count++;
    }
    private void ClearMoveBuffer()
    {
        move_count = 0;
    }

    // Adding and clearing methods for the good moves array. Stores the move as enocded values (not node adresses)
    private void AddToGoodMoves(int good_move)
    {
        good_moves[good_moves_count] = good_move;
        good_moves_count++;
    }
    private void ClearGoodMoves()
    {
        good_moves_count = 0;
    }

    // Adding and clearing methods for the casting array. Add is deprecated since we just assign by index in a loop
    private void AddToCast(int square)
    {
        cast_results[cast_count] = square;
        cast_count++;
    }

    private void ClearCast()
    {
        cast_count = 0;
    }

    // methods for piece buffer. The piece buffer stores the index of a piece on the board, and is thus dependent on
    // board_state for the results. Additionally has Get and Set methods for use in rearranging the order
    // in which we evaluate pieces, in case future changes want to optimize for alpha-beta pruning.

    // It should be noted this change is prospective and requires serious tree structure changes to make any meaningful difference
    private int GetPieceRefFromPieceBuffer(int index)
    {
        return pieces_to_move[index];
    }
    private void SetPieceRefToPieceBuffer(int index, int piece_reference)
    {
        pieces_to_move[index] = piece_reference;
    }
    private void AddPieceRefToPieceBuffer(int piece_reference)
    {
        pieces_to_move[pieces_count] = piece_reference;
        pieces_count++;
    }
    private void ClearPieceRefsFromPieceBuffer()
    {
        pieces_count = 0;
    }


    ///////////////////////////////////////////
    ///                                     ///
    ///             Event Calls             ///
    ///                                     ///
    ///////////////////////////////////////////

    private void OnMoveFound()
    {
        int move_found = move_chosen_buffer;
        if (move_found != 0)
        {
            MakeMoveOnBoard(root_board, move_chosen_buffer);
        }
        else
        {
            Debug.Log("Warning, no chosen move detected");
        }

        // Logic to make AI play itself
        isPlayingWhite = !isPlayingWhite;
        //SearchForMove();
    }

    private void OnInitialization()
    {
        //SearchForMove();
    }

    ///////////////////////////////////////////
    ///                                     ///
    ///      Misc. High Level Methods       ///
    ///                                     ///
    ///////////////////////////////////////////

    public void SearchForMove()
    {
        StartSearchForMove();
    }

    public void MakeMoveOnBoard(Chess4DBoard board, int move)
    {
        // Revert the board state to the boards actual state (cloning the array and discarding the current one)
        LoadBoardState(board);

        // Calculate the from and to squares for the board
        int from = decode_from(move);
        int to = decode_to(move);

        // Tell the board to make the move
        board.MakeMove(from, to);

        // Update the board state array in the evaluator
        LoadBoardState(root_board);
    }

    // Announce that a move is about to be made
    public void AnnounceMove(int[] state, int move)
    {
        int from = decode_from(move);
        int from_piece = state[from];
        int fx = (from >> 0) & 3;
        int fy = (from >> 2) & 3;
        int fz = (from >> 4) & 3;
        int fw = (from >> 6) & 3;

        int color_from = PieceColor(from_piece);
        string color_string = color_from == 0 ? "Black" : "White";

        int to = decode_to(move);
        int to_piece = state[to];
        int tx = (to >> 0) & 3;
        int ty = (to >> 2) & 3;
        int tz = (to >> 4) & 3;
        int tw = (to >> 6) & 3;

        bool isCapturing = (decode_captured(move) != NULL && decode_captured(move) != 0);
        string output = "Moving a " + color_string + " " + GetPieceString(from_piece) + " from " + CalculateCoordinateString(fx, fy, fz, fw) + " to " + CalculateCoordinateString(tx, ty, tz, tw);
        if (isCapturing)
        {
            output = output + ", capturing a " + GetPieceString(to_piece);
        }
        Debug.Log(output);
    }

    ///////////////////////////////////////////
    ///                                     ///
    ///    Tree Construction and Freeing    ///
    ///                                     ///
    ///////////////////////////////////////////

    // OOOOOOOH its the big one! Main logic for constructing, evaluating, and freeing the move tree.
    // The tree itself is n-nary, consisting of a root node (a move that does not change the board, and
    // doesnt belong to either player) to start, with each node having a child node, that is in itself
    // the head of a linked list of moves (handling the n-nary part).


    // Tell the AI to start searching for a move. Just an extra level of abstraction so the high level methods look slightly cleaner
    private void StartSearchForMove()
    {
        if (!isClearingTree && !isAdvancing)
        {
            StartTreeConstruction(isPlayingWhite);
        }
    }

    // Tree construction and freeing is driven by certain flags in update function. So on update we
    // evaluate the flags and determine which, if any, of the iterative processes to advance.
    private void Update()
    {
        if (!isClearingTree && isAdvancing)
        {
            for (int i = 0; i < STEPS_PER_FRAME; i++)
            {
                AdvanceTreeConstruction();
            }
        }
        if (isClearingTree)
        {
            for (int i = 0; i < STEPS_PER_FRAME; i++)
            {
                AdvanceFreeingTree();
            }

        }
    }

    // Begin the Tree Construction. TODO: Put the tree freeing logic into the start of this
    private void StartTreeConstruction(bool isWhite)
    {
        // Only do something if not already busy to avoid memory leakage
        if (!isClearingTree && !isAdvancing)
        {
            LoadBoardState(root_board);
            if (gbl_tree_root_ref[0] != NULL) { AddNullRoot(gbl_tree_root_ref); }
            gbl_sentinal_ref[0] = gbl_tree_root_ref[0];
            gbl_current_depth_ref[0] = 0;
            gbl_current_move_ref[0] = 0;
            gbl_current_piece_ref[0] = 0;
            gbl_current_breadth_ref[0] = 0;
            gbl_state_ref = (int[])board_state.Clone();
            int color = isWhite ? 1 : 0;
            gbl_color_ref[0] = color;
            FillPieceBuffer(gbl_state_ref, true);
            FillMoveBuffer(gbl_state_ref, pieces_to_move[0]);
            isAdvancing = true;
            move_chosen_buffer = 0;
        }
        else
        {
            Debug.Log("Warning, tree contruction currently busy. If you continue getting this, its probably crashed irreparably");
        }
    }

    // Add an empty node as the root node to the move tree.
    private void AddNullRoot(int[] tree_root_pointer)
    {
        int address = GetFreeMemoryAddress();
        tree_root_pointer[0] = address;

        // Null out the roots next, parent, and child pointers
        next_array[address] = NULL;
        parent_array[address] = NULL;
        child_array[address] = NULL;

        // Set its move and value entries to 0. Note that move_array usually holds encoded moves, meaning the encoded move should be
        // "move piece at index 0 to index 0" WARNING: this could just delete the piece at square 0 if executed depending on the
        // implementation of the method.
        move_array[address] = 0;
        value_array[address] = 0;
    }

    // Advance the tree construction if we are able to. Doesnt repeat main logic if called multiple times a frame after failing
    private void AdvanceTreeConstruction()
    {
        if (isAdvancing)
        {
            // if we can, advance the tree, and store the advancability into isAdvancing.
            isAdvancing = AddNextMoveToTreeAlt(gbl_sentinal_ref, gbl_current_piece_ref, gbl_current_move_ref, gbl_current_depth_ref, gbl_current_breadth_ref, gbl_state_ref, isPlayingWhite, SEARCH_BREADTH, SEARCH_DEPTH);
            if (!isAdvancing)
            {
                // If it failed, then end the tree construction
                EndTreeConstruction();
            }
        }
    }

    // So how do you handle n-nary trees without recursion? A bunch of inout variables. And how do you handle inout variables without inout or pass by reference?
    // Integer arrays of length one. Takes in a bunch of integer arrays of length one to pass variables by reference, and executes one step in constructing the
    // move tree, return false when no more steps are to be taken
    private bool AddNextMoveToTreeAlt(int[] sentinal_ref, int[] current_piece_ref, int[] current_move_ref, int[] current_depth_ref, int[] current_breadth_ref, int[] state_ref, bool isPlayingWhite, int breadth, int depth)
    {
        bool isStartingBlack = !isPlayingWhite;

        // Unload the references into local scope so we don't need to keep using the array notation.
        // All 5 of these variables must be kept track of from iteration from iteration. This allows
        // us to call iterations over multiple frames, but also multiple iterations over a single frame.
        int sentinal = sentinal_ref[0];
        int current_piece = current_piece_ref[0];
        int current_move = current_move_ref[0];
        int current_depth = current_depth_ref[0];
        int current_breadth = current_breadth_ref[0];

        // breadth denotes how many children each node should have. The current breadth keeps track of how many children we've ADDED, not the child we are at
        // If we are still able to add child nodes to the sentinal's current node
        if (current_breadth < breadth)
        {
            // If we havent added any children, nor have we looked through any pieces
            if (current_breadth == 0 && current_piece == 0)
            {
                // Assume that we dont have any pieces set up, and do a piece search.
                int color_parity = current_depth & 1;
                bool isSearchingWhite = (color_parity == 0) == isStartingBlack;
                FillPieceBuffer(state_ref, isSearchingWhite);
            }
            // If we havent evaluated any moves yet and have more pieces, then fill the move buffer with our current pieces moves. Note that if it cant find any
            // pieces, it will simply skip over this step, leaving the move_count at 0.
            if (current_move == 0 && current_piece < pieces_count)
            {
                FillMoveBuffer(state_ref, pieces_to_move[current_piece]);
            }
            // If we still have moves to evaluate
            if (current_move < move_count)
            {
                // Add the current move being evaluated to the move tree
                int move = move_buffer[current_move];
                int value = point_table[decode_captured(move)];
                AddChildToMoveTree(sentinal, move_buffer[current_move], (value_array[sentinal] + value) );

                // Increase the current breadth (number of nodes added), and move on to the next move (without evaluating)
                current_breadth++;
                current_move++;
            }
            // Otherwise, move on to the next piece for the next time around, and clear the move buffer. Note this is the first else statement so far
            else
            {
                current_piece++;
                current_move = 0;
                ClearMoveBuffer();
            }
            // if our current piece is out of bounds of our piece array (which happens immediately after the previous else statement if we run out of moves)
            if (current_piece >= pieces_count)
            {
                // Clear both buffers, and set the current breadth to the maximum, effectively exiting the first half of the method
                ClearMoveBuffer();
                ClearPieceRefsFromPieceBuffer();
                current_breadth = breadth;
            }
        }
        // If we have already added the maximum number of nodes, or ran out of moves to evaluate then begin the logic for moving the sentinal
        // onto the next part of the tree.
        else
        {
            // Reset both the move buffers, the number of nodes added, and the current move and pieces index.
            ClearMoveBuffer();
            ClearPieceRefsFromPieceBuffer();
            current_breadth = 0;
            current_move = 0;
            current_piece = 0;

            // Check if we can continue increasing depth of the tree, and that there is a child of the current sentinal.
            // The child node would theoretically be null if no moves could be made after the previous node (nodes representing encoded moves)
            if (current_depth < depth && child_array[sentinal] != NULL)
            {
                sentinal = child_array[sentinal];
                ApplyEncodedMovement(state_ref, move_array[sentinal]);
                current_depth++;
            }
            // If we cant continue increasing depth, we begin assesing if we can traverse to the next sentinal or not.
            else
            {
                // This loop brings us as high up the tree as we can go without finding a "next" node to traverse into, the assumption
                // being that we only ever move the sentinal forward, and we only do so after the sentinal's node has been evaluated.
                // So if there's no where forward to go, we've evaluated everything on that branch.
                while (parent_array[sentinal] != NULL && next_array[sentinal] == NULL)
                {
                    // If we still have a parent array to go up to (not to actually travel to, but to know if we have to evalute the children nodes.
                    // Evaluating the children of the nodes means taking the minimax of them, and propegating it to the parent. Therefore, we must
                    // skip that step if we are at the very top node (our empty placeholder) to avoid freeing the current possible moves from memory
                    if (parent_array[sentinal] != NULL)
                    {
                        // Some over complicated parity math to determine to minimize or maximize the mini-max algorithm.
                        int color_parity = ( current_depth ) & 1;
                        bool isWhite = (color_parity == 1) == isStartingBlack;
                        bool isMinning = isWhite;
                        int parent_value = value_array[sentinal];

                        // Since we just moved up into this node, all its children have been added. Since this is the case, we'll evaluate the children.
                        int child_value = EvaluateChildren(sentinal, isMinning, current_depth, isPlayingWhite, isWhite, parent_value, state_ref);
                        value_array[sentinal] = child_value;

                        // After evaluating, free the children nodes from memory to save space (exponentially)
                        FreeNodesChildren(sentinal);
                    }

                    // Prepare to move up along the tree by reverting the motion of the current node, then actually move up and update the current depth
                    RevertEncodedMovement(state_ref, move_array[sentinal]);
                    sentinal = parent_array[sentinal];
                    current_depth = current_depth - 1;
                }
                // After we've detected the node we're at has more nodes ahead of it to evaluate and stopped traveling up the tree
                if (next_array[sentinal] != NULL)
                {
                    // Update the value of the node we ended up landing on, since we just exited from its children
                    if (parent_array[sentinal] != NULL)
                    {
                        // Some over complicated parity math to determine to minimize or maximize the mini-max algorithm.
                        int color_parity = (current_depth) & 1;
                        bool isWhite = (color_parity == 1) == isStartingBlack;
                        bool isMinning = isWhite;
                        int parent_value = value_array[sentinal];

                        // Since we just moved up into this node, all its children have been added. Since this is the case, we'll evaluate the children.
                        int child_value = EvaluateChildren(sentinal, isMinning, current_depth, isPlayingWhite, isWhite, parent_value, state_ref);
                        value_array[sentinal] = child_value;

                        // After evaluating, free the children nodes from memory to save space (exponentially)
                        FreeNodesChildren(sentinal);
                    }
                    // To avoid tricking the algorithm into going straight back down on the next pass, move the sentinal to the next node in the array,
                    // updating the board state as we go
                    RevertEncodedMovement(state_ref, move_array[sentinal]);
                    sentinal = next_array[sentinal];
                    ApplyEncodedMovement(state_ref, move_array[sentinal]);
                }
                // If we've exited to a node that has no parent (the very top node), we know it also has no next node. So if we cant find
                // any parent or next node, simply return false since we've made it to the end of the tree. Note that this doesnt run immediately
                // because this second half of the method runs only after running out of breadth, then we move into the children node before checking
                // this in any given pass, unless one side has no moves to be made at all.
                else
                {
                    // End the iterative algorithm by returning false
                    return false;
                }
            }
        }

        // Export the updated local variables to their global scope.
        sentinal_ref[0] = sentinal;
        current_piece_ref[0] = current_piece;
        current_move_ref[0] = current_move;
        current_depth_ref[0] = current_depth;
        current_breadth_ref[0] = current_breadth;

        // Return true for a succesful iteration
        return true;
    }

    // Fill the buffer with piece references that the current evaluating player can hypothetically mvoe
    private void FillPieceBuffer(int[] board_state, bool isBlack)
    {
        // Clear the piece buffer
        ClearPieceRefsFromPieceBuffer();
        for (int i = 0; i < board_state.Length; i++)
        {
            // And for any piece that belongs to our player, add it to the piece buffer
            if (isBlackPiece(board_state[i]) == isBlack)
            {
                // TODO: add extra processing to make better moves early on in-case tree can be refactored to allow alpha-beta pruning
                AddPieceRefToPieceBuffer(i);
            }
        }
    }

    // Add a child node to the move tree with the specified move and value, managing all of its new pointers
    private void AddChildToMoveTree(int parent_address, int move, int value)
    {
        // If we are adding the node to a valid parent node
        if (parent_address != NULL)
        {
            // Allocate a new memory address for the node
            int child_address = GetFreeMemoryAddress();

            // If we got a valid memory address from the memory stack
            if (child_address != NULL)
            {
                // encode the move and value to the new node
                move_array[child_address] = move;
                value_array[child_address] = value;

                // Set the next array of the child to be the previous main child of the parent. If there was no child already, this sets the
                // next array pointer to be NULL, which is expected behavior, and is used to detect whether or not to terminate branches
                next_array[child_address] = child_array[parent_address];

                // Set the child's child pointer to be NULL, as it can't possibly have any children since it was just added
                child_array[child_address] = NULL;

                // link the parent and child addresses together with child and address pointers, respectively
                parent_array[child_address] = parent_address;
                child_array[parent_address] = child_address;

                //// If the parent does not have any child nodes alread
                //if (child_array[parent_adress] == NULL)
                //{
                //    child_array[parent_adress] = child_address;
                //    child_array[child_address] = NULL;
                //    parent_array[child_address] = parent_adress;
                //    next_array[child_address] = NULL;
                //}
                //else
                //{
                //    next_array[child_address] = child_array[parent_adress];
                //    child_array[parent_adress] = child_address;
                //    parent_array[child_address] = parent_adress;
                //    child_array[child_address] = NULL;

                //}
            }
        }
    }

    // Given a specified tree node, iterate through its children based and find the minimax of the children's scores
    private int EvaluateChildren(int node, bool isMinning, int depth, bool isWhiteStart, bool isWhitePlaying, int parent_value, int[] state)
    {

        // If neither our supplied node nor its child are null
        if (node != NULL && child_array[node] != NULL)
        {
            // Drop a sentinal into the child of the supplied node
            int sentinal = child_array[node];

            // Keep track of the minimum and maximum values of the sentinal. Techinally we can make a conditional and only keep track of one, but its
            // easier to debug the parity math if we can print out both of them, the decision it made, and other useful info like depth and color
            int max_value = value_array[sentinal];
            int min_value = value_array[sentinal];
            
            // This counter is a safegaurd against infinite loops. Just makes sure we arent spending longer than is possible iterating through nodes
            // Can probably be removed, since it was simply added for some debugging cases.
            int counter = 0;

            // While we have a node to traverse forwards into
            while (next_array[sentinal] != NULL && counter < SEARCH_BREADTH + 1)
            {
                // Move on into the next node
                sentinal = next_array[sentinal];

                // compare the minimax values of the new node with the min and max
                min_value = Mathf.Min(min_value, value_array[sentinal]);
                max_value = Mathf.Max(max_value, value_array[sentinal]);
                counter++;
            }
            // If we are minimizing, return the minimum, if not return the maximum.
            if (isMinning)
            {
                return min_value;
            }
            else
            {
                return max_value;
            }
        }
        // if we fail to find the node or its children, throw a log error.
        else
        {
            Debug.LogError("Warning, attempting to evaluate nodes without children");
            return 0;
        }
    }

    // Final cleanup process after ending the tree construction
    private void EndTreeConstruction()
    {
        // Search the 1st level of the tree (after the empty root node) for immediately possible moves of minimax'd scores.
        // Loads all moves that tie in score into an array as to randomly pick one out.
        FillGoodMoves(gbl_tree_root_ref[0], isPlayingWhite);
        //EvaluateChildren(gbl_tree_root_ref[0], isPlayingWhite, 0, isPlayingWhite, isPlayingWhite, 0, board_state);

        // pick a random move that ties with the best scored one.
        int return_move = good_moves[Random.Range(0, good_moves_count)];
        move_chosen_buffer = return_move;

        // And free the move tree from memory since its no longer needed
        StartFreeingTree();
    }


    // Iterate through the immediately possible moves to determine the best one based on a minimax
    private void FillGoodMoves(int tree_head, bool isMinning)
    {
        // Reset the good move buffer in which we'll store moves
        ClearGoodMoves();

        // Drop a sentinal at the trees head
        int sentinal = tree_head;

        // If there is a tree to evaluate
        if (sentinal != NULL)
        {
            // And that tree has immediate moves to evaluate
            if (child_array[tree_head] != NULL)
            {
                // Move into the depth containing our immediate possible moves
                sentinal = child_array[tree_head];

                // Set a baseline as the value of the first move we dropped into
                int target_value = value_array[sentinal];
                
                // minimizing case
                if (isMinning)
                {
                    // As long as we have next pointers to traverse through, traverse and compare values, storing the minimax
                    while (next_array[sentinal] != NULL)
                    {
                        sentinal = next_array[sentinal];
                        target_value = Mathf.Min(target_value, value_array[sentinal]);
                    }
                }
                // maximizing case
                else
                {
                    // As long as we have next pointers to traverse through, traverse and compare values, storing the minimax
                    while (next_array[sentinal] != NULL)
                    {
                        sentinal = next_array[sentinal];
                        target_value = Mathf.Max(target_value, value_array[sentinal]);
                    }
                }

                // Once we're done with iterating through all the children to determine our target value
                sentinal = child_array[tree_head];
                while (sentinal != NULL)
                {
                    // iterat through them again, adding any moves with tied scores into the value array
                    if (value_array[sentinal] == target_value)
                    {
                        AddToGoodMoves(move_array[sentinal]);
                    }
                    // Advance the sentinal for the next run around.
                    sentinal = next_array[sentinal];
                }
            }
        }
    }

    // Method to start freeing the tree from memory. Also distributes iterations across frames, but only requires a single sentinal
    // as the inout variable.
    private void StartFreeingTree()
    {
        // Flag the isClrearingTree bool and set up a sentinal at the root of the tree.
        isClearingTree = true;
        gbl_garbage_collector[0] = gbl_tree_root_ref[0];
    }

    // Iterate the next step of freeing the tree from memory
    private void AdvanceFreeingTree()
    {
        // If we are clearing the tree
        if (isClearingTree)
        {
            // Advance the sentinal to the next node, and free its children
            gbl_garbage_collector[0] = FreeNextTreeNodeAndReturnSentinal(gbl_garbage_collector[0]);
            
            // If we reach the very root of the tree
            if (gbl_garbage_collector[0] == gbl_tree_root_ref[0])
            {
                // Also free the children of the trees root
                FreeNodesChildren(gbl_tree_root_ref[0]);
                
                // And flag we are no longer clearing to avoid multiple executions while running several iterations a frame
                isClearingTree = false;
                
                // Run the final cleanup logic
                EndFreeingTree();
            }
        }
    }

    // Advances the sentinal above and removes the children from the new node
    private int FreeNextTreeNodeAndReturnSentinal(int sentinal)
    {
        // if we have a child to advance to
        if (child_array[sentinal] != NULL)
        {
            // Advance down the tree as far as possible
            while (child_array[sentinal] != NULL)
            {
                sentinal = child_array[sentinal];
            }
            // once we hit the very bottom, if we are able to go up (i.e. we have a tree with at least one child of the empty root node)
            if (parent_array[sentinal] != NULL)
            {
                // move back up to the parent of the child
                sentinal = parent_array[sentinal];
            }
            // and free all the children of the node.
            FreeNodesChildren(sentinal);
        }
        // If we can then traverse to a next node
        if (next_array[sentinal] != NULL)
        {
            // then do so
            sentinal = next_array[sentinal];
        }
        // otherwise, move the sentinal to the parent of the node. Not sure if this is actually necessary, since the next iteration will just
        // move down through any available children anyway, but the overhead cost is relatively low and I'm not sure if I can remove this
        else if (parent_array[sentinal] != NULL)
        {
            sentinal = parent_array[sentinal];
        }
        // Return the address of where the sentinal finally ended up.
        return sentinal;
    }
    

    // Free the memory addresses of the node children (and all their pointers!)
    private void FreeNodesChildren(int address)
    {
        // While we still have children to free
        while (child_array[address] != NULL)
        {
            // Keeping freeing them children
            FreeNodesChild(address);
        }
    }

    // WARNING: Do no use FreeNode on nodes with children, it will NOT free the nodes children recursively and cause memory leak
    // Frees the memory address of a nodes main child, moving the main child pointer onto the next child if it exists
    private void FreeNodesChild(int address)
    {
        // If we have a child to free
        if (child_array[address] != NULL)
        {
            // get the child pointer of the address supplied
            int child_pointer = child_array[address];

            // update the child pointer to be the node after the current child node (which can be NULL)
            child_array[address] = next_array[child_array[address]];

            // And free the memory address of the child pointer, returning it to the memory stack
            FreeMemoryAddress(child_pointer);
        }
    }

    // Final cleanup of freeing the move tree from memory
    private void EndFreeingTree()
    {
        // Free the memory address of the root tree
        FreeMemoryAddress(gbl_tree_root_ref[0]);

        OnMoveFound();
    }

    ///////////////////////////////////////////
    ///                                     ///
    ///          Piece Searching            ///
    ///                                     ///
    ///////////////////////////////////////////

    // Methods used for finding various pieces on the board state



    // Takes in starting coordinate, and the index of the forward and lateral direction in which to cast in.
    // Stores casting result in the array cast_results, and takes in the current board state to cast within.
    // Stores NULL as the casting result if it starts going out of bounds
    private void CastForPiece(int x, int y, int z, int w, int lateral, int forward, int[] cast_results, int[] state)
    {
        int px = x;
        int py = y;
        int pz = z;
        int pw = w;

        int dx = 0;
        int dy = 0;
        int dz = 0;
        int dw = 0;

        if (lateral < laterals_x.Length)
        {
            dx = laterals_x[lateral];
            dy = laterals_y[lateral];
            dz = laterals_z[lateral];
            dw = laterals_w[lateral];
        }
        if (forward < forwards_x.Length)
        {
            dx += forwards_x[forward];
            dy += forwards_y[forward];
            dz += forwards_z[forward];
            dw += forwards_w[forward];
        }
        for (int i = 0; i < cast_results.Length; i++)
        {
            px += dx;
            py += dy;
            pz += dz;
            pw += dw;
            cast_results[i] = NULL;
            if (!isOutOfBounds(px, py, pz, pw))
            {
                cast_results[i] = state[CoordToIndex(px, py, pz, pw)];
            }
        }
        //return cast_results;
    }


    ///////////////////////////////////////////
    ///                                     ///
    ///      Evaluator initialization       ///
    ///                                     ///
    ///////////////////////////////////////////

    // idk man, it initializes the evaluator



    // Initialize The evaluator. Pretty High level I guess? I dont know where to put this
    public void InitializeEvaluator(Chess4DBoard initializing_board)
    {
        root_board = initializing_board;
        LoadBoardState(root_board);

        // Initialize two simulated memory blocks for the encoded moves (using 24 bits) and the value of said move.
        // Values should be designed to avoid overflow, which shouldnt be too hard with one king.
        move_array = new int[SYSTEM_VMEM];
        value_array = new int[SYSTEM_VMEM];

        // Initialize three simulated memory blocks for pointers to construct our tree. Note that each parent will only
        // have one child pointer, and that child will be the head of a linked list constructed with next_array
        parent_array = new int[SYSTEM_VMEM];
        child_array = new int[SYSTEM_VMEM];
        next_array = new int[SYSTEM_VMEM];
        
        // Initialize the memory stack. This will be used to keep track of what memory addresses (of the simulated blocks)
        // are still available. Will be treated as a stack, first in, last out.
        memory_stack_next = new int[SYSTEM_VMEM];

        // Initialize a static (unchanging length) array with a manual element count. SEARCH_BREADTH denotes the maximum
        // number of moves to be evaluated at each step, so we should never expect more moves than that.
        good_moves = new int[SEARCH_BREADTH];
        good_moves_count = 0;

        // Okay this one is really cursed, to avoid 1 indexing the simulated memory blocks, we will treat the last element
        // of the array to be our "null" pointer. note that NULL is an int, wheras null is typeNone (or the c# equivalent at least).
        NULL = SYSTEM_VMEM - 1;

        // And of course, since the NULL'th element of an array doesnt exist, how could it point to something?
        move_array[NULL] = NULL;
        parent_array[NULL] = NULL;

        value_array[NULL] = NULL;
        next_array[NULL] = NULL;
        child_array[NULL] = NULL;

        // I think this is being used as the root of the move tree but I'm not sure. Might be leftover from a linked list or something
        moves_head[0] = NULL;

        // Initialize an array of length one to serve as the memory stack head. Using an array for udon compatible inout variables.
        memory_stack_head = new int[1];
        memory_stack_head[0] = NULL;

        // Initialize our memory
        InitializeMemory();

        // Reset our virtual memory usage to keep track of
        VMEM_USAGE = 0;

        // call the initialization event for any other tasks
        OnInitialization();
    }


    ///////////////////////////////////////////
    ///                                     ///
    ///    Simulated Memory manipulation    ///
    ///                                     ///
    ///////////////////////////////////////////

    // This program is simulating several memory blocks with integer arrays. The main memory block
    // is move_array, but its paired with several others for some pretty extreme struct-like instancing



    // Initialize our simulated memory
    private void InitializeMemory()
    {
        for (int i = 0; i < SYSTEM_VMEM - 1; i++)
        {
            FreeMemoryAddress(i);
        }
    }

    // Get the and return next free memory address
    private int GetFreeMemoryAddress()
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

    // Free the specified memory address. No I'm not implementing a garbage collector
    private void FreeMemoryAddress(int address)
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


    ///////////////////////////////////////////
    ///                                     ///
    ///      Board state manipulation       ///
    ///                                     ///
    ///////////////////////////////////////////

    // A board can be boiled down to an integer array, int this case (that was a pun), of length 256 for 4x4x4x4 board



    // Applies an encoded motion to a specified board state, effectively executing the motion
    private void ApplyEncodedMovement(int[] state, int encoded_movment)
    {
        int from = decode_from(encoded_movment);
        int to = decode_to(encoded_movment);
        int temp = state[from];
        state[from] = 0;
        state[to] = temp;
    }

    // Reverts an encoded motion to a specified board state. Requires captured pieces to be encoded into the move
    private void RevertEncodedMovement(int[] state, int encoded_movment)
    {
        int from = decode_from(encoded_movment);
        int to = decode_to(encoded_movment);
        int captured = decode_captured(encoded_movment);
        int temp = state[to];
        state[to] = captured;
        state[from] = temp;
    }

    // load the board state from a specified chess board into the global board state
    private void LoadBoardState(Chess4DBoard board)
    {
        board_state = board.GetSquareArray();
    }


    ///////////////////////////////////////////
    ///                                     ///
    ///       Small Helper Functions        ///
    ///                                     ///
    ///////////////////////////////////////////

    // Small functions that can be summarized in one or two sentences, usualy performing just basic checks or calculations

    

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

    // take the piece type in, and output a string corresponding to its name.
    public string GetPieceString(int piece)
    {
        int piece_type = PieceTypeColorless(piece);
        if (piece_type == 1)
        {
            return "King";
        }
        else if (piece_type == 2)
        {
            return "Queen";
        }
        else if (piece_type == 3)
        {
            return "Bishop";
        }
        else if (piece_type == 4)
        {
            return "Knight";
        }
        else if (piece_type == 5)
        {
            return "Rook";
        }
        else if (piece_type == 6)
        {
            return "Pawn";
        }
        else if (piece_type == 0)
        {
            return "Nothing";
        }
        else
        {
            return "Error";
        }
    }

    // Do not call as it will crash the program
    private void CrashProgram()
    {
        GameObject obj = null;
        print(obj.name);
    }

    // Make a coordinate into a nice string to output
    private string CalculateCoordinateString(int x, int y, int z, int w)
    {
        return "( " + x + ", " + y + ", " + z + ", " + w + " )";
    }

    // Determines if a pieces is black without checking for empties
    public bool isBlackPiece(int piece_id)
    {
        return (piece_id < 7);
    }

    // Gets the piece type, stripping out color info. Refer to whatever documentation I end up coming up with
    public int PieceTypeColorless(int piece_type)
    {
        if (piece_type == 0) { return 0; }
        return ((piece_type - 1) % 6) + 1;
    }

    // Gets the piece color as an integer, either 0 or 1.
    public int PieceColor(int piece_type)
    {
        if (isBlackPiece(piece_type))
        {
            return 0;
        }
        return 1;
    }

    // Determines if a coordinate is out of bounds
    public bool isOutOfBounds(int xi, int yi, int zi, int wi)
    {
        bool x_out = xi < 0 || 3 < xi;
        bool y_out = yi < 0 || 3 < yi;
        bool z_out = zi < 0 || 3 < zi;
        bool w_out = wi < 0 || 3 < wi;

        return x_out || y_out || z_out || w_out;
    }

    // Calculates the index of a coordinate without checking bounds
    public int CoordToIndex(int xi, int yi, int zi, int wi)
    {
        return ((xi & 3 ) << 0) + ((yi & 3) << 2) + ((zi & 3) << 4) + ((wi & 3) << 6);
    }


    ///////////////////////////////////////////
    ///                                     ///
    ///            Pasta Section            ///
    ///                                     ///
    ///////////////////////////////////////////

    // For only the most exquisite pastas. Angel hair, spagetti, udon noodles, this shit should come with a strainer



    // The worst function in this code. Fills the move buffer based on the pieces in the supplied state.
    // Mostly just copy pasted together without worrying about abstraction or code base.
    private void FillMoveBuffer(int[] state, int square)
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
            for (int i = 0; i < laterals_x.Length; i++)
            {
                for (int j = 0; j < forwards_x.Length; j++)
                {
                    int lateral = i;
                    int forward = j;
                    bool occluded = false;
                    CastForPiece(sx, sy, sz, sw, lateral, forward, cast_results, state);
                    for (int k = 0; k < 1; k++)
                    {
                        int result = cast_results[k];
                        // Our results are NULL when casting out of bounds
                        if (result != NULL && !occluded)
                        {
                            // so therefore if it isnt NULL, we are within bounds for the cast;
                            if (result == 0)
                            {
                                target_index = CoordToIndex(sx + laterals_x[i] * (k + 1) + forwards_x[j] * (k + 1),
                                                            sy + laterals_y[i] * (k + 1) + forwards_y[j] * (k + 1),
                                                            sz + laterals_z[i] * (k + 1) + forwards_z[j] * (k + 1),
                                                            sw + laterals_w[i] * (k + 1) + forwards_w[j] * (k + 1));

                                // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                                int encoded_movement = encode_movement(square, target_index, 0);
                                move_buffer[move_count] = encoded_movement;
                                move_count++;
                            }
                            else if (isBlackPiece(result) == isBlackPiece(piece))
                            {
                                occluded = true;
                            }
                            else if (isBlackPiece(result) != isBlackPiece(piece))
                            {
                                occluded = true;
                                target_index = CoordToIndex(sx + laterals_x[i] * (k + 1) + forwards_x[j] * (k + 1),
                                                            sy + laterals_y[i] * (k + 1) + forwards_y[j] * (k + 1),
                                                            sz + laterals_z[i] * (k + 1) + forwards_z[j] * (k + 1),
                                                            sw + laterals_w[i] * (k + 1) + forwards_w[j] * (k + 1));

                                int encoded_movement = encode_movement(square, target_index, state[target_index]);
                                move_buffer[move_count] = encoded_movement;
                                move_count++;
                            }
                        }
                    }
                }

            }
            for (int i = 0; i < laterals_x.Length; i++)
            {
                int lateral = i;
                int forward = NULL;
                bool occluded = false;
                ClearCast();
                CastForPiece(sx, sy, sz, sw, lateral, forward, cast_results, state);
                for (int j = 0; j < 1; j++)
                {
                    int result = cast_results[j];
                    // Our results are NULL when casting out of bounds
                    if (result != NULL && !occluded)
                    {
                        // so therefore if it isnt NULL, we are within bounds for the cast;
                        if (result == 0)
                        {
                            target_index = CoordToIndex(sx + laterals_x[i] * (j + 1),
                                                        sy + laterals_y[i] * (j + 1),
                                                        sz + laterals_z[i] * (j + 1),
                                                        sw + laterals_w[i] * (j + 1));

                            // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                            int encoded_movement = encode_movement(square, target_index, 0);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                        else if (isBlackPiece(result) == isBlackPiece(piece))
                        {
                            occluded = true;
                        }
                        else if (isBlackPiece(result) != isBlackPiece(piece))
                        {
                            occluded = true;
                            target_index = CoordToIndex(sx + laterals_x[i] * (j + 1),
                                                        sy + laterals_y[i] * (j + 1),
                                                        sz + laterals_z[i] * (j + 1),
                                                        sw + laterals_w[i] * (j + 1));

                            int encoded_movement = encode_movement(square, target_index, state[target_index]);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                    }
                }

                lateral = NULL;
                forward = i;
                occluded = false;
                ClearCast();
                CastForPiece(sx, sy, sz, sw, lateral, forward, cast_results, state);
                for (int j = 0; j < 1; j++)
                {
                    int result = cast_results[j];
                    // Our results are NULL when casting out of bounds
                    if (result != NULL && !occluded)
                    {
                        // so therefore if it isnt NULL, we are within bounds for the cast;
                        if (result == 0)
                        {
                            target_index = CoordToIndex(sx + forwards_x[i] * (j + 1),
                                                        sy + forwards_y[i] * (j + 1),
                                                        sz + forwards_z[i] * (j + 1),
                                                        sw + forwards_w[i] * (j + 1));

                            // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                            int encoded_movement = encode_movement(square, target_index, 0);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                        else if (isBlackPiece(result) == isBlackPiece(piece))
                        {
                            occluded = true;
                        }
                        else if (isBlackPiece(result) != isBlackPiece(piece))
                        {
                            occluded = true;
                            target_index = CoordToIndex(sx + forwards_x[i] * (j + 1),
                                                        sy + forwards_y[i] * (j + 1),
                                                        sz + forwards_z[i] * (j + 1),
                                                        sw + forwards_w[i] * (j + 1));

                            int encoded_movement = encode_movement(square, target_index, state[target_index]);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                    }
                }
            }
        }
        else if (piece_type == 2)
        {
            for (int i = 0; i < laterals_x.Length; i++)
            {
                for (int j = 0; j < forwards_x.Length; j++)
                {
                    int lateral = i;
                    int forward = j;
                    bool occluded = false;
                    CastForPiece(sx, sy, sz, sw, lateral, forward, cast_results, state);
                    for (int k = 0; k < cast_results.Length; k++)
                    {
                        int result = cast_results[k];
                        // Our results are NULL when casting out of bounds
                        if (result != NULL && !occluded)
                        {
                            // so therefore if it isnt NULL, we are within bounds for the cast;
                            if (result == 0)
                            {
                                target_index = CoordToIndex(sx + laterals_x[i] * (k + 1) + forwards_x[j] * (k + 1),
                                                            sy + laterals_y[i] * (k + 1) + forwards_y[j] * (k + 1),
                                                            sz + laterals_z[i] * (k + 1) + forwards_z[j] * (k + 1),
                                                            sw + laterals_w[i] * (k + 1) + forwards_w[j] * (k + 1));

                                // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                                int encoded_movement = encode_movement(square, target_index, 0);
                                move_buffer[move_count] = encoded_movement;
                                move_count++;
                            }
                            else if (isBlackPiece(result) == isBlackPiece(piece))
                            {
                                occluded = true;
                            }
                            else if (isBlackPiece(result) != isBlackPiece(piece))
                            {
                                occluded = true;
                                target_index = CoordToIndex(sx + laterals_x[i] * (k + 1) + forwards_x[j] * (k + 1),
                                                            sy + laterals_y[i] * (k + 1) + forwards_y[j] * (k + 1),
                                                            sz + laterals_z[i] * (k + 1) + forwards_z[j] * (k + 1),
                                                            sw + laterals_w[i] * (k + 1) + forwards_w[j] * (k + 1));

                                int encoded_movement = encode_movement(square, target_index, state[target_index]);
                                move_buffer[move_count] = encoded_movement;
                                move_count++;
                            }
                        }
                    }
                }

            }
            for (int i = 0; i < laterals_x.Length; i++)
            {
                int lateral = i;
                int forward = NULL;
                bool occluded = false;
                ClearCast();
                CastForPiece(sx, sy, sz, sw, lateral, forward, cast_results, state);
                for (int j = 0; j < cast_results.Length; j++)
                {
                    int result = cast_results[j];
                    // Our results are NULL when casting out of bounds
                    if (result != NULL && !occluded)
                    {
                        // so therefore if it isnt NULL, we are within bounds for the cast;
                        if (result == 0)
                        {
                            target_index = CoordToIndex(sx + laterals_x[i]*(j+1),
                                                        sy + laterals_y[i]*(j+1),
                                                        sz + laterals_z[i]*(j+1),
                                                        sw + laterals_w[i]*(j+1));

                            // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                            int encoded_movement = encode_movement(square, target_index, 0);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                        else if (isBlackPiece(result) == isBlackPiece(piece))
                        {
                            occluded = true;
                        }
                        else if (isBlackPiece(result) != isBlackPiece(piece))
                        {
                            occluded = true;
                            target_index = CoordToIndex(sx + laterals_x[i] * (j + 1),
                                                        sy + laterals_y[i] * (j + 1),
                                                        sz + laterals_z[i] * (j + 1),
                                                        sw + laterals_w[i] * (j + 1));

                            int encoded_movement = encode_movement(square, target_index, state[target_index]);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                    }
                }

                lateral = NULL;
                forward = i;
                occluded = false;
                ClearCast();
                CastForPiece(sx, sy, sz, sw, lateral, forward, cast_results, state);
                for (int j = 0; j < cast_results.Length; j++)
                {
                    int result = cast_results[j];
                    // Our results are NULL when casting out of bounds
                    if (result != NULL && !occluded)
                    {
                        // so therefore if it isnt NULL, we are within bounds for the cast;
                        if (result == 0)
                        {
                            target_index = CoordToIndex(sx + forwards_x[i] * (j + 1),
                                                        sy + forwards_y[i] * (j + 1),
                                                        sz + forwards_z[i] * (j + 1),
                                                        sw + forwards_w[i] * (j + 1));

                            // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                            int encoded_movement = encode_movement(square, target_index, 0);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                        else if (isBlackPiece(result) == isBlackPiece(piece))
                        {
                            occluded = true;
                        }
                        else if (isBlackPiece(result) != isBlackPiece(piece))
                        {
                            occluded = true;
                            target_index = CoordToIndex(sx + forwards_x[i] * (j + 1),
                                                        sy + forwards_y[i] * (j + 1),
                                                        sz + forwards_z[i] * (j + 1),
                                                        sw + forwards_w[i] * (j + 1));

                            int encoded_movement = encode_movement(square, target_index, state[target_index]);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                    }
                }
            }
        }
        else if (piece_type == 3)
        {
            for (int i = 0; i < laterals_x.Length; i++)
            {
                for (int j = 0; j < forwards_x.Length; j++)
                {
                    int lateral = i;
                    int forward = j;
                    bool occluded = false;
                    CastForPiece(sx, sy, sz, sw, lateral, forward, cast_results, state);
                    for (int k = 0; k < cast_results.Length; k++)
                    {
                        int result = cast_results[k];
                        // Our results are NULL when casting out of bounds
                        if (result != NULL && !occluded)
                        {
                            // so therefore if it isnt NULL, we are within bounds for the cast;
                            if (result == 0)
                            {
                                target_index = CoordToIndex(sx + laterals_x[i] * (k + 1) + forwards_x[j] * (k + 1),
                                                            sy + laterals_y[i] * (k + 1) + forwards_y[j] * (k + 1),
                                                            sz + laterals_z[i] * (k + 1) + forwards_z[j] * (k + 1),
                                                            sw + laterals_w[i] * (k + 1) + forwards_w[j] * (k + 1));

                                // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                                int encoded_movement = encode_movement(square, target_index, 0);
                                move_buffer[move_count] = encoded_movement;
                                move_count++;
                            }
                            else if (isBlackPiece(result) == isBlackPiece(piece))
                            {
                                occluded = true;
                            }
                            else if (isBlackPiece(result) != isBlackPiece(piece))
                            {
                                occluded = true;
                                target_index = CoordToIndex(sx + laterals_x[i] * (k + 1) + forwards_x[j] * (k + 1),
                                                            sy + laterals_y[i] * (k + 1) + forwards_y[j] * (k + 1),
                                                            sz + laterals_z[i] * (k + 1) + forwards_z[j] * (k + 1),
                                                            sw + laterals_w[i] * (k + 1) + forwards_w[j] * (k + 1));

                                int encoded_movement = encode_movement(square, target_index, state[target_index]);
                                move_buffer[move_count] = encoded_movement;
                                move_count++;
                            }
                        }
                    }
                }

            }
        }
        else if (piece_type == 4)
        {
            for (int i = 0; i < laterals_x.Length; i++)
            {
                for (int j = 0; j < forwards_x.Length; j++)
                {
                    sx = px + 1 * forwards_x[i] + 2 * laterals_x[i];
                    sy = py + 1 * forwards_y[i] + 2 * laterals_y[i];
                    sz = pz + 1 * forwards_z[i] + 2 * laterals_z[i];
                    sw = pw + 1 * forwards_w[i] + 2 * laterals_w[i];

                    if (!isOutOfBounds(sx, sy, sz, sw))
                    {
                        target_index = CoordToIndex(sx, sy, sz, sw);
                        int result = state[target_index];
                        if (result == 0 || isBlackPiece(result) != isBlackPiece(piece))
                        {
                            target_index = CoordToIndex(sx, sy, sz, sw);
                            // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                            int encoded_movement = encode_movement(square, target_index, state[target_index]);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                    }

                    sx = px + 2 * forwards_x[i] + 1 * laterals_x[i];
                    sy = py + 2 * forwards_y[i] + 1 * laterals_y[i];
                    sz = pz + 2 * forwards_z[i] + 1 * laterals_z[i];
                    sw = pw + 2 * forwards_w[i] + 1 * laterals_w[i];

                    if (!isOutOfBounds(sx, sy, sz, sw))
                    {
                        target_index = CoordToIndex(sx, sy, sz, sw);
                        int result = state[target_index];
                        if (result == 0 || isBlackPiece(result) != isBlackPiece(piece))
                        {
                            target_index = CoordToIndex(sx, sy, sz, sw);
                            // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                            int encoded_movement = encode_movement(square, target_index, state[target_index]);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                    }

                }
            }
        }
        else if (piece_type == 5)
        {
            
            for (int i = 0; i < laterals_x.Length; i++)
            {
                int lateral = i;
                int forward = NULL;
                bool occluded = false;
                ClearCast();
                CastForPiece(sx, sy, sz, sw, lateral, forward, cast_results, state);
                for (int j = 0; j < cast_results.Length; j++)
                {
                    int result = cast_results[j];
                    // Our results are NULL when casting out of bounds
                    if (result != NULL && !occluded)
                    {
                        // so therefore if it isnt NULL, we are within bounds for the cast;
                        if (result == 0)
                        {
                            target_index = CoordToIndex(sx + laterals_x[i]*(j+1),
                                                        sy + laterals_y[i]*(j+1),
                                                        sz + laterals_z[i]*(j+1),
                                                        sw + laterals_w[i]*(j+1));

                            // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                            int encoded_movement = encode_movement(square, target_index, state[target_index]);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                        else if (isBlackPiece(result) == isBlackPiece(piece))
                        {
                            occluded = true;
                        }
                        else if (isBlackPiece(result) != isBlackPiece(piece))
                        {
                            occluded = true;
                            target_index = CoordToIndex(sx + laterals_x[i] * (j + 1),
                                                        sy + laterals_y[i] * (j + 1),
                                                        sz + laterals_z[i] * (j + 1),
                                                        sw + laterals_w[i] * (j + 1));

                            int encoded_movement = encode_movement(square, target_index, state[target_index]);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                    }
                }

                lateral = NULL;
                forward = i;
                occluded = false;
                ClearCast();
                CastForPiece(sx, sy, sz, sw, lateral, forward, cast_results, state);
                for (int j = 0; j < cast_results.Length; j++)
                {
                    int result = cast_results[j];
                    // Our results are NULL when casting out of bounds
                    if (result != NULL && !occluded)
                    {
                        // so therefore if it isnt NULL, we are within bounds for the cast;
                        if (result == 0)
                        {
                            target_index = CoordToIndex(sx + forwards_x[i] * (j + 1),
                                                        sy + forwards_y[i] * (j + 1),
                                                        sz + forwards_z[i] * (j + 1),
                                                        sw + forwards_w[i] * (j + 1));

                            // Note, all encoded movments MUST be valid, otherwise the tree walk for evaluating moves may break
                            int encoded_movement = encode_movement(square, target_index, state[target_index]);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                        else if (isBlackPiece(result) == isBlackPiece(piece))
                        {
                            occluded = true;
                        }
                        else if (isBlackPiece(result) != isBlackPiece(piece))
                        {
                            occluded = true;
                            target_index = CoordToIndex(sx + forwards_x[i] * (j + 1),
                                                        sy + forwards_y[i] * (j + 1),
                                                        sz + forwards_z[i] * (j + 1),
                                                        sw + forwards_w[i] * (j + 1));

                            int encoded_movement = encode_movement(square, target_index, state[target_index]);
                            move_buffer[move_count] = encoded_movement;
                            move_count++;
                        }
                    }
                }
            }
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


    //// Unused code for binary tree I really like. Spent a long time making it, then realized "what do I need a binary tree for?"
    //// Still a nice little method, so I'm keeping it in here until I can extract it to a new project
    
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

}
