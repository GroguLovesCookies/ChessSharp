using System.Collections.Generic;
using Chess.ChessEngine;
using Chess.MoveGen;
using Chess.Utils;

namespace Chess.Classes {
    public class Move(Board board, int start, int end, Move.MoveType type = Move.MoveType.None) {
        public enum MoveType {
            None,
            ShortCastle,
            LongCastle,
            PromoteQueen,
            PromoteRook,
            PromoteBishop,
            PromoteKnight,
            DoublePush,
            EnPassant
        }
        Board board = board;
        public readonly int start = start, end = end;
        public int pieceMoved = board.pieces.GetValueOrDefault(start);
        public int pieceTaken = board.pieces.GetValueOrDefault(end);
        MoveType type = type;
        public bool WhiteMove => pieceMoved.GetPieceColor() == 0b01000;
        (int, int) StartSquare => start.ToSquare();
        (int, int) EndSquare => end.ToSquare();

        public bool IsPromotion => type == MoveType.PromoteQueen || type == MoveType.PromoteRook || type == MoveType.PromoteBishop || type == MoveType.PromoteKnight;

        public int PromotesTo => type switch
            {
                MoveType.PromoteQueen => 'q'.GetPieceValue(),
                MoveType.PromoteRook => 'r'.GetPieceValue(),
                MoveType.PromoteBishop => 'b'.GetPieceValue(),
                MoveType.PromoteKnight => 'r'.GetPieceValue(),
                _ => ' ',
            };

        public Move(Board board, string moveStr) : this(board, 0, 0) {
            if(moveStr == "O-O") {
                start = board.kings[board.white? 0: 1];
                end = board.kings[board.white? 0: 1] - 2;
                type = MoveType.ShortCastle;
            }
            else if(moveStr == "O-O-O") {
                start = board.kings[board.white? 0: 1];
                end = board.kings[board.white? 0: 1] + 2;
                type = MoveType.LongCastle;
            }
            else {
                int startX = moveStr[0] - 'a';
                int startY = '1' + 7 - moveStr[1];
                int endX = moveStr[2] - 'a';
                int endY = '1' + 7 - moveStr[3];

                start = (startX, startY).ToIndex();
                end = (endX, endY).ToIndex();

                if(moveStr.Length == 5) {
                    switch(moveStr[4]) {
                        case 'q':
                            type = MoveType.PromoteQueen; break;
                        case 'r':
                            type = MoveType.PromoteRook; break;
                        case 'b':
                            type = MoveType.PromoteBishop; break;
                        case 'n':
                            type = MoveType.PromoteKnight; break;
                    }
                }
            }
            pieceMoved = board.pieces.GetValueOrDefault(start);
            pieceTaken = board.pieces.GetValueOrDefault(end);
        }

