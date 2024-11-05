using Chess.Classes;
using Chess.MoveGen;
using Chess.Utils;
using Chess.Zobrist;

namespace Chess.ChessEngine {
    public class Engine(Board board) {
        readonly Board board = board;
        readonly MoveGenerator generator = new(board);
        readonly TranspositionTable tt = new(board, 64);
        readonly RepetitionTable repetitionTable = new();
        public const int pawnValue = 100;
        public const int knightValue = 320;
        public const int bishopValue = 330;
        public const int rookValue = 500;
        public const int queenValue = 900;

        public const int winningCapture = 8000000;
        public const int losingCapture = 2000000;
        public const int promoteBias = 6000000;

        public static readonly int[] piecePoints = [0, 100, 320, 330, 500, 900, 0];

        public static readonly int[] pawnPieceSquare = [0,  0,  0,  0,  0,  0,  0,  0,
                                                        50, 50, 50, 50, 50, 50, 50, 50,
                                                        10, 10, 20, 30, 30, 20, 10, 10,
                                                        5,  5, 10, 25, 25, 10,  5,  5,
                                                        0,  0,  0, 20, 20,  0,  0,  0,
                                                        5, -5,-10,  0,  0,-10, -5,  5,
                                                        5, 10, 10,-20,-20, 10, 10,  5,
                                                        0,  0,  0,  0,  0,  0,  0,  0 ];
        
        public static readonly int[] knightPieceSquare = [  -50,-40,-30,-30,-30,-30,-40,-50,
                                                            -40,-20,  0,  0,  0,  0,-20,-40,
                                                            -30,  0, 10, 15, 15, 10,  0,-30,
                                                            -30,  5, 15, 20, 20, 15,  5,-30,
                                                            -30,  0, 15, 20, 20, 15,  0,-30,
                                                            -30,  5, 10, 15, 15, 10,  5,-30,
                                                            -40,-20,  0,  5,  5,  0,-20,-40,
                                                            -50,-40,-30,-30,-30,-30,-40,-50  ];
        
        public static readonly int[] bishopPieceSquare = [  -20,-10,-10,-10,-10,-10,-10,-20,
                                                            -10,  0,  0,  0,  0,  0,  0,-10,
                                                            -10,  0,  5, 10, 10,  5,  0,-10,
                                                            -10,  5,  5, 10, 10,  5,  5,-10,
                                                            -10,  0, 10, 10, 10, 10,  0,-10,
                                                            -10, 10, 10, 5, 5, 10, 10,-10,
                                                            -10,  5,  0,  0,  0,  0,  5,-10,
                                                            -20,-10,-10,-10,-10,-10,-10,-20   ];
        
        public static readonly int[] rookPieceSquare = [0,  0,  0,  0,  0,  0,  0,  0,
                                                        5, 10, 10, 10, 10, 10, 10,  5,
                                                        -5,  0,  0,  0,  0,  0,  0, -5,
                                                        -5,  0,  0,  0,  0,  0,  0, -5,
                                                        -5,  0,  0,  0,  0,  0,  0, -5,
                                                        -5,  0,  0,  0,  0,  0,  0, -5,
                                                        -5,  0,  0,  0,  0,  0,  0, -5,
                                                        0,  0,  0,  5,  5,  0,  0,  0 ];
        
        public static readonly int[] queenPieceSquare = [ -20,-10,-10, -5, -5,-10,-10,-20,
                                                        -10,  0,  0,  0,  0,  0,  0,-10,
                                                        -10,  0,  5,  5,  5,  5,  0,-10,
                                                        -5,  0,  5,  5,  5,  5,  0, -5,
                                                        0,  0,  5,  5,  5,  5,  0, -5,
                                                        -10,  5,  5,  5,  5,  5,  0,-10,
                                                        -10,  0,  5,  0,  0,  0,  0,-10,
                                                        -20,-10,-10, -5, -5,-10,-10,-20];

        public static readonly int[] kingPieceSquare = [-30,-40,-40,-50,-50,-40,-40,-30,
                                                        -30,-40,-40,-50,-50,-40,-40,-30,
                                                        -30,-40,-40,-50,-50,-40,-40,-30,
                                                        -30,-40,-40,-50,-50,-40,-40,-30,
                                                        -20,-30,-30,-40,-40,-30,-30,-20,
                                                        -10,-20,-20,-20,-20,-20,-20,-10,
                                                        20, 20,  0,  0,  0,  0, 20, 20,
                                                        20, 30, 10,  0,  0, 10, 30, 20];
        
        public static readonly int[] kingEndPieceSquare = [ -50,-40,-30,-20,-20,-30,-40,-50,
                                                            -30,-20,-10,  0,  0,-10,-20,-30,
                                                            -30,-10, 20, 30, 30, 20,-10,-30,
                                                            -30,-10, 30, 40, 40, 30,-10,-30,
                                                            -30,-10, 30, 40, 40, 30,-10,-30,
                                                            -30,-10, 20, 30, 30, 20,-10,-30,
                                                            -30,-30,  0,  0,  0,  0,-30,-30,
                                                            -50,-30,-30,-30,-30,-30,-30,-50];
        
