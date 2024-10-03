using Chess.Bitboards;
using Chess.Classes;
using Chess.Utils;

namespace Chess.MoveGen {
    public class MoveGenerator(Board board) {
        readonly Board board = board;

        public List<Move> GenerateKnightMoves(int start) {
            ulong knightMask = Masks.knightMasks[start];
            ulong blockerBitboard = board.GetBitboard(board.pieces[start].IsWhitePiece());
            knightMask &= ~blockerBitboard;

            knightMask = FilterPieceMoves(knightMask, start);
            return knightMask.BitboardToMoveArray(board, start);
        }

        public List<Move> GenerateKingMoves(int start) {
            ulong kingMask = Masks.kingMasks[start];
            ulong blockerBitboard = board.GetBitboard(board.pieces[start].IsWhitePiece()) | board.attackedSquares;
            kingMask &= ~blockerBitboard;

            List<Move> moves = kingMask.BitboardToMoveArray(board, start);
            if(board.inCheck)
                return moves;

            bool white = board.pieces[start].IsWhitePiece();
            blockerBitboard |= board.GetBitboard(!board.pieces[start].IsWhitePiece());

            int offset = white? 0 : 56;
            if(board.GetCastlingRights(true, white)) {
                ulong kingsideCastle = Masks.kingsideCastleMask << offset;
                ulong kingsideRook = Masks.kingsideRookMask << offset;

                if(((blockerBitboard & kingsideCastle) == 0) && (kingsideRook & board.GetBitboard(white, 'r')) > 0) {
                    moves.Add(new(board, start, start - 2, Move.MoveType.ShortCastle));
                }
            }
            if(board.GetCastlingRights(false, white)) {
                ulong queensideCastle = Masks.queensideCastleMask << offset;
                ulong queensideRook = Masks.queensideRookMask << offset;
                ulong queensideCheck = Masks.queensideCheckMask << offset;

                blockerBitboard = board.WhiteBitboard | board.BlackBitboard | (board.attackedSquares & queensideCheck);

                if(((blockerBitboard & queensideCastle) == 0) && (queensideRook & board.GetBitboard(white, 'r')) > 0) {
                    moves.Add(new(board, start, start + 2, Move.MoveType.LongCastle));
                }
            }

            return moves;
        }

        public ulong GenerateSlidingMap(int start, char pieceType = 'r') {
            ulong mask = pieceType == 'r'? Masks.rookMasks[start]: Masks.bishopMasks[start];
            ulong blockers = board.WhiteBitboard | board.BlackBitboard;
            blockers &= mask;
            ulong movesBitboard = (pieceType == 'r'? Masks.rookTable: Masks.bishopTable)[(start, blockers)];
            return movesBitboard;
        }

        public List<Move> GenerateSlidingMoves(int start, char pieceType = 'r') {
            ulong output = GenerateSlidingMap(start, pieceType);
            if(pieceType == 'q')
                output |= GenerateSlidingMap(start, 'r');
            ulong bitboard = board.GetBitboard(board.pieces[start].IsWhitePiece());
            output &= ~bitboard;
            output = FilterPieceMoves(output, start);
            return output.BitboardToMoveArray(board, start);
        }

        public List<Move> GeneratePawnMoves(int start, out bool doFilter) {
            int y = start.ToSquare().Item2;
            bool white = board.pieces[start].IsWhitePiece();
            ulong output = (white? Masks.whitePawnMasks: Masks.blackPawnMasks)[start];
            ulong blockers = board.WhiteBitboard | board.BlackBitboard;
            ulong blockerMask = white? (ulong.MaxValue << (8 * (8 - y))): (ulong.MaxValue >> (8 * (y + 1)));
            blockers &= blockerMask;

            blockers |= white? (blockers << 8): (blockers >> 8);

            output &= ~blockers;

            ulong captures = (white? Masks.whitePawnCaptures: Masks.blackPawnCaptures)[start];
            ulong enemyBitboard = board.GetBitboard(!white) | (1ul << board.epSquare.Item1);
            captures &= enemyBitboard;
            output |= captures;
            
            doFilter = (captures & (1ul << board.epSquare.Item1)) > 0;

            output = FilterPieceMoves(output, start);
            List<Move> moves = output.BitboardToPawnMoveArray(board, start);

            return moves;
        }

