using Chess.Classes;
using Chess.MoveGen;
using Chess.Utils;

namespace Chess.Tests {
    public class MoveGenerationTest(Board board) {
        Board board = board;
        MoveGenerator generator = new(board);
        public int GenerationTest(int depth, int logDepth = 5) {
            if(depth == 0)
                return 1;
            
            List<Move> moves = generator.GenerateMoves(board.white);
            int positions = 0;

            foreach(Move move in moves) {
                move.MakeMove();
                // if(depth == 2 && move.ToString() == "a2a3") {
                //     foreach(Move move1 in generator.GenerateMoves(board.white)) {
                //         Console.WriteLine(move1.ToString());
                //     }
                // }
                int newMoves = GenerationTest(depth - 1, logDepth);
                if(depth == logDepth) {
                    Console.WriteLine($"{move}: {newMoves}");
                }
                positions += newMoves;
                move.UndoMove();
            }

            return positions;
        }
    }
}