using System;
using System.Collections.Generic;
using Chess.Bitboards;
using Chess.ChessEngine;
using Chess.Classes;
using Chess.MoveGen;
using Chess.Tests;
using Chess.Utils;

namespace Chess {
    public class MainClass {
        public static void Main(string[] args) {
            Board board = new("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1");
            board.SetCastlingRights(true, true, false);
            board.SetCastlingRights(false, true, false);
            board.SetCastlingRights(true, false, false);
            board.SetCastlingRights(false, false, false);


            Masks.Initialize();
            Engine engine = new(board);

            string input = "";
            while(input != "q") {
                Console.Write("Enter move: ");
                input = Console.ReadLine();
                Move made = new(board, input);
                made.MakeMove();

                Move best = new(board, 0, 0);
                engine.Search(4, -1000000000, 1000000000, ref best);
                Console.WriteLine(best.ToString());
                best.MakeMove();
            }
        }
    }
}