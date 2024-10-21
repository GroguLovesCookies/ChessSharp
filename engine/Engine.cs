using Chess.Classes;
using Chess.MoveGen;
using Chess.Utils;
using Chess.Zobrist;

namespace Chess.ChessEngine {
    public class Engine(Board board) {
        readonly Board board = board;
        readonly MoveGenerator generator = new(board);
        readonly TranspositionTable tt = new(board, 64);
        public const int pawnValue = 100;
        public const int knightValue = 320;
        public const int bishopValue = 330;
        public const int rookValue = 500;
        public const int queenValue = 900;

        public const int winningCapture = 8000000;
        public const int losingCapture = 2000000;
        public const int promoteBias = 6000000;

        public static readonly int[] piecePoints = [0, 100, 320, 330, 500, 900, 0];

        public Move bestMove;
        public Move oldBestMove;
        public List<Move> killerMoves = [];

        public static int pruned = 0;

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
            return piecePoints[piece.GetPieceValue()];
        }

        public int Search(int depth, int alpha, int beta, int plyFromRoot = 0) {
            pruned++;
            int ttVal = tt.LookupEvaluation(depth, plyFromRoot, alpha, beta, out TranspositionTable.Entry entry);
            if(ttVal >= 0) {
                if(plyFromRoot == 0)
                    bestMove = entry.move;
                return ttVal;
            }

            Move? localBestMove = null;


            if(depth == 0)
                return GetPositionQuality();

            List<Move> moves = generator.GenerateMoves(board.white);
            if(moves.Count == 0) {
                if(board.inCheck)
                    return int.MinValue;
                return 0;
            }
            List<int> sorted = SortMoves(moves);

            int evalType = 1;

            foreach(int i in sorted) {
                Move move = moves[i];
                move.MakeMove();
                int eval = -Search(depth - 1, -beta, -alpha, plyFromRoot+1);
                move.UndoMove();
                if(eval >= beta) {
                    tt.StoreEval(depth, plyFromRoot, beta, 2, move);
                    return beta;
                }
                if(alpha < eval) {
                    evalType = 0;
                    alpha = eval;
                    localBestMove = move;
                }
            }
            if(plyFromRoot == 0 && localBestMove != null)
                bestMove = localBestMove;

            if(localBestMove != null)
                tt.StoreEval(depth, plyFromRoot, alpha, evalType, localBestMove);
            return alpha;
        }

        public int GetMoveScore(Move move) {
            int moveScore = 0;

            if(move.pieceTaken != 0) {
                int delta = GetPiecePoints(move.pieceTaken) - GetPiecePoints(move.pieceMoved);
                bool recapture = (move.board.attackedSquares & (1ul << move.end)) > 0;
                moveScore += (recapture && delta >= 0)? winningCapture: losingCapture + delta;
            }

            if(move.IsPromotion)
                moveScore += promoteBias + GetPiecePoints(move.PromotesTo);

            if(((1ul << move.end) & board.attackedDict['p'.GetPieceValue()]) > 0) {
                moveScore -= 700 * GetPiecePoints(move.pieceMoved);
            }

            return moveScore;
        }

        public List<int> SortMoves(List<Move> moves) {
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