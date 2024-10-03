using System.Text.Json;
using Chess.Utils;

namespace Chess.Bitboards {
    public static class Masks {
        public static readonly ulong[] knightMasks = new ulong[64];
        public static readonly ulong[] kingMasks = new ulong[64];
        public static readonly ulong[] rookMasks = new ulong[64];
        public static readonly ulong[] bishopMasks = new ulong[64];
        public static readonly ulong[] whitePawnMasks = new ulong[64];
        public static readonly ulong[] blackPawnMasks = new ulong[64];
        public static readonly ulong[] whitePawnCaptures = new ulong[64];
        public static readonly ulong[] blackPawnCaptures = new ulong[64];
        public static readonly ulong[] inFrontMasks = new ulong[8];
        public static readonly int[,] distances =  new int[64, 8];
        public static readonly int[] offsets = [-1, -8, 1, 8, -9, -7, 9, 7];
        public static readonly Dictionary<(int, ulong), ulong> rookTable = []; 
        public static readonly Dictionary<(int, ulong), ulong> bishopTable = []; 
        public const ulong kingsideCastleMask = 0b00000110;
        public const ulong queensideCastleMask = 0b01110000;
        public const ulong castleKingMask = 0b00001000;
        public const ulong kingsideRookMask = 0b00000001;
        public const ulong queensideRookMask = 0b10000000;
        public const ulong queensideCheckMask = 0b00110000;

        public static void GetKnightMask() {
            for(int i = 0; i < 64; i++) {
                ulong output = 0;
                (int, int) coords = i.ToSquare();
                int x = coords.Item1, y = coords.Item2;
                if(x > 0) {
                    if(y > 1)
                        output |= 1ul << (i + 17);
                    if(y < 6)
                        output |= 1ul << (i - 15);
                }
                if(x > 1) {
                    if(y > 0)
                        output |= 1ul << (i + 10);
                    if(y < 7)
                        output |= 1ul << (i - 6);
                }
                if(x <  7) {
                    if(y > 1)
                        output |= 1ul << (i + 15);
                    if(y < 6)
                        output |= 1ul << (i - 17);
                }
                if(x < 6) {
                    if(y > 0)
                        output |= 1ul << (i + 6);
                    if(y < 7)
                        output |= 1ul << (i - 10);
                }
                knightMasks[i] = output;
            }
        }

        public static void GetKingMask() {
            for(int i = 0; i < 64; i++) {
                ulong output = 0;
                (int, int) coords = i.ToSquare();
                int x = coords.Item1, y = coords.Item2;

                if(x > 0) {
                    output |= 1ul << (i + 1);
                    if(y > 0)
                        output |= 1ul << (i + 9);
                    if(y < 7)
                        output |= 1ul << (i - 7);
                }
                if(x < 7) {
                    output |= 1ul << (i - 1);
                    if(y > 0)
                        output |= 1ul << (i + 7);
                    if(y < 7)
                        output |= 1ul << (i - 9);
                }
                if(y > 0)
                    output |= 1ul << (i + 8);
                if(y < 7)
                    output |= 1ul << (i - 8);

                kingMasks[i] = output;
            }
        }

        public static void PrecomputeDistances() {
            for(int i = 0; i < 64; i++) {
                (int, int) coords = i.ToSquare();
                int x = coords.Item1, y = coords.Item2;

                distances[i, 0] = 8 - x;
                distances[i, 1] = 8 - y;
                distances[i, 2] = x + 1;
                distances[i, 3] = y + 1;
                distances[i, 4] = Math.Min(8-x, 8-y);
                distances[i, 5] = Math.Min(x+1, 8-y);
                distances[i, 6] = Math.Min(x+1, y+1);
                distances[i, 7] = Math.Min(8-x, y+1);
            }
        }

