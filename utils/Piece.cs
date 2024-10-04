using System;

namespace Chess.Utils {
    public static class Pieces {
        public static int GetPieceValue(this char piece) {
            return piece switch
            {
                'p' => 0b00001,
                'n' => 0b00010,
                'b' => 0b00011,
                'r' => 0b00100,
                'q' => 0b00101,
                'k' => 0b00110,
                _ => 0b00000,
            };
        }

        public static char GetPieceChar(this int piece) {
            return piece switch
            {
                0b00001 => 'p',
                0b00010 => 'n',
                0b00011 => 'b',
                0b00100 => 'r',
                0b00101 => 'q',
                0b00110 => 'k',
                _ => ' ',
            };
        }

        public static int GetPiece(char piece, bool white) {
            return piece.GetPieceValue() | (white? 0b01000: 0b10000);
        }

        public static string ToBitboardString(this ulong bitboard) {
            string temp = Convert.ToString((long) bitboard, 2);
            string output = "";
            for(int i = 0; i < 64 - temp.Length; i++) {
                output += "0";
            }
            output += temp;
            for(int i = 7; i < 64; i += 8) {
                output.Insert(i, "\n");
            }
            return output;
        }

        public static void PrintBitboard(this ulong bitboard) {
            string output = bitboard.ToBitboardString();
            for(int i = 0; i < 64; i++) {
                char chr = output[i];
                Console.Write(chr);
                if(i % 8 == 7) {
                    Console.Write("\n");
                }
            }
        }

        public static int GetPieceValue(this int piece) => piece & 0b00111;
        public static int GetPieceColor(this int piece) => piece & 0b11000;
        public static char ToPieceString(this int piece) {
            char chr = piece.GetPieceValue().GetPieceChar();
            return piece.GetPieceColor() == 0b01000? char.ToUpper(chr): chr;
        }
        public static bool IsWhitePiece(this int piece) => piece.GetPieceColor() == 0b01000;
        public static bool IsEmpty(this int piece) => piece == 0;
        public static bool IsSameColor(this int piece1, int piece2) => piece1.GetPieceColor() == piece2.GetPieceColor();
        public static bool IsOrthogonalPiece(this int piece) => piece.GetPieceValue() == 'r'.GetPieceValue() || piece.GetPieceValue() == 'q'.GetPieceValue();
        public static bool IsDiagonalPiece(this int piece) => piece.GetPieceValue() == 'b'.GetPieceValue() || piece.GetPieceValue() == 'q'.GetPieceValue();

        public static int ToIndex(this (int, int) coords) => 63 - (coords.Item1 + coords.Item2 * 8);
        public static (int, int) ToSquare(this int index) => ((63 - index) % 8, (63 - index) / 8);
    }
}