        public void MakeMove(bool updateDict = true) {
            if(pieceMoved.GetPieceValue() == 'p'.GetPieceValue() && end == board.epSquare.Item1)
                type = MoveType.EnPassant;
            board.ResetRights();

            ulong startRank = (ulong) 0b11111111 << (WhiteMove? 8: 48);
            ulong targetRank = (ulong) 0b11111111 << (WhiteMove? 24: 32);
            if(
                pieceMoved.GetPieceValue() == 'p'.GetPieceValue() &&
                ((1ul << start) & startRank) > 0 &&
                ((1ul << end) & targetRank) > 0
            ) {
                type = MoveType.DoublePush;
            }

            Dictionary<int, ulong> bitboard = board.GetBitboards(WhiteMove);
            Dictionary<int, ulong> enemyBitboard = board.GetBitboards(!WhiteMove);
            int colour = WhiteMove? 0b01000: 0b10000;

            if(type == MoveType.LongCastle) {
                ulong mask = 1ul << (start + 2) | 1ul << start;
                bitboard['k'.GetPieceValue()] ^= mask;
                ulong rookMask = 1ul << (start + 1) | 1ul << (start + 4);
                bitboard['r'.GetPieceValue()] ^= rookMask;
            
                board.pieces.Remove(start, out int king);
                board.pieces[start + 2] = king;
                board.pieces.Remove(start + 4, out int rook);
                board.pieces[start + 1] = rook;

                board.kings[WhiteMove? 0: 1] = start + 2;

                board.SetCastlingRights(true, WhiteMove);
                board.SetCastlingRights(false, WhiteMove);
            }
            else if(type == MoveType.ShortCastle) {
                ulong mask = 1ul << (start - 2) | 1ul << start;
                bitboard['k'.GetPieceValue()] ^= mask;
                ulong rookMask = 1ul << (start - 1) | 1ul << (start - 3);
                bitboard['r'.GetPieceValue()] ^= rookMask;

                board.pieces.Remove(start, out int king);
                board.pieces[start - 2] = king;
                board.pieces.Remove(start - 3, out int rook);
                board.pieces[start - 1] = rook;

                board.kings[WhiteMove? 0: 1] = start - 2;

                board.SetCastlingRights(true, WhiteMove);
                board.SetCastlingRights(false, WhiteMove);
            }
            else {
                ulong mask = 1ul << start | 1ul << end;
                bitboard[pieceMoved.GetPieceValue()] ^= mask;
                board.pieces[end] = pieceMoved;
                board.pieces.Remove(start);
                if(pieceTaken > 0) {
                    enemyBitboard[pieceTaken.GetPieceValue()] ^= 1ul << end;
                    board.pieceCounts[pieceTaken]--;
                }

                if(type == MoveType.PromoteQueen) {
                    bitboard['q'.GetPieceValue()] ^= 1ul << end;
                    bitboard['p'.GetPieceValue()] ^= 1ul << end;
                    int piece = 'q'.GetPieceValue() | colour;
                    board.pieces[end] = piece;
                    board.pieceCounts[piece]++;
                }
                else if(type == MoveType.PromoteRook) {
                    bitboard['r'.GetPieceValue()] ^= 1ul << end;
                    bitboard['p'.GetPieceValue()] ^= 1ul << end;
                    int piece = 'r'.GetPieceValue() | colour;
                    board.pieces[end] = piece;
                    board.pieceCounts[piece]++;
                }
                else if(type == MoveType.PromoteBishop) {
                    bitboard['b'.GetPieceValue()] ^= 1ul << end;
                    bitboard['p'.GetPieceValue()] ^= 1ul << end;
                    int piece = 'b'.GetPieceValue() | colour;
                    board.pieces[end] = piece;
                    board.pieceCounts[piece]++;
                }
                else if(type == MoveType.PromoteKnight) {
                    bitboard['n'.GetPieceValue()] ^= 1ul << end;
                    bitboard['p'.GetPieceValue()] ^= 1ul << end;
                    int piece = 'n'.GetPieceValue() | colour;
                    board.pieces[end] = piece;
                    board.pieceCounts[piece]++;
                }
                else if(type == MoveType.DoublePush) {
                    int offset = WhiteMove? -8: 8;
                    board.epSquare = (end + offset, end);
                }
                else if(type == MoveType.EnPassant) {
                    (int, int) epSquare = board.epStack[^1];
                    if(epSquare.Item1 >= 0) {
                        int index = epSquare.Item2;
                        enemyBitboard['p'.GetPieceValue()] ^= 1ul << index;
                        board.pieces.Remove(index);
                        board.pieceCounts[Pieces.GetPiece('p', !WhiteMove)]--;
                    }
                }

                if(pieceMoved.GetPieceValue().GetPieceChar() == 'k') {
                    board.SetCastlingRights(true, WhiteMove);
                    board.SetCastlingRights(false, WhiteMove);
                    board.kings[WhiteMove? 0: 1] = end;
                }
                else if(pieceMoved.GetPieceValue().GetPieceChar() == 'r') {
                    if(start % 8 == 0)
                        board.SetCastlingRights(true, WhiteMove);
                    else if(start % 8 == 7)
                        board.SetCastlingRights(false, WhiteMove);
                }
            }
        }

