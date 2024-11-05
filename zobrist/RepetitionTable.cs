using Chess.Classes;

namespace Chess.Zobrist {
    public class RepetitionTable {
        readonly ulong[] hashes;
        readonly int[] startIndices;
        int count;

        public RepetitionTable() {
            hashes = new ulong[256];
            startIndices = new int[hashes.Length + 1];
        }

        public void Push(ulong hash, bool reset) {
            if(count < hashes.Length) {
                hashes[count] = hash;
                startIndices[count + 1] = reset? count: startIndices[count];
            }

            count++;
        }

        public void TryPop() {
            count = Math.Max(0, count-1);
        }

        public bool Contains(ulong h) {
            int s = startIndices[count];
            for(int i = s; i < count - 1; i++) {
                if(hashes[i] == h)
                    return true;
            }

            return false;
        }
    }
}