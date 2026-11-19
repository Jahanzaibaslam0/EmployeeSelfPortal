using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace HRMS.Services
{
    /// <summary>
    /// GS1 DataMatrix (ECC200) with FNC1 in first position.
    /// AIs: (01) GTIN-14, (11) mfg YYMMDD, (10) batch, (17) expiry YYMMDD, (21) serial.
    /// Placement and Reed-Solomon follow ISO/IEC 16022 / ZXing ECC200.
    /// </summary>
    public static class Gs1DataMatrix
    {
        public const char GroupSeparator = '\u001D';

        public static string Gtin14(string gtin)
        {
            var digits = Regex.Replace(gtin ?? "", @"\D", "");
            if (digits.Length == 0) return "";
            if (digits.Length > 14) digits = digits.Substring(digits.Length - 14);
            return digits.PadLeft(14, '0');
        }

        public static string YyMmDd(DateTime? date)
        {
            if (!date.HasValue) return "";
            return date.Value.ToString("yyMMdd");
        }

        public static string SanitizeGs1Text(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var sb = new StringBuilder(raw.Length);
            foreach (var ch in raw.Trim())
            {
                if (ch == GroupSeparator) continue;
                if (ch >= 32 && ch <= 126 && ch != '(' && ch != ')')
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        public static string BuildElementString(string gtin, DateTime? manufacturingDate, string batch, DateTime? expiryDate, string serial)
        {
            var gtin14 = Gtin14(gtin);
            if (gtin14.Length != 14)
                throw new InvalidOperationException("A 8/12/13/14-digit GTIN is required to build a GS1 DataMatrix.");
            var serialClean = SanitizeGs1Text(serial);
            if (string.IsNullOrEmpty(serialClean))
                throw new InvalidOperationException("Serial number (hexacode) is required for GS1 AI (21).");

            var mfg = YyMmDd(manufacturingDate);
            var exp = YyMmDd(expiryDate);
            var lot = SanitizeGs1Text(batch);

            var sb = new StringBuilder();
            sb.Append("01").Append(gtin14);
            if (mfg.Length == 6) sb.Append("11").Append(mfg);
            if (!string.IsNullOrEmpty(lot))
            {
                sb.Append("10").Append(lot);
                if (exp.Length == 6 || serialClean.Length > 0)
                    sb.Append(GroupSeparator);
            }
            if (exp.Length == 6) sb.Append("17").Append(exp);
            sb.Append("21").Append(serialClean);
            return sb.ToString();
        }

        public static string BuildHumanReadable(string gtin, DateTime? manufacturingDate, string batch, DateTime? expiryDate, string serial)
        {
            var gtin14 = Gtin14(gtin);
            var lines = new List<string>();
            if (gtin14.Length == 14) lines.Add("(01)" + gtin14);
            var mfg = YyMmDd(manufacturingDate);
            if (mfg.Length == 6) lines.Add("(11)" + mfg);
            var lot = SanitizeGs1Text(batch);
            if (!string.IsNullOrEmpty(lot)) lines.Add("(10)" + lot);
            var exp = YyMmDd(expiryDate);
            if (exp.Length == 6) lines.Add("(17)" + exp);
            var serialClean = SanitizeGs1Text(serial);
            if (!string.IsNullOrEmpty(serialClean)) lines.Add("(21)" + serialClean);
            return string.Join("\n", lines);
        }

        public static byte[] RenderPng(string elementString, int modulePx = 8, bool withHri = true, string hri = null)
        {
            if (string.IsNullOrEmpty(elementString))
                throw new InvalidOperationException("GS1 element string is empty.");
            if (modulePx < 4) modulePx = 4;
            if (modulePx > 16) modulePx = 16;

            var matrix = DataMatrixEcc200.EncodeGs1(elementString);
            const int quiet = 4;
            var dim = matrix.GetLength(0);
            var size = (dim + quiet * 2) * modulePx;
            var hriLines = withHri
                ? (hri ?? "").Replace("\r\n", "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                : new string[0];
            var fontHeight = Math.Max(16, modulePx * 2);
            var textBlock = hriLines.Length == 0 ? 0 : 12 + hriLines.Length * fontHeight;

            using (var bmp = new Bitmap(size, size + textBlock))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.Clear(Color.White);
                for (var y = 0; y < dim; y++)
                {
                    for (var x = 0; x < dim; x++)
                    {
                        if (!matrix[y, x]) continue;
                        g.FillRectangle(Brushes.Black,
                            (x + quiet) * modulePx,
                            (y + quiet) * modulePx,
                            modulePx, modulePx);
                    }
                }
                if (hriLines.Length > 0)
                {
                    using (var font = new Font("Consolas", Math.Max(9f, modulePx * 1.1f), FontStyle.Regular, GraphicsUnit.Point))
                    {
                        var y = size + 6;
                        foreach (var line in hriLines)
                        {
                            g.DrawString(line, font, Brushes.Black, 8, y);
                            y += fontHeight;
                        }
                    }
                }
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
    }

    /// <summary>ISO/IEC 16022 ECC200 encoder aligned with ZXing (Apache 2.0).</summary>
    internal static class DataMatrixEcc200
    {
        private const int Fnc1Codeword = 232;

        private static readonly int[] FactorSets = { 5, 7, 10, 11, 12, 14, 18, 20, 24, 28, 36, 42, 48, 56, 62, 68 };
        private static readonly int[][] Factors =
        {
            new[] {228, 48, 15, 111, 62},
            new[] {23, 68, 144, 134, 240, 92, 254},
            new[] {28, 24, 185, 166, 223, 248, 116, 255, 110, 61},
            new[] {175, 138, 205, 12, 194, 168, 39, 245, 60, 97, 120},
            new[] {41, 153, 158, 91, 61, 42, 142, 213, 97, 178, 100, 242},
            new[] {156, 97, 192, 252, 95, 9, 157, 119, 138, 45, 18, 186, 83, 185},
            new[] {83, 195, 100, 39, 188, 75, 66, 61, 241, 213, 109, 129, 94, 254, 225, 48, 90, 188},
            new[] {15, 195, 244, 9, 233, 71, 168, 2, 188, 160, 153, 145, 253, 79, 108, 82, 27, 174, 186, 172},
            new[] {52, 190, 88, 205, 109, 39, 176, 21, 155, 197, 251, 223, 155, 21, 5, 172, 254, 124, 12, 181, 184, 96, 50, 193},
            new[] {211, 231, 43, 97, 71, 96, 103, 174, 37, 151, 170, 53, 75, 34, 249, 121, 17, 138, 110, 213, 141, 136, 120, 151, 233, 168, 93, 255},
            new[] {245, 127, 242, 218, 130, 250, 162, 181, 102, 120, 84, 179, 220, 251, 80, 182, 229, 18, 2, 4, 68, 33, 101, 137, 95, 119, 115, 44, 175, 184, 59, 25, 225, 98, 81, 112},
            new[] {77, 193, 137, 31, 19, 38, 22, 153, 247, 105, 122, 2, 245, 133, 242, 8, 175, 95, 100, 9, 167, 105, 214, 111, 57, 121, 21, 1, 253, 57, 54, 101, 248, 202, 69, 50, 150, 177, 226, 5, 9, 5},
            new[] {245, 132, 172, 223, 96, 32, 117, 22, 238, 133, 238, 231, 205, 188, 237, 87, 191, 106, 16, 147, 118, 23, 37, 90, 170, 205, 131, 88, 120, 100, 66, 138, 186, 240, 82, 44, 176, 87, 187, 147, 160, 175, 69, 213, 92, 253, 225, 19},
            new[] {175, 9, 223, 238, 12, 17, 220, 208, 100, 29, 175, 170, 230, 192, 215, 235, 150, 159, 36, 223, 38, 200, 132, 54, 228, 146, 218, 234, 117, 203, 29, 232, 144, 238, 22, 150, 201, 117, 62, 207, 164, 13, 137, 245, 127, 67, 247, 28, 155, 43, 203, 107, 233, 53, 143, 46},
            new[] {242, 93, 169, 50, 144, 210, 39, 118, 202, 188, 201, 189, 143, 108, 196, 37, 185, 112, 134, 230, 245, 63, 197, 190, 250, 106, 185, 221, 175, 64, 114, 71, 161, 44, 147, 6, 27, 218, 51, 63, 87, 10, 40, 130, 188, 17, 163, 31, 176, 170, 4, 107, 232, 7, 94, 166, 224, 124, 86, 47, 11, 204},
            new[] {220, 228, 173, 89, 251, 149, 159, 56, 89, 33, 147, 244, 154, 36, 73, 127, 213, 136, 248, 180, 234, 197, 158, 177, 68, 122, 93, 213, 15, 160, 227, 236, 66, 139, 153, 185, 202, 167, 179, 25, 220, 232, 96, 210, 231, 136, 223, 239, 181, 241, 59, 52, 172, 25, 49, 232, 211, 189, 64, 54, 108, 153, 132, 63, 96, 103, 82, 186}
        };

        private static readonly int[] Log = new int[256];
        private static readonly int[] Alog = new int[255];

        static DataMatrixEcc200()
        {
            var p = 1;
            for (var i = 0; i < 255; i++)
            {
                Alog[i] = p;
                Log[p] = i;
                p *= 2;
                if (p >= 256) p ^= 0x12D;
            }
        }

        public static bool[,] EncodeGs1(string elementString)
        {
            var data = EncodeAsciiGs1(elementString);
            var symbol = LookupSquare(data.Count);
            var padded = Pad(data, symbol.DataCapacity);
            var codewords = EncodeEcc200(padded, symbol);
            var placement = new int[symbol.SymbolDataWidth * symbol.SymbolDataHeight];
            for (var i = 0; i < placement.Length; i++) placement[i] = -1;
            Place(codewords, symbol.SymbolDataWidth, symbol.SymbolDataHeight, placement);
            return EncodeLowLevel(placement, symbol);
        }

        private static List<int> EncodeAsciiGs1(string text)
        {
            var result = new List<int> { Fnc1Codeword };
            var i = 0;
            while (i < text.Length)
            {
                var ch = text[i];
                if (ch == Gs1DataMatrix.GroupSeparator)
                {
                    result.Add(Fnc1Codeword);
                    i++;
                    continue;
                }
                if (ch >= '0' && ch <= '9' && i + 1 < text.Length && text[i + 1] >= '0' && text[i + 1] <= '9')
                {
                    result.Add(130 + (ch - '0') * 10 + (text[i + 1] - '0'));
                    i += 2;
                    continue;
                }
                if (ch > 127)
                    throw new InvalidOperationException("GS1 DataMatrix data contains an unsupported character.");
                result.Add(ch + 1);
                i++;
            }
            return result;
        }

        private static Symbol LookupSquare(int dataCount)
        {
            foreach (var s in SquareSymbols)
                if (dataCount <= s.DataCapacity) return s;
            throw new InvalidOperationException("GS1 DataMatrix data is too long for ECC200.");
        }

        private static int[] Pad(List<int> data, int capacity)
        {
            var result = new int[capacity];
            for (var i = 0; i < data.Count; i++) result[i] = data[i];
            if (data.Count >= capacity) return result;
            result[data.Count] = 129;
            for (var i = data.Count + 1; i < capacity; i++)
            {
                var temp = 129 + ((149 * (i + 1)) % 253) + 1;
                result[i] = temp <= 254 ? temp : temp - 254;
            }
            return result;
        }

        private static int[] EncodeEcc200(int[] data, Symbol symbol)
        {
            var blockCount = symbol.InterleavedBlockCount;
            var full = new int[symbol.DataCapacity + symbol.ErrorCodewords];
            Array.Copy(data, full, data.Length);
            if (blockCount == 1)
            {
                var ecc = CreateEccBlock(data, symbol.ErrorCodewords);
                Array.Copy(ecc, 0, full, symbol.DataCapacity, ecc.Length);
                return full;
            }
            for (var block = 0; block < blockCount; block++)
            {
                var temp = new List<int>();
                for (var d = block; d < symbol.DataCapacity; d += blockCount)
                    temp.Add(data[d]);
                var ecc = CreateEccBlock(temp.ToArray(), symbol.RsBlockError);
                var pos = 0;
                for (var e = block; e < symbol.RsBlockError * blockCount; e += blockCount)
                    full[symbol.DataCapacity + e] = ecc[pos++];
            }
            return full;
        }

        private static int[] CreateEccBlock(int[] codewords, int numEc)
        {
            var table = -1;
            for (var i = 0; i < FactorSets.Length; i++)
                if (FactorSets[i] == numEc) { table = i; break; }
            if (table < 0)
                throw new InvalidOperationException("Unsupported ECC length: " + numEc);
            var poly = Factors[table];
            var ecc = new int[numEc];
            for (var i = 0; i < codewords.Length; i++)
            {
                var m = ecc[numEc - 1] ^ codewords[i];
                for (var k = numEc - 1; k > 0; k--)
                {
                    if (m != 0 && poly[k] != 0)
                        ecc[k] = ecc[k - 1] ^ Alog[(Log[m] + Log[poly[k]]) % 255];
                    else
                        ecc[k] = ecc[k - 1];
                }
                if (m != 0 && poly[0] != 0)
                    ecc[0] = Alog[(Log[m] + Log[poly[0]]) % 255];
                else
                    ecc[0] = 0;
            }
            var reversed = new int[numEc];
            for (var i = 0; i < numEc; i++)
                reversed[i] = ecc[numEc - i - 1];
            return reversed;
        }

        private static void Place(int[] codewords, int numcols, int numrows, int[] bits)
        {
            var pos = 0;
            var row = 4;
            var col = 0;
            do
            {
                if (row == numrows && col == 0) Corner1(bits, numcols, numrows, codewords, pos++);
                if (row == numrows - 2 && col == 0 && numcols % 4 != 0) Corner2(bits, numcols, numrows, codewords, pos++);
                if (row == numrows - 2 && col == 0 && numcols % 8 == 4) Corner3(bits, numcols, numrows, codewords, pos++);
                if (row == numrows + 4 && col == 2 && numcols % 8 == 0) Corner4(bits, numcols, numrows, codewords, pos++);
                do
                {
                    if (row < numrows && col >= 0 && NoBit(bits, numcols, col, row))
                        Utah(bits, numcols, numrows, row, col, codewords, pos++);
                    row -= 2;
                    col += 2;
                } while (row >= 0 && col < numcols);
                row++;
                col += 3;
                do
                {
                    if (row >= 0 && col < numcols && NoBit(bits, numcols, col, row))
                        Utah(bits, numcols, numrows, row, col, codewords, pos++);
                    row += 2;
                    col -= 2;
                } while (row < numrows && col >= 0);
                row += 3;
                col++;
            } while (row < numrows || col < numcols);

            if (NoBit(bits, numcols, numcols - 1, numrows - 1))
            {
                SetBit(bits, numcols, numcols - 1, numrows - 1, true);
                SetBit(bits, numcols, numcols - 2, numrows - 2, true);
            }
        }

        private static bool[,] EncodeLowLevel(int[] placement, Symbol symbol)
        {
            var symbolWidth = symbol.SymbolDataWidth;
            var symbolHeight = symbol.SymbolDataHeight;
            var matrixW = symbol.SymbolWidth;
            var matrixH = symbol.SymbolHeight;
            var matrix = new bool[matrixH, matrixW];
            var matrixY = 0;
            for (var y = 0; y < symbolHeight; y++)
            {
                int matrixX;
                if (y % symbol.MatrixHeight == 0)
                {
                    matrixX = 0;
                    for (var x = 0; x < matrixW; x++)
                    {
                        matrix[matrixY, matrixX] = (x % 2) == 0;
                        matrixX++;
                    }
                    matrixY++;
                }
                matrixX = 0;
                for (var x = 0; x < symbolWidth; x++)
                {
                    if (x % symbol.MatrixWidth == 0)
                    {
                        matrix[matrixY, matrixX] = true;
                        matrixX++;
                    }
                    matrix[matrixY, matrixX] = GetBit(placement, symbolWidth, x, y);
                    matrixX++;
                    if (x % symbol.MatrixWidth == symbol.MatrixWidth - 1)
                    {
                        matrix[matrixY, matrixX] = (y % 2) == 0;
                        matrixX++;
                    }
                }
                matrixY++;
                if (y % symbol.MatrixHeight == symbol.MatrixHeight - 1)
                {
                    matrixX = 0;
                    for (var x = 0; x < matrixW; x++)
                    {
                        matrix[matrixY, matrixX] = true;
                        matrixX++;
                    }
                    matrixY++;
                }
            }
            return matrix;
        }

        private static void Utah(int[] bits, int numcols, int numrows, int row, int col, int[] cw, int pos)
        {
            Module(bits, numcols, numrows, row - 2, col - 2, cw, pos, 1);
            Module(bits, numcols, numrows, row - 2, col - 1, cw, pos, 2);
            Module(bits, numcols, numrows, row - 1, col - 2, cw, pos, 3);
            Module(bits, numcols, numrows, row - 1, col - 1, cw, pos, 4);
            Module(bits, numcols, numrows, row - 1, col, cw, pos, 5);
            Module(bits, numcols, numrows, row, col - 2, cw, pos, 6);
            Module(bits, numcols, numrows, row, col - 1, cw, pos, 7);
            Module(bits, numcols, numrows, row, col, cw, pos, 8);
        }

        private static void Corner1(int[] bits, int numcols, int numrows, int[] cw, int pos)
        {
            Module(bits, numcols, numrows, numrows - 1, 0, cw, pos, 1);
            Module(bits, numcols, numrows, numrows - 1, 1, cw, pos, 2);
            Module(bits, numcols, numrows, numrows - 1, 2, cw, pos, 3);
            Module(bits, numcols, numrows, 0, numcols - 2, cw, pos, 4);
            Module(bits, numcols, numrows, 0, numcols - 1, cw, pos, 5);
            Module(bits, numcols, numrows, 1, numcols - 1, cw, pos, 6);
            Module(bits, numcols, numrows, 2, numcols - 1, cw, pos, 7);
            Module(bits, numcols, numrows, 3, numcols - 1, cw, pos, 8);
        }

        private static void Corner2(int[] bits, int numcols, int numrows, int[] cw, int pos)
        {
            Module(bits, numcols, numrows, numrows - 3, 0, cw, pos, 1);
            Module(bits, numcols, numrows, numrows - 2, 0, cw, pos, 2);
            Module(bits, numcols, numrows, numrows - 1, 0, cw, pos, 3);
            Module(bits, numcols, numrows, 0, numcols - 4, cw, pos, 4);
            Module(bits, numcols, numrows, 0, numcols - 3, cw, pos, 5);
            Module(bits, numcols, numrows, 0, numcols - 2, cw, pos, 6);
            Module(bits, numcols, numrows, 0, numcols - 1, cw, pos, 7);
            Module(bits, numcols, numrows, 1, numcols - 1, cw, pos, 8);
        }

        private static void Corner3(int[] bits, int numcols, int numrows, int[] cw, int pos)
        {
            Module(bits, numcols, numrows, numrows - 3, 0, cw, pos, 1);
            Module(bits, numcols, numrows, numrows - 2, 0, cw, pos, 2);
            Module(bits, numcols, numrows, numrows - 1, 0, cw, pos, 3);
            Module(bits, numcols, numrows, 0, numcols - 2, cw, pos, 4);
            Module(bits, numcols, numrows, 0, numcols - 1, cw, pos, 5);
            Module(bits, numcols, numrows, 1, numcols - 1, cw, pos, 6);
            Module(bits, numcols, numrows, 2, numcols - 1, cw, pos, 7);
            Module(bits, numcols, numrows, 3, numcols - 1, cw, pos, 8);
        }

        private static void Corner4(int[] bits, int numcols, int numrows, int[] cw, int pos)
        {
            Module(bits, numcols, numrows, numrows - 1, 0, cw, pos, 1);
            Module(bits, numcols, numrows, numrows - 1, numcols - 1, cw, pos, 2);
            Module(bits, numcols, numrows, 0, numcols - 3, cw, pos, 3);
            Module(bits, numcols, numrows, 0, numcols - 2, cw, pos, 4);
            Module(bits, numcols, numrows, 0, numcols - 1, cw, pos, 5);
            Module(bits, numcols, numrows, 1, numcols - 3, cw, pos, 6);
            Module(bits, numcols, numrows, 1, numcols - 2, cw, pos, 7);
            Module(bits, numcols, numrows, 1, numcols - 1, cw, pos, 8);
        }

        private static void Module(int[] bits, int numcols, int numrows, int row, int col, int[] cw, int pos, int bit)
        {
            if (row < 0)
            {
                row += numrows;
                col += 4 - ((numrows + 4) % 8);
            }
            if (col < 0)
            {
                col += numcols;
                row += 4 - ((numcols + 4) % 8);
            }
            var v = cw[pos] & (1 << (8 - bit));
            SetBit(bits, numcols, col, row, v != 0);
        }

        private static bool NoBit(int[] bits, int numcols, int col, int row) => bits[row * numcols + col] < 0;
        private static bool GetBit(int[] bits, int numcols, int col, int row) => bits[row * numcols + col] == 1;
        private static void SetBit(int[] bits, int numcols, int col, int row, bool on) => bits[row * numcols + col] = on ? 1 : 0;

        // Square ECC200 symbols used by GS1 (ZXing SymbolInfo).
        private static readonly Symbol[] SquareSymbols =
        {
            new Symbol(3, 5, 8, 8, 1, 3, 5),
            new Symbol(5, 7, 10, 10, 1, 5, 7),
            new Symbol(8, 10, 12, 12, 1, 8, 10),
            new Symbol(12, 12, 14, 14, 1, 12, 12),
            new Symbol(18, 14, 16, 16, 1, 18, 14),
            new Symbol(22, 18, 18, 18, 1, 22, 18),
            new Symbol(30, 20, 20, 20, 1, 30, 20),
            new Symbol(36, 24, 22, 22, 1, 36, 24),
            new Symbol(44, 28, 24, 24, 1, 44, 28),
            new Symbol(62, 36, 14, 14, 4, 62, 36),
            new Symbol(86, 42, 16, 16, 4, 86, 42),
            new Symbol(114, 48, 18, 18, 4, 114, 48),
            new Symbol(144, 56, 20, 20, 4, 144, 56),
            new Symbol(174, 68, 22, 22, 4, 174, 68),
            new Symbol(204, 84, 24, 24, 4, 102, 42),
            new Symbol(280, 112, 14, 14, 16, 140, 56)
        };

        private sealed class Symbol
        {
            public int DataCapacity;
            public int ErrorCodewords;
            public int MatrixWidth;
            public int MatrixHeight;
            public int DataRegions;
            public int RsBlockData;
            public int RsBlockError;
            public int InterleavedBlockCount => DataCapacity / RsBlockData;
            public int HorizontalRegions => DataRegions == 1 ? 1 : DataRegions == 4 ? 2 : DataRegions == 16 ? 4 : 1;
            public int VerticalRegions => DataRegions == 1 ? 1 : DataRegions == 4 ? 2 : DataRegions == 16 ? 4 : 1;
            public int SymbolDataWidth => HorizontalRegions * MatrixWidth;
            public int SymbolDataHeight => VerticalRegions * MatrixHeight;
            public int SymbolWidth => SymbolDataWidth + HorizontalRegions * 2;
            public int SymbolHeight => SymbolDataHeight + VerticalRegions * 2;

            public Symbol(int data, int error, int mw, int mh, int regions, int rsData, int rsError)
            {
                DataCapacity = data;
                ErrorCodewords = error;
                MatrixWidth = mw;
                MatrixHeight = mh;
                DataRegions = regions;
                RsBlockData = rsData;
                RsBlockError = rsError;
            }
        }
    }
}