        public void UndoMove() {
            board.RestoreRights();
            ulong startRank = (ulong) 0b11111111 << (WhiteMove? 8: 48);
            ulong targetRank = (ulong) 0b11111111 << (WhiteMove? 24: 32);
            if(
                pieceMoved.GetPieceValue() == 'p'.GetPieceValue() &&
                ((1ul << start) & startRank) > 0 &&
                ((1ul << end) & targetRank) > 0
            ) {
                type = MoveType.DoublePush;
            }

            Dictionary<int, ulong> bitboard = board.GetBitboards(WhiteMove);
            Dictionary<int, ulong> enemyBitboard = board.GetBitboards(!WhiteMove);

            if(type == MoveType.LongCastle) {
                ulong mask = 1ul << (start + 2) | 1ul << start;
                bitboard['k'.GetPieceValue()] ^= mask;
                ulong rookMask = 1ul << (start + 1) | 1ul << (start + 4);
                bitboard['r'.GetPieceValue()] ^= rookMask;
                
                board.kings[WhiteMove? 0: 1] = start;

                board.pieces.Remove(start + 2, out int king);
                board.pieces[start] = king;
                board.pieces.Remove(start + 1, out int rook);
                board.pieces[start + 4] = rook;
            }
            else if(type == MoveType.ShortCastle) {
                ulong mask = 1ul << (start - 2) | 1ul << start;
                bitboard['k'.GetPieceValue()] ^= mask;
                ulong rookMask = 1ul << (start - 1) | 1ul << (start - 3);
                bitboard['r'.GetPieceValue()] ^= rookMask;

                board.kings[WhiteMove? 0: 1] = start;

                board.pieces.Remove(start - 2, out int king);
                board.pieces[start] = king;
                board.pieces.Remove(start - 1, out int rook);
                board.pieces[start - 3] = rook;
            }
            else {
                ulong mask = 1ul << start | 1ul << end;
                bitboard[pieceMoved.GetPieceValue()] ^= mask;
                board.pieces.Remove(end);
                board.pieces[start] = pieceMoved;

                if(type == MoveType.PromoteQueen) {
                    bitboard['q'.GetPieceValue()] ^= 1ul << end;
                    bitboard['p'.GetPieceValue()] ^= 1ul << end;
                    board.pieces.Remove(end, out int piece);
                    board.pieceCounts[piece]--;
                }
                else if(type == MoveType.PromoteRook) {
                    bitboard['r'.GetPieceValue()] ^= 1ul << end;
                    bitboard['p'.GetPieceValue()] ^= 1ul << end;
                    board.pieces.Remove(end, out int piece);
                    board.pieceCounts[piece]--;
                }
                else if(type == MoveType.PromoteBishop) {
                    bitboard['b'.GetPieceValue()] ^= 1ul << end;
                    bitboard['p'.GetPieceValue()] ^= 1ul << end;
                    board.pieces.Remove(end, out int piece);
                    board.pieceCounts[piece]--;
                }
                else if(type == MoveType.PromoteKnight) {
                    bitboard['n'.GetPieceValue()] ^= 1ul << end;
                    bitboard['p'.GetPieceValue()] ^= 1ul << end;
                    board.pieces.Remove(end, out int piece);
                    board.pieceCounts[piece]--;
                }
                else if(type == MoveType.EnPassant) {
                    (int, int) epSquare = board.epSquare;
                    if(epSquare.Item1 >= 0) {
                        int index = epSquare.Item2;
                        enemyBitboard['p'.GetPieceValue()] ^= 1ul << index;
                        int piece = Pieces.GetPiece('p', !WhiteMove);;
                        board.pieces[index] = piece;
                        board.pieceCounts[piece]++;
                    }
                }
                if(pieceTaken > 0) {
                    enemyBitboard[pieceTaken.GetPieceValue()] ^= 1ul << end;
                    board.pieces[end] = pieceTaken;
                    board.pieceCounts[pieceTaken]++;
                }

                if(pieceMoved.GetPieceValue().GetPieceChar() == 'k')
                    board.kings[WhiteMove? 0: 1] = start;
            }
        }

        public override string ToString()
        {
            if(type == MoveType.ShortCastle)
                return "O-O";
            if(type == MoveType.LongCastle)
                return "O-O-O";
            
            if(pieceMoved == 0)
                return "";

            char startFile = (char)('a' + StartSquare.Item1);
            char startRank = (char)('1' + 7 - StartSquare.Item2);
            char targetFile = (char)('a' + EndSquare.Item1);
            char targetRank = (char)('1' + 7 - EndSquare.Item2);

            string output = $"{startFile}{startRank}{targetFile}{targetRank}";
            switch(type) {
                case MoveType.PromoteQueen:
                    output += 'q'; break;
                case MoveType.PromoteRook:
                    output += 'r'; break;
                case MoveType.PromoteBishop:
                    output += 'b'; break;
                case MoveType.PromoteKnight:
                    output += 'n'; break;
            }
            return output;
        }
    }
}