        public static readonly int[][] pieceSquareTables = [[], pawnPieceSquare, knightPieceSquare, bishopPieceSquare, rookPieceSquare, queenPieceSquare, kingPieceSquare];

        public Move bestMove;
        public Move oldBestMove;
        public List<Move> killerMoves = [];

        public static int pruned = 0;

        public int GetPositionQuality(int plyFromRoot) {
            int whiteMaterial = CountMaterial(true);
            int blackMaterial = CountMaterial(false);

            bool useMopUp = board.pieceCounts[Pieces.GetPiece('p', !board.white)] == 0;
            int eval = whiteMaterial - blackMaterial;
            if(useMopUp) {
                int opponentKing = board.kings[board.white? 1: 0];
                int friendlyKing = board.kings[board.white? 0: 1];
                int dist = Math.Abs(opponentKing/8 - friendlyKing/8) + Math.Abs((opponentKing % 8) - (friendlyKing % 8));
                eval += dist * 10/(plyFromRoot + 1);

                int centerDist = Math.Max(opponentKing/8 - 4, 3 - opponentKing/8) + Math.Max(opponentKing%8 - 4, 3 - opponentKing%8);
                eval += centerDist * 10/(plyFromRoot + 1);
            }

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
            return material + (white? board.whiteQuality: board.blackQuality);
        }

        public static int GetPiecePoints(int piece, int square) {
            return piecePoints[piece.GetPieceValue()] + GetSquarePoints(piece, square);
        }

        public static int GetSquarePoints(int piece, int square) {
            (int, int) coords = square.ToSquare();
            coords = piece.IsWhitePiece()? coords: (coords.Item1, 7-coords.Item2);
            square = 63 - coords.ToIndex();
            return pieceSquareTables[piece.GetPieceValue()][square];
        }

        public int Search(int depth, int alpha, int beta, int plyFromRoot = 0) {
            if(plyFromRoot > 0) {
                if(repetitionTable.Contains(board.zobrist)) {
                    return 0;
                }

                alpha = Math.Max(alpha, -100000 + plyFromRoot);
                beta = Math.Min(beta, 100000 - plyFromRoot);
                if(alpha >= beta)
                    return alpha;
            }

            int ttVal = tt.LookupEvaluation(depth, plyFromRoot, alpha, beta, out TranspositionTable.Entry entry);

            if(ttVal >= 0) {
                if(plyFromRoot == 0)
                    bestMove = entry.move;
                return ttVal;
            }

            Move? localBestMove = null;


            if(depth == 0)
                return GetPositionQuality(plyFromRoot);

            List<Move> moves = generator.GenerateMoves(board.white);
            if(moves.Count == 0) {
                if(board.inCheck)
                    return -100000000/(plyFromRoot + 1);
                return 0;
            }
            if(plyFromRoot > 0) {
                bool wasPawnMove = board.pieces.GetValueOrDefault(board.GetLastMoved(!board.white)).GetPieceValue().GetPieceChar() == 'p';
                repetitionTable.Push(board.zobrist, wasPawnMove);
            }
            List<int> sorted = SortMoves(moves);

            int evalType = 1;

            foreach(int i in sorted) {
                Move move = moves[i];
                move.MakeMove();
                int eval = -Search(depth - 1, -beta, -alpha, plyFromRoot+1);
                if(plyFromRoot == 0) {
                    Console.WriteLine($"{move} {eval}");
                }
                move.UndoMove();
                if(eval >= beta) {
                    tt.StoreEval(depth, plyFromRoot, beta, 2, move);
                    if(plyFromRoot > 0)
                        repetitionTable.TryPop();
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

            if(plyFromRoot > 0)
                repetitionTable.TryPop();

            if(localBestMove != null)
                tt.StoreEval(depth, plyFromRoot, alpha, evalType, localBestMove);
            return alpha;
        }

        public int GetMoveScore(Move move) {
            int moveScore = GetSquarePoints(move.pieceMoved, move.end);

            move.MakeMove();
            if(repetitionTable.Contains(board.zobrist)) {
                moveScore -= 200;
            }
            move.UndoMove();

            if(move.pieceMoved.GetPieceValue().GetPieceChar() == 'k' && move.type == Move.MoveType.None)
                moveScore -= 200;

            if(move.pieceTaken != 0) {
                int delta = GetPiecePoints(move.pieceTaken, move.end) - GetPiecePoints(move.pieceMoved, move.start);
                bool recapture = (move.board.attackedSquares & (1ul << move.end)) > 0;
                moveScore += (recapture && delta >= 0)? winningCapture: losingCapture + delta;
            }

            if(move.IsPromotion)
                moveScore += promoteBias + GetPiecePoints(move.PromotesTo, move.end);

            if(((1ul << move.end) & board.attackedDict['p'.GetPieceValue()]) > 0) {
                moveScore -= 700 * GetPiecePoints(move.pieceMoved, move.start);
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