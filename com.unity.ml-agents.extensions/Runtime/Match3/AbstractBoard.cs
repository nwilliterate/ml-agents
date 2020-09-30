using UnityEngine;

namespace Unity.MLAgents.Extensions.Match3
{
    public abstract class AbstractBoard : MonoBehaviour
    {
        public int Rows;
        public int Columns;
        public int NumCellTypes;


        /// <summary>
        /// Returns the "type" of piece at the given row and column.
        /// This should be between 0 and NumCellTypes-1 (inclusive).
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public abstract int GetCellType(int row, int col);

        /// <summary>
        /// Check whether the particular Move is valid for the game.
        /// The actual results will depend on the rules of the game, but we provide SimpleIsMoveValid()
        /// that handles basic match3 rules with no special or immovable pieces.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public abstract bool IsMoveValid(Move m);

        /// <summary>
        /// Instruct the game to make the given move. Returns true if the move was made.
        /// Note that during training, a move that was marked as invalid may occasionally still be
        /// requested. If this happens, it is safe to do nothing and request another move.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public abstract bool MakeMove(Move m);

        // TODO handle "special" cell types?

        /// <summary>
        /// Returns true if swapped the cells specified by the move would result in
        /// 3 or more cells of the same type in a row. This assumes that all pieces are allowed
        /// to be moved; to add extra logic, incorporate it into you IsMoveValid() method.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        public bool SimpleIsMoveValid(Move move)
        {
            using (TimerStack.Instance.Scoped("SimpleIsMoveValid"))
            {
                var moveVal = GetCellType(move.Row, move.Column);
                var (otherRow, otherCol) = move.OtherCell();
                var oppositeVal = GetCellType(otherRow, otherCol);

                // Simple check - if the values are the same, don't match
                // This might not be valid for all games
                {
                    if (moveVal == oppositeVal)
                    {
                        return false;
                    }
                }

                bool moveMatches = CheckHalfMove(otherRow, otherCol, moveVal, move.Direction);
                if (moveMatches)
                {
                    // early out
                    return true;
                }

                bool otherMatches = CheckHalfMove(move.Row, move.Column, oppositeVal, move.OtherDirection());
                return otherMatches;
            }
        }

        /// <summary>
        /// Check if the "half" of a move is matches 3 or more.
        /// </summary>
        /// <param name="newRow"></param>
        /// <param name="newCol"></param>
        /// <param name="newValue"></param>
        /// <param name="incomingDirection"></param>
        /// <returns></returns>
        bool CheckHalfMove(int newRow, int newCol, int newValue, Direction incomingDirection)
        {
            int matchedLeft = 0, matchedRight = 0, matchedUp = 0, matchedDown = 0;

            if (incomingDirection != Direction.Right)
            {
                for (var c = newCol - 1; c >= 0; c--)
                {

                    if (GetCellType(newRow, c) == newValue)
                        matchedLeft++;
                    else
                        break;
                }
            }

            if (incomingDirection != Direction.Left)
            {
                for (var c = newCol + 1; c < Columns; c++)
                {
                    if (GetCellType(newRow, c) == newValue)
                        matchedRight++;
                    else
                        break;
                }
            }

            if (incomingDirection != Direction.Down)
            {
                for (var r = newRow + 1; r < Rows; r++)
                {
                    if (GetCellType(r, newCol) == newValue)
                        matchedUp++;
                    else
                        break;
                }
            }

            if (incomingDirection != Direction.Up)
            {
                for (var r = newRow - 1; r >= 0; r--)
                {
                    if (GetCellType(r, newCol) == newValue)
                        matchedDown++;
                    else
                        break;
                }
            }

            if ((matchedUp + matchedDown >= 2) || (matchedLeft + matchedRight >= 2))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a random valid move index, or -1 if none are available.
        /// </summary>
        /// <param name="rand"></param>
        /// <returns></returns>
        public int GetRandomValidMoveIndex(System.Random rand)
        {
            using (TimerStack.Instance.Scoped("GetRandomValidMove"))
            {
                int numMoves = Move.NumEdgeIndices(Rows, Columns);
                var validMoves = new bool[numMoves];

                int numValidMoves = 0;
                for (var index = 0; index < Move.NumEdgeIndices(Rows, Columns); index++)
                {
                    var move = Move.FromEdgeIndex(index, Rows, Columns);
                    if (IsMoveValid(move))
                    {
                        validMoves[index] = true;
                        numValidMoves++;
                    }
                }

                // TODO reservoir sample? More random calls, but one pass through the indices.
                if (numValidMoves == 0)
                {
                    Debug.Log("No valid moves");
                    return -1;
                }

                // We'll make the n'th valid move where n in [0, numValidMoves)
                var target = rand.Next(numValidMoves);
                var numSkipped = 0;

                for (var i = 0; i < validMoves.Length; i++)
                {
                    var valid = validMoves[i];
                    if (valid)
                    {
                        if (numSkipped == target)
                        {
                            return i;
                        }

                        numSkipped++;
                    }
                }

                // Should never reach here
                return -1;
            }
        }
    }
}