        public List<Move> GenerateMoves(bool white) {
            List<Move> output = [];
            CalculatePins(white);
            ReloadAttacked(!white);
            bool doFilter = false;
            foreach(KeyValuePair<int, int> piece in board.pieces) {
                if(!piece.Value.IsEmpty() && piece.Value.IsWhitePiece() == white)  {
                    switch(piece.Value.GetPieceValue().GetPieceChar()) {
                        case 'p':
                            output.AddRange(GeneratePawnMoves(piece.Key, out bool temp)); doFilter |= temp; break;
                        case 'n':
                            output.AddRange(GenerateKnightMoves(piece.Key)); break;
                        case 'b':
                            output.AddRange(GenerateSlidingMoves(piece.Key, 'b')); break;
                        case 'r':
                            output.AddRange(GenerateSlidingMoves(piece.Key)); break;
                        case 'q':
                            output.AddRange(GenerateSlidingMoves(piece.Key, 'q')); break;
                        case 'k':
                            output.AddRange(GenerateKingMoves(piece.Key)); break;
                    }
                }
            }
            return (board.inCheck || doFilter)? FilterLegalMoves(output): output;
        }

        public ulong FilterPieceMoves(ulong bitboard, int start) {
            if(board.IsPinned(start)) {
                ulong? temp = board.FindPinIndex(start);
                if(temp == null)
                    return bitboard;

                ulong pin = (ulong) temp;
                return bitboard & pin;
            }
            return bitboard;
        }

        public void ReloadAttacked(bool white) {
            board.attackedSquares = GetAttackMap(white);
            board.inCheck = false;
            
            if((board.GetBitboard(!white, 'k') & board.attackedSquares) > 0) {
                board.inCheck = true;
            }
        }

        public ulong GetAttackMap(bool white) {
            ulong map = 0;
            foreach(KeyValuePair<int, int> piece in board.pieces) {
                if(piece.Value.IsEmpty() || piece.Value.IsWhitePiece() != white)
                    continue;

                switch(piece.Value.GetPieceValue().GetPieceChar()) {
                    case 'p':
                        map |= (white? Masks.whitePawnCaptures: Masks.blackPawnCaptures)[piece.Key]; break;
                    case 'n':
                        map |= Masks.knightMasks[piece.Key]; break;
                    case 'b':
                        map |= GenerateSlidingMap(piece.Key, 'b'); break;
                    case 'r':
                        map |= GenerateSlidingMap(piece.Key, 'r'); break;
                    case 'q':
                        map |= GenerateSlidingMap(piece.Key, 'r') | GenerateSlidingMap(piece.Key, 'b'); break;
                    case 'k':
                        map |= Masks.kingMasks[piece.Key]; break;
                }
            }
            return map;
        }

        public void CalculatePins(bool white) {
            int kingSquare = board.kings[white? 0: 1];
            var enemyBitboards = board.GetBitboards(!white);

            board.pins = 0;
            board.pinRays = new ulong[8];
            
            int start = 0, end = 8;
            if(enemyBitboards['q'.GetPieceValue()] > 0) {
                end = 8;
            }
            else {
                if(enemyBitboards['r'.GetPieceValue()] == 0) {
                    start = 4;
                }
                if(enemyBitboards['b'.GetPieceValue()] == 0) {
                    end = 4;
                }
            }

            for(int i = start; i < end; i++) {
                int offset = Masks.offsets[i];
                bool diagonal = i >= 4;
                
                ulong kingRayMask = diagonal? Masks.bishopMasks[kingSquare]: Masks.rookMasks[kingSquare];
                ulong sliderBitboards = diagonal? board.GetDiagonalBitboards(!white): board.GetOrthogonalBitboards(!white);

                if((kingRayMask & sliderBitboards) == 0)
                    continue;
                
                int distance = Masks.distances[kingSquare, i];
                bool hasFriendlyPiece = false;
                ulong rayMask = 0;

                for(int t = 1; t < distance; t++) {
                    int targetIndex = kingSquare + t * offset;
                    rayMask |= 1ul << targetIndex;

                    int piece = board.pieces.GetValueOrDefault(targetIndex);
                    if(!piece.IsEmpty()) {
                        if(piece.IsWhitePiece() == white) {
                            if(!hasFriendlyPiece)
                                hasFriendlyPiece = true;
                            else
                                break;
                        }
                        else {
                            if((piece.IsDiagonalPiece() && diagonal) || (piece.IsOrthogonalPiece() && !diagonal)) {
                                if(hasFriendlyPiece) {
                                    board.pins |= rayMask;
                                    board.pinRays[i] |= rayMask;
                                }
                                else
                                    break;
                            }
                            else
                                break;
                        }
                    }
                }
            }
        }

        public List<Move> FilterLegalMoves(List<Move> moves) {
            List<Move> output = [];
            if(moves.Count == 0)
                return moves;
            
            bool white = moves[0].WhiteMove;
            foreach(Move move in moves) {
                if(move.ToString() == "a2a4")
                    Console.Write("");
                move.MakeMove();
                ulong kingBitboard = board.GetBitboard(white, 'k');
                ulong attacked = GetAttackMap(!move.WhiteMove);
                if((attacked & kingBitboard) == 0) {
                    output.Add(move);
                }
                move.UndoMove();
            }
            return output;
        }
    }
}