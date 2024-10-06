using System.Net.NetworkInformation;
using Chess.Utils;

namespace Chess.Zobrist {
    public static class ZobristHashing {
        public static ulong[] keys = new ulong[12 * 64 + 1];
        public static void InitZobrist() {
            Random random = new(0);
            for(int i = 0; i < 12 * 64 + 1; i++) {
                keys[i] = (ulong)random.NextInt64(0, long.MaxValue);
            }
        }

        public static int GetZobristIndex(this int piece, int squareIndex) {
            return (piece.GetPieceValue() + (piece.IsWhitePiece()? 6: 0) - 1) * 64 + squareIndex;
        }
    }
}