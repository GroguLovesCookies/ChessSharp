using System;
using System.Collections.Generic;
using System.Diagnostics;
using Chess.Bitboards;
using Chess.ChessEngine;
using Chess.Classes;
using Chess.MoveGen;
using Chess.Tests;
using Chess.Utils;

namespace Chess {
    public class MainClass {
        public static void Main(string[] args) {
            // Board board = new("r2qkb1r/pppnpppp/5n2/3p1b2/3P1B2/6P1/PPP1PPBP/RN1QK1NR");


            // Masks.Initialize();
            // Engine engine = new(board);

            // string input = "";
            // MoveGenerator generator = new(board);

            // Stopwatch stopwatch = new();

            // while(input != "q") {
            //     Console.Write("Enter move: ");
            //     input = Console.ReadLine();
            //     Move made = new(board, input);
            //     made.MakeMove();

            //     Move best = new(board, 0, 0);
            //     stopwatch.Start();
            //     engine.Search(6, -1000000000, 1000000000, ref best);
            //     stopwatch.Stop();
            //     Console.WriteLine(best.ToString());
            //     // Console.WriteLine(Engine.swaps);
            //     Console.WriteLine(stopwatch.ElapsedMilliseconds);
            //     best.MakeMove();
            // }
        }
    }
}