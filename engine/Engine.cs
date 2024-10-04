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
            switch(piece.GetPieceValue().GetPieceChar()) {
                case 'p':
                    return pawnValue;
                case 'n':
                    return knightValue;
                case 'b':
                    return bishopValue;
                case 'r':
                    return rookValue;
                case 'q':
                    return queenValue;
                default:
                    return 0;
            }
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
            (Move, int)[] sorted = SortMoves(moves);

            Move discard = null;

            foreach((Move move, int _) in sorted) {
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

        public static (Move, int)[] SortMoves(List<Move> moves) {
            (Move, int)[] scores = new (Move, int)[moves.Count];
            for(int i = 0; i < moves.Count; i++) {
                scores[i] = (moves[i], GetMoveScore(moves[i]));
            }
            QuickSort(scores, 0, moves.Count-1, i => -i.Item2);
            return scores;
        }

        public static void QuickSort<T>(T[] data, int left, int right, Func<T, int> key) {
            var i = left;
            var j = right;
            var pivot = key(data[left]);
            while (i <= j)
            {
                while (key(data[i]) < pivot)
                {
                    i++;
                }
                
                while (key(data[j]) > pivot)
                {
                    j--;
                }
                if (i <= j)
                {
                    (data[i], data[j]) = (data[j], data[i]);
                    i++;
                    j--;
                }
            }
            
            if (left < j)
                QuickSort(data, left, j, key);
            if (i < right)
                QuickSort(data, i, right, key);
            return;
        }
    }
}