        public static void GetSlidingMask(char piece = 'r') {
            int start = piece == 'r'? 0: 4, end = piece == 'r'? 4: 8;
            for(int i = 0; i < 64; i++) {
                ulong output = 0;
                for(int j = start; j < end; j++) {
                    int offset = offsets[j];
                    int distance = distances[i, j];

                    for(int t = 1; t < distance; t++) {
                        output |= 1ul << (i + t * offset);
                    }
                }
                if(piece == 'r')
                    rookMasks[i] = output;
                else
                    bishopMasks[i] = output;
            }
        }

        public static ulong[] CreateAllBlockerBitboards(ulong movementMask) {
            List<int> squareIndices = [];
            for(int i = 0; i < 64; i++) {
                if(((movementMask >> i) & 1ul) == 1ul)
                    squareIndices.Add(i);
            }

            long numPatterns = 1 << squareIndices.Count;
            ulong[] blockerBitboards = new ulong[numPatterns];
            for(int i = 0; i < numPatterns; i++) {
                for(int j = 0; j < squareIndices.Count; j++) {
                    ulong bit = (ulong)(i >> j) & 1ul;
                    blockerBitboards[i] |= bit << squareIndices[j];
                }
            }

            return blockerBitboards;
        }

        public static ulong GenerateSlidingLegalMoves(int startSquare, ulong blockers, char piece = 'r') {
            ulong output = 0;

            int start = piece == 'r'? 0: 4, end = piece == 'r'? 4: 8;
            for(int i = start; i < end; i++) {
                int offset = offsets[i];
                int distance = distances[startSquare, i];
                for(int t = 1; t < distance; t++) {
                    int targetSquare = startSquare + t * offset;
                    output |= 1ul << targetSquare;
                    if(((blockers >> targetSquare) & 1ul) == 1ul)
                        break;
                }
            }

            return output;
        }

        public static void GenerateSlidingLookupTable(char piece = 'r') {
            for(int i = 0; i < 64; i++) {
                ulong movementMask = (piece == 'r'? rookMasks: bishopMasks)[i];
                ulong[] blockerPatterns = CreateAllBlockerBitboards(movementMask);
                foreach(ulong bitboard in blockerPatterns) {
                    ulong legalBitboard = GenerateSlidingLegalMoves(i, bitboard, piece);
                    if(piece == 'r')
                        rookTable[(i, bitboard)] = legalBitboard;
                    else
                        bishopTable[(i, bitboard)] = legalBitboard;
                }
            }
        }

        public static void GeneratePawnMasks(bool white) {
            int doublePushRank = white? 6: 1;
            int offsetIndex = white? 3: 1;
            for(int i = 0; i < 64; i++) {
                ulong output = 0;
                (int, int) coords = i.ToSquare();
                int y = coords.Item2;
                
                int dist = Math.Min(distances[i, offsetIndex], y == doublePushRank? 3: 2);
                int offset = offsets[offsetIndex];
                for(int t = 1; t < dist; t++) {
                    output |= 1ul << (i + t * offset);
                }
                (white? whitePawnMasks: blackPawnMasks)[i] = output;
            }
        }

        public static void GeneratePawnCaptures(bool white) {
            int start = white? 6: 4, end = white? 8: 6;
            for(int i = 0; i < 64; i++) {
                ulong output = 0;
                for(int offsetIndex = start; offsetIndex < end; offsetIndex++) {
                    int distance = Math.Min(distances[i, offsetIndex], 2);
                    int offset = offsets[offsetIndex];

                    for(int t = 1; t < distance; t++) {
                        output |= 1ul << (i + t*offset);
                    }
                }
                (white? whitePawnCaptures: blackPawnCaptures)[i] = output;
            }
        }

        public static void Initialize() {
            PrecomputeDistances();
            GetSlidingMask('r');
            GetSlidingMask('b');
            GeneratePawnMasks(true);
            GeneratePawnMasks(false);
            GeneratePawnCaptures(true);
            GeneratePawnCaptures(false);
            GetKnightMask();
            GetKingMask();
            GenerateSlidingLookupTable('r');
            GenerateSlidingLookupTable('b');
        }
    }
}