using Chess.Classes;
using Chess.MoveGen;
using Chess.Utils;

namespace Chess.ChessEngine {
    public class Engine(Board board) {
        readonly Board board = board;
        readonly MoveGenerator generator = new(board);
        public const int pawnValue = 100;
        public const int knightValue = 300;
        public const int bishopValue = 300;
        public const int rookValue = 500;
        public const int queenValue = 900;

        public static int swaps = 0;

        public int GetPositionQuality() {
            int whiteMaterial = CountMaterial(true);
            int blackMaterial = CountMaterial(false);

            int eval = whiteMaterial - blackMaterial;

            int multiplier = board.white? 1: -1;
            return eval * multiplier;
        }

        public int CountMaterial(bool white) {
            int material = 0;
            material += board.pieceCounts[Pieces.GetPiece('p', white)] * pawnValue;
            material += board.pieceCounts[Pieces.GetPiece('n', white)] * knightValue;
            material += board.pieceCounts[Pieces.GetPiece('b', white)] * bishopValue;
            material += board.pieceCounts[Pieces.GetPiece('r', white)] * rookValue;
            material += board.pieceCounts[Pieces.GetPiece('q', white)] * queenValue;
            return material;
        }

        public static int GetPiecePoints(int piece) {
            return piece.GetPieceValue().GetPieceChar() switch
            {
                'p' => pawnValue,
                'n' => knightValue,
                'b' => bishopValue,
                'r' => rookValue,
                'q' => queenValue,
                _ => 0,
            };
        }

        public int Search(int depth, int alpha, int beta, ref Move output) {
            if(depth == 0)
                return GetPositionQuality();

            List<Move> moves = generator.GenerateMoves(board.white);
            if(moves.Count == 0) {
                if(board.inCheck)
                    return int.MinValue;
                return 0;
            }
            List<int> sorted = SortMoves(moves);

            Move discard = null;

            foreach(int i in sorted) {
                Move move = moves[i];
                move.MakeMove();
                int eval = -Search(depth - 1, -beta, -alpha, ref discard);
                move.UndoMove();
                if(eval >= beta)
                    return beta;
                if(alpha < eval) {
                    alpha = eval;
                    output = move;
                }
            }
            return alpha;
        }

        public static int GetMoveScore(Move move) {
            int moveScore = 0;

            if(move.pieceTaken != 0) {
                moveScore = 10 * GetPiecePoints(move.pieceTaken) - GetPiecePoints(move.pieceMoved);
            }

            if(move.IsPromotion)
                moveScore += GetPiecePoints(move.PromotesTo);

            return moveScore;
        }

        public static List<int> SortMoves(List<Move> moves) {
            List<int> moveScores = new(moves.Count);
            List<int> indices = new(moves.Count);
            
            int startInsertIndex = 0;
            int zeroCount = 0;
            int i = 0;
            foreach(Move move in moves) {
                int score = GetMoveScore(move);
                if(score > 0) {
                    indices.Insert(0, i);
                    startInsertIndex++;
                }
                else if(score < 0) {
                    indices.Add(i);
                }
                else {
                    indices.Insert(startInsertIndex, i);
                    zeroCount++;
                }
                i++;
                moveScores.Add(score);
            }
            DualPivotQuicksortYaroslavskiy(indices, 0, startInsertIndex - 1, i => -moveScores[i]);
            DualPivotQuicksortYaroslavskiy(indices, startInsertIndex + zeroCount, moves.Count - 1, i => -moveScores[i]);
            return indices;
        }

        public static void DualPivotQuicksortYaroslavskiy<T>(List<T> data, int left, int right, Func<T, int> key) {
            if(left >= right)
                return;
            int p = key(data[left]), q = key(data[right]);
            if(p > q) {
                (data[left], data[right]) = (data[right], data[left]);
                (p, q) = (q, p);
            }

            int l = left + 1, k = l, g = right - 1;
            while(k <= g) {
                if(key(data[k]) < p) {
                    (data[k], data[l]) = (data[l], data[k]);
                    l++;
                }
                else {
                    if(key(data[k]) > q) {
                        while(key(data[g]) > q && k < g)
                            g--;
                        (data[k], data[g]) = (data[g], data[k]);
                        g--;
                        if(key(data[k]) < p) {
                            (data[k], data[l]) = (data[l], data[k]);
                            l++;
                        }
                    }
                }
                k++;
            }
            l--; g++;
            (data[left], data[l]) = (data[l], data[left]);
            (data[right], data[g]) = (data[g], data[right]);
            DualPivotQuicksortYaroslavskiy(data, left, l-1, key);
            DualPivotQuicksortYaroslavskiy(data, l+1, g-1, key);
            DualPivotQuicksortYaroslavskiy(data, g+1, right, key);
        }
    }
}