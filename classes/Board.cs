using System.Collections.Generic;
using System;
using Chess.Utils;
using Chess.Zobrist;
using Chess.ChessEngine;

namespace Chess.Classes {
    public class Board {
        public readonly int width = 8, height = 8;
        public (int, int) epSquare = (-1, -1);
        public bool[] castling = [true, true, true, true];
        public bool white = true;
        public ulong pins = 0;
        public ulong[] pinRays = new ulong[8];

        public List<(int, int)> epStack = [];
        public List<bool[]> castleStack = [];

        public int[] kings = new int[2];
        public bool inCheck = false;
        public ulong zobrist = 0;

        public int whiteQuality = 0;
        public int blackQuality = 0;

        public int lastWhiteMoved = 0, lastBlackMoved = 0;
        public List<int> lastWhiteStack = [], lastBlackStack = [];

        public Dictionary<int, int> pieceCounts = new() {
            [Pieces.GetPiece('p', true)] = 0,
            [Pieces.GetPiece('n', true)] = 0,
            [Pieces.GetPiece('b', true)] = 0,
            [Pieces.GetPiece('r', true)] = 0,
            [Pieces.GetPiece('q', true)] = 0,
            [Pieces.GetPiece('k', true)] = 0,
            [Pieces.GetPiece('p', false)] = 0,
            [Pieces.GetPiece('n', false)] = 0,
            [Pieces.GetPiece('b', false)] = 0,
            [Pieces.GetPiece('r', false)] = 0,
            [Pieces.GetPiece('q', false)] = 0,
            [Pieces.GetPiece('k', false)] = 0,
        };

        public ulong attackedSquares = 0;
        public Dictionary<int, int> pieces = [];
        public Dictionary<int, ulong> attackedDict = new() {
            ['p'.GetPieceValue()] = 0,
            ['n'.GetPieceValue()] = 0,
            ['b'.GetPieceValue()] = 0,
            ['r'.GetPieceValue()] = 0,
            ['q'.GetPieceValue()] = 0,
            ['k'.GetPieceValue()] = 0,
        };
        public Dictionary<int, ulong> whiteBitboards = new() {
            ['p'.GetPieceValue()] = 0,
            ['n'.GetPieceValue()] = 0,
            ['b'.GetPieceValue()] = 0,
            ['r'.GetPieceValue()] = 0,
            ['q'.GetPieceValue()] = 0,
            ['k'.GetPieceValue()] = 0,
        };
        public Dictionary<int, ulong> blackBitboards = new() {
            ['p'.GetPieceValue()] = 0,
            ['n'.GetPieceValue()] = 0,
            ['b'.GetPieceValue()] = 0,
            ['r'.GetPieceValue()] = 0,
            ['q'.GetPieceValue()] = 0,
            ['k'.GetPieceValue()] = 0,
        };

        public Board() {}
        public Board(string fen) {
            int i = 0;
            foreach(char chr in fen) {
                if(i >= width * height)
                    break;
                
                if(char.IsDigit(chr)) {
                    i += chr - '0' - 1;
                }
                else if(char.IsLetter(chr)) {
                    int colour = char.IsAsciiLetterUpper(chr)? 0b01000: 0b10000;
                    int piece = char.ToLower(chr).GetPieceValue();
                    Dictionary<int, ulong> bitboards = GetBitboards(char.IsAsciiLetterUpper(chr));
                    bitboards[piece] |= 1ul << (63 - i);
                    pieces[63 - i] = piece | colour;
                    ChangeQuality(char.IsAsciiLetterUpper(chr), Engine.GetSquarePoints(piece | colour, 63 - i));
                    pieceCounts[piece | colour]++;
                    zobrist |= ZobristHashing.keys[ZobristHashing.GetZobristIndex(piece | colour, 63 - i)];
                    if(char.ToLower(chr) == 'k')
                        kings[char.IsAsciiLetterUpper(chr)? 0: 1] = 63 - i;
                }
                else
                    continue;

                i += 1;
            }
        }

        public void SetLastMoved(bool white, int lastMoved) {
            if(white) {
                lastWhiteStack.Add(lastWhiteMoved);
                lastWhiteMoved = lastMoved;
            }
            else {
                lastBlackStack.Add(lastBlackMoved);
                lastBlackMoved = lastMoved;
            }
        }

