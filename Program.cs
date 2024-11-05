using System.Diagnostics;
using Chess.Bitboards;
using Chess.ChessEngine;
using Chess.Classes;
using Chess.MoveGen;
using Chess.Utils;
using Chess.Zobrist;

namespace Chess {
    public class MainClass {
        public static void Main(string[] args) {
            Pieces.InitializePieces();
            ZobristHashing.InitZobrist();
            Board board = new("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR") {
            };

            Masks.Initialize();
            Engine engine = new(board);

            string input = "";
            MoveGenerator generator = new(board);

            Stopwatch stopwatch = new();

            while(input != "q") {
                Console.Write("Enter move: ");
                input = Console.ReadLine();
                Move made = new(board, input);
                made.MakeMove();

                stopwatch.Start();
                int depth = board.pieceCounts[Pieces.GetPiece('p', true)] == 0? 7: 7;
                engine.Search(depth, -1000000000, 1000000000);
                stopwatch.Stop();
                Console.WriteLine(engine.bestMove.ToString());
                Console.WriteLine(stopwatch.ElapsedMilliseconds);
                engine.bestMove.MakeMove();
                stopwatch.Reset();
            }
        }
    }
}