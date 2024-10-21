using System.Runtime.InteropServices;
using Chess.Classes;

namespace Chess.Zobrist {
    public class TranspositionTable {
        public readonly Board board;

        public Entry[] entries;
        public ulong count;

        public TranspositionTable(Board board, int sizeMB)
        {
            this.board = board;

			int numEntries = 250000;

			count = (ulong)(numEntries);
			entries = new Entry[numEntries];
        }

        public ulong Index => board.zobrist % count; 

        public int LookupEvaluation(int depth, int plyFromRoot, int alpha, int beta, out Entry entry) {
            entry = entries[Index];
            if(entry.key == board.zobrist) {
                if(entry.depth >= depth) {
                    if(entry.type == 0 || (entry.type == 1 && entry.value <= alpha) || (entry.type == 2 && entry.value >= beta))
                        return entry.value;
                }
            }
            return -1;
        }

        public void StoreEval(int depth, int plySearched, int eval, int evalType, Move move) {
            Entry entry = new(board.zobrist, eval, (byte) depth, (byte) evalType, move);
            entries[Index] = entry;
        }


        public struct Entry(ulong key, int value, byte depth, byte type, Move move) {
            public readonly ulong key = key;
            public readonly int value = value;
            public readonly Move move = move;
            public readonly byte depth = depth;
            public readonly byte type = type;
        }
    }
}