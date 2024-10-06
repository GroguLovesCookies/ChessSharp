using Chess.Classes;

namespace Chess.Utils {
    public static class BitboardUtils {
        public static List<Move> BitboardToMoveArray(this ulong bitboard, Board board, int start, int min = 0, int max = 64) {
            List<Move> output = [];
            for(int i = min; i < max; i++) {
                if(((bitboard >> i) & 0b1) == 1)
                    output.Add(new Move(board, start, i));
            }
            return output;
        }

        public static List<Move> BitboardToMoveArray(this ulong bitboard, Board board, int start, params int[] indices) {
            List<Move> output = [];
            foreach(int i in indices) {
                if(i < 0)
                    continue;
                if(((bitboard >> i) & 0b1) == 1)
                    output.Add(new Move(board, start, i));
            }
            return output;
        }

        public static List<Move> BitboardToPawnMoveArray(this ulong bitboard, Board board, int start, params int[] indices) {
            List<Move> output = [];
            bool white = board.pieces[start].IsWhitePiece();
            ulong mask = (ulong)0b11111111 << (white? 56: 0);
            foreach(int i in indices) {
                if(((bitboard >> i) & 0b1) == 1) {
                    if(((1ul << i) & mask) > 0) {
                        output.Add(new(board, start, i, Move.MoveType.PromoteQueen));
                        output.Add(new(board, start, i, Move.MoveType.PromoteRook));
                        output.Add(new(board, start, i, Move.MoveType.PromoteBishop));
                        output.Add(new(board, start, i, Move.MoveType.PromoteKnight));
                    }
                    else
                        output.Add(new(board, start, i));
                }
            }

            return output;
        }
    }
}