        public void ResetLastMoved(bool white) {
            if(white) {
                lastWhiteMoved = lastWhiteStack[^1];
                lastWhiteStack.RemoveAt(lastWhiteStack.Count - 1);
            }
            else {
                lastBlackMoved = lastBlackStack[^1];
                lastBlackStack.RemoveAt(lastBlackStack.Count - 1);
            }
        }
        
        public int GetLastMoved(bool white) => white? lastWhiteMoved: lastBlackMoved;

        public void ChangeQuality(bool white, int amount) {
            if(white)
                whiteQuality += amount;
            else
                blackQuality += amount;
        }

        public void SetCastlingRights(bool kingside, bool white, bool targetValue = false) {
            int i = white? 0: 2;
            i += kingside? 0: 1;
            castling[i] = targetValue;
        }

        public bool GetCastlingRights(bool kingside, bool white) {
            int i = white? 0: 2;
            i += kingside? 0: 1;
            return castling[i];
        }

        public void ResetRights() {
            epStack.Add(epSquare);
            epSquare = (-1, -1);
            castleStack.Add((bool[])castling.Clone());
            white = !white;
        }

        public void RestoreRights() {
            epSquare = epStack[^1];
            epStack.RemoveAt(epStack.Count - 1);
            castling = (bool[])castleStack[^1].Clone();
            castleStack.RemoveAt(castleStack.Count - 1);
            white = !white;
        }

        public ulong GetBitboard(bool white, int piece) {
            return GetBitboards(white)[piece];
        }
        public ulong GetBitboard(bool white) {
            return white? WhiteBitboard: BlackBitboard;
        }
        public ulong GetBitboard(bool white, char piece) {
            return GetBitboard(white, piece.GetPieceValue());
        }


        public string FEN {
            get {
                string fen = "";
                int gap = 0;
                ulong totalBitboard = BlackBitboard | WhiteBitboard;
                for(int i = 63; i >= 0; i--) {
                    if(((totalBitboard >> i) & 0b1) == 0)
                        gap++;
                    else {
                        if(gap > 0) {
                            fen += gap.ToString();
                            gap = 0;
                        }
                        int piece = 0;
                        bool white = true;
                        bool found = false;
                        foreach(KeyValuePair<int, ulong> bitboard in whiteBitboards) {
                            if(((bitboard.Value >> i) & 0b1) == 1) {
                                found = true;
                                piece = bitboard.Key;
                                break;
                            }
                        }

                        if(!found) {
                            white = false;
                            foreach(KeyValuePair<int, ulong> bitboard in blackBitboards) {
                                if(((bitboard.Value >> i) & 0b1) == 1) {
                                    found = true;
                                    piece = bitboard.Key;
                                    break;
                                }
                            }
                        }

                        if(found) {
                            char chr = piece.GetPieceChar();
                            fen += white? char.ToUpper(chr): chr;
                        }
                    }
                }
                return fen;
            }
        }

        public Dictionary<int, ulong> GetBitboards(bool white) => white? whiteBitboards: blackBitboards;
        public ulong WhiteBitboard {
            get {
                ulong output = 0;
                foreach(KeyValuePair<int, ulong> bitboard in whiteBitboards)
                    output |= bitboard.Value;
                return output;
            }
        }
        public ulong BlackBitboard {
            get {
                ulong output = 0;
                foreach(KeyValuePair<int, ulong> bitboard in blackBitboards)
                    output |= bitboard.Value;
                return output;
            }
        }

        public ulong GetOrthogonalBitboards(bool white) => GetBitboard(white, 'q') | GetBitboard(white, 'r');
        public ulong GetDiagonalBitboards(bool white) => GetBitboard(white, 'q') | GetBitboard(white, 'b');
        public bool IsPinned(int start) => ((pins >> start) & 0b1) > 0;
        public ulong? FindPinIndex(int start) {
            for(int i = 0; i < 8; i++) {
                ulong pin = pinRays[i];
                if(((pin >> start) & 0b1) != 0)
                    return pin;
            }
            return null;
        }
    }
}