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
            
            // var moves = generator.GenerateMoves(true);
            // foreach(int i in Engine.SortMoves(moves)) {
            //     Console.WriteLine($"{moves[i]}");
            // }

            while(input != "q") {
                Console.Write("Enter move: ");
                input = Console.ReadLine();
                Move made = new(board, input);
                made.MakeMove();

                Move best = new(board, 0, 0);
                stopwatch.Start();
                engine.Search(6, -1000000000, 1000000000, ref best);
                stopwatch.Stop();
                Console.WriteLine(best.ToString());
                // Console.WriteLine(Engine.swaps);
                Console.WriteLine(stopwatch.ElapsedMilliseconds);
                best.MakeMove();
            }
        }
    }
}