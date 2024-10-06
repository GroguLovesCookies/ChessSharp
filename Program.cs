using System;
using System.Collections.Generic;
using System.Diagnostics;
using Chess.Bitboards;
using Chess.ChessEngine;
using Chess.Classes;
using Chess.MoveGen;
using Chess.Tests;
using Chess.Utils;
using Chess.Zobrist;

namespace Chess {
    public class MainClass {
        public static void Main(string[] args) {
            Pieces.InitializePieces();
            ZobristHashing.InitZobrist();
            Board board = new("rnbq1k1r/pp1Pbppp/2p5/8/2B3n1/8/PPP1N1PP/RNBQK2R") {
                castling = [true, true, false, false],
                white = false
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
                engine.Search(8, -1000000000, 1000000000);
                stopwatch.Stop();
                Console.WriteLine(engine.bestMove.ToString());
                Console.WriteLine(stopwatch.ElapsedMilliseconds);
                engine.bestMove.MakeMove();
            }
        }
    }
}