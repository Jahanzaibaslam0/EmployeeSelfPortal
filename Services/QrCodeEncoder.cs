using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace HRMS.Services
{
    /// <summary>ISO/IEC 18004 QR Code (Model 2), byte mode, error correction M.</summary>
    public static class QrCodeEncoder
    {
        public static byte[] RenderPng(string payload, int modulePx = 4)
        {
            if (modulePx < 2) modulePx = 2;
            if (modulePx > 12) modulePx = 12;
            var matrix = Encode(payload ?? "");
            var dim = matrix.GetLength(0);
            const int quiet = 4;
            var size = (dim + quiet * 2) * modulePx;
            using (var bmp = new Bitmap(size, size))
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
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }

        public static bool[,] Encode(string text)
        {
            var data = Encoding.UTF8.GetBytes(text ?? "");
            var version = ChooseVersion(data.Length);
            var size = SizeOf(version);
            var dataCodewords = EncodeData(data, version);
            var ecc = AddEccAndInterleave(dataCodewords, version);

            var modules = new bool[size, size];
            var isFunction = new bool[size, size];
            DrawFunctionPatterns(modules, isFunction, version);
            PlaceData(modules, isFunction, ecc);
            var mask = ChooseMask(modules, isFunction, version);
            ApplyMask(modules, isFunction, mask);
            DrawFormatBits(modules, isFunction, mask);
            if (version >= 7)
                DrawVersionBits(modules, isFunction, version);
            return modules;
        }

        private static int SizeOf(int version) => 21 + 4 * (version - 1);

        private static readonly int[] DataCodewordsM =
        {
            0,
            16, 28, 44, 64, 86, 108, 124, 154, 182, 216,
            254, 290, 334, 365, 415, 453, 507, 563, 627, 669
        };

        private static readonly int[] EccPerBlockM =
        {
            0,
            10, 16, 26, 18, 24, 16, 18, 22, 22, 26,
            30, 22, 22, 24, 24, 28, 28, 26, 26, 26
        };

        private static readonly int[] NumBlocksM =
        {
            0,
            1, 1, 1, 2, 2, 4, 4, 4, 5, 5,
            5, 8, 9, 9, 10, 10, 11, 13, 14, 16
        };

        private static readonly int[][] AlignmentCenters =
        {
            null,
            new int[0],
            new[] { 6, 18 },
            new[] { 6, 22 },
            new[] { 6, 26 },
            new[] { 6, 30 },
            new[] { 6, 34 },
            new[] { 6, 22, 38 },
            new[] { 6, 24, 42 },
            new[] { 6, 26, 46 },
            new[] { 6, 28, 50 },
            new[] { 6, 30, 54 },
            new[] { 6, 32, 58 },
            new[] { 6, 34, 62 },
            new[] { 6, 26, 46, 66 },
            new[] { 6, 26, 48, 70 },
            new[] { 6, 26, 50, 74 },
            new[] { 6, 30, 54, 78 },
            new[] { 6, 30, 56, 82 },
            new[] { 6, 30, 58, 86 },
            new[] { 6, 34, 62, 90 }
        };

        private static int ChooseVersion(int byteCount)
        {
            for (var v = 1; v <= 20; v++)
            {
                var countBits = v <= 9 ? 8 : 16;
                var capacityBits = DataCodewordsM[v] * 8;
                var needed = 4 + countBits + byteCount * 8 + 4;
                if (needed <= capacityBits) return v;
            }
            throw new InvalidOperationException("Contact QR payload is too large to encode.");
        }

        private static byte[] EncodeData(byte[] data, int version)
        {
            var countBits = version <= 9 ? 8 : 16;
            var bits = new BitBuffer();
            bits.Append(0x4, 4);
            bits.Append(data.Length, countBits);
            foreach (var b in data)
                bits.Append(b, 8);
            var capacity = DataCodewordsM[version] * 8;
            var terminator = Math.Min(4, capacity - bits.Length);
            bits.Append(0, terminator);
            while (bits.Length % 8 != 0)
                bits.Append(0, 1);
            var pad = 0xEC;
            while (bits.Length < capacity)
            {
                bits.Append(pad, 8);
                pad = pad == 0xEC ? 0x11 : 0xEC;
            }
            var result = new byte[DataCodewordsM[version]];
            for (var i = 0; i < result.Length; i++)
            {
                var v = 0;
                for (var j = 0; j < 8; j++)
                    v = (v << 1) | bits.Get(i * 8 + j);
                result[i] = (byte)v;
            }
            return result;
        }

        private static byte[] AddEccAndInterleave(byte[] data, int version)
        {
            var numBlocks = NumBlocksM[version];
            var eccLen = EccPerBlockM[version];
            var blockDataLen = DataCodewordsM[version] / numBlocks;
            var shortBlocks = numBlocks - DataCodewordsM[version] % numBlocks;
            var generator = RsDivisor(eccLen);
            var blocks = new List<byte[]>();
            var eccBlocks = new List<byte[]>();
            var offset = 0;
            for (var i = 0; i < numBlocks; i++)
            {
                var len = blockDataLen + (i < shortBlocks ? 0 : 1);
                var block = new byte[len];
                Array.Copy(data, offset, block, 0, len);
                offset += len;
                blocks.Add(block);
                eccBlocks.Add(RsRemainder(block, generator));
            }
            var result = new byte[data.Length + numBlocks * eccLen];
            var pos = 0;
            var maxData = blockDataLen + 1;
            for (var i = 0; i < maxData; i++)
            {
                for (var b = 0; b < numBlocks; b++)
                {
                    if (i < blocks[b].Length)
                        result[pos++] = blocks[b][i];
                }
            }
            for (var i = 0; i < eccLen; i++)
            {
                for (var b = 0; b < numBlocks; b++)
                    result[pos++] = eccBlocks[b][i];
            }
            return result;
        }

        private static void DrawFunctionPatterns(bool[,] modules, bool[,] isFunction, int version)
        {
            var size = modules.GetLength(0);
            DrawFinder(modules, isFunction, 0, 0);
            DrawFinder(modules, isFunction, size - 7, 0);
            DrawFinder(modules, isFunction, 0, size - 7);
            for (var i = 0; i < 8; i++)
            {
                SetFunction(modules, isFunction, 7, i, false);
                SetFunction(modules, isFunction, i, 7, false);
                SetFunction(modules, isFunction, size - 8, i, false);
                SetFunction(modules, isFunction, size - 1 - i, 7, false);
                SetFunction(modules, isFunction, 7, size - 1 - i, false);
                SetFunction(modules, isFunction, i, size - 8, false);
            }
            for (var i = 8; i < size - 8; i++)
            {
                SetFunction(modules, isFunction, 6, i, i % 2 == 0);
                SetFunction(modules, isFunction, i, 6, i % 2 == 0);
            }
            foreach (var y in AlignmentCenters[version])
            {
                foreach (var x in AlignmentCenters[version])
                {
                    if (OverlapsFinder(x, y, size)) continue;
                    DrawAlignment(modules, isFunction, x, y);
                }
            }
            SetFunction(modules, isFunction, 8, size - 8, true);
            for (var i = 0; i < 9; i++)
            {
                MarkFunction(isFunction, 8, i);
                MarkFunction(isFunction, i, 8);
            }
            for (var i = 0; i < 8; i++)
            {
                MarkFunction(isFunction, 8, size - 1 - i);
                MarkFunction(isFunction, size - 1 - i, 8);
            }
            if (version >= 7)
            {
                for (var i = 0; i < 6; i++)
                {
                    for (var j = 0; j < 3; j++)
                    {
                        MarkFunction(isFunction, i, size - 11 + j);
                        MarkFunction(isFunction, size - 11 + j, i);
                    }
                }
            }
        }

        private static bool OverlapsFinder(int cx, int cy, int size)
        {
            if (cx == 6 && cy == 6) return true;
            if (cx == 6 && cy == size - 7) return true;
            if (cx == size - 7 && cy == 6) return true;
            return false;
        }

        private static void DrawFinder(bool[,] modules, bool[,] isFunction, int left, int top)
        {
            for (var dy = -1; dy <= 7; dy++)
            {
                for (var dx = -1; dx <= 7; dx++)
                {
                    var x = left + dx;
                    var y = top + dy;
                    if (x < 0 || y < 0 || x >= modules.GetLength(0) || y >= modules.GetLength(0))
                        continue;
                    var on = (dx >= 0 && dx <= 6 && dy >= 0 && dy <= 6)
                             && (dx == 0 || dx == 6 || dy == 0 || dy == 6 || (dx >= 2 && dx <= 4 && dy >= 2 && dy <= 4));
                    SetFunction(modules, isFunction, x, y, on);
                }
            }
        }

        private static void DrawAlignment(bool[,] modules, bool[,] isFunction, int cx, int cy)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                for (var dx = -2; dx <= 2; dx++)
                {
                    var on = Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1;
                    SetFunction(modules, isFunction, cx + dx, cy + dy, on);
                }
            }
        }

        private static void PlaceData(bool[,] modules, bool[,] isFunction, byte[] data)
        {
            var size = modules.GetLength(0);
            var bitIndex = 0;
            var totalBits = data.Length * 8;
            for (var right = size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right = 5;
                for (var vert = 0; vert < size; vert++)
                {
                    for (var j = 0; j < 2; j++)
                    {
                        var x = right - j;
                        var upward = ((right + 1) & 2) == 0;
                        var y = upward ? size - 1 - vert : vert;
                        if (isFunction[y, x] || bitIndex >= totalBits) continue;
                        var bit = ((data[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) == 1;
                        modules[y, x] = bit;
                        bitIndex++;
                    }
                }
            }
        }

        private static int ChooseMask(bool[,] modules, bool[,] isFunction, int version)
        {
            var size = modules.GetLength(0);
            var bestMask = 0;
            var bestScore = int.MaxValue;
            for (var mask = 0; mask < 8; mask++)
            {
                ApplyMask(modules, isFunction, mask);
                DrawFormatBits(modules, isFunction, mask);
                if (version >= 7) DrawVersionBits(modules, isFunction, version);
                var score = Penalty(modules);
                ApplyMask(modules, isFunction, mask);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestMask = mask;
                }
            }
            return bestMask;
        }

        private static void ApplyMask(bool[,] modules, bool[,] isFunction, int mask)
        {
            var size = modules.GetLength(0);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    if (isFunction[y, x]) continue;
                    var invert = false;
                    switch (mask)
                    {
                        case 0: invert = (x + y) % 2 == 0; break;
                        case 1: invert = y % 2 == 0; break;
                        case 2: invert = x % 3 == 0; break;
                        case 3: invert = (x + y) % 3 == 0; break;
                        case 4: invert = (x / 3 + y / 2) % 2 == 0; break;
                        case 5: invert = x * y % 2 + x * y % 3 == 0; break;
                        case 6: invert = (x * y % 2 + x * y % 3) % 2 == 0; break;
                        case 7: invert = ((x + y) % 2 + x * y % 3) % 2 == 0; break;
                    }
                    if (invert) modules[y, x] = !modules[y, x];
                }
            }
        }

        private static void DrawFormatBits(bool[,] modules, bool[,] isFunction, int mask)
        {
            var data = (0 << 3) | mask;
            var rem = data;
            for (var i = 0; i < 10; i++)
                rem = (rem << 1) ^ (((rem >> 9) & 1) * 0x537);
            var bits = ((data << 10) | rem) ^ 0x5412;
            var size = modules.GetLength(0);
            for (var i = 0; i <= 5; i++)
                SetFunction(modules, isFunction, 8, i, GetBit(bits, i));
            SetFunction(modules, isFunction, 8, 7, GetBit(bits, 6));
            SetFunction(modules, isFunction, 8, 8, GetBit(bits, 7));
            SetFunction(modules, isFunction, 7, 8, GetBit(bits, 8));
            for (var i = 9; i < 15; i++)
                SetFunction(modules, isFunction, 14 - i, 8, GetBit(bits, i));
            for (var i = 0; i < 8; i++)
                SetFunction(modules, isFunction, size - 1 - i, 8, GetBit(bits, i));
            for (var i = 8; i < 15; i++)
                SetFunction(modules, isFunction, 8, size - 15 + i, GetBit(bits, i));
        }

        private static void DrawVersionBits(bool[,] modules, bool[,] isFunction, int version)
        {
            var rem = version;
            for (var i = 0; i < 12; i++)
                remnantShift(ref rem);
            var bits = (version << 12) | rem;
            var size = modules.GetLength(0);
            for (var i = 0; i < 18; i++)
            {
                var bit = GetBit(bits, i);
                var a = size - 11 + i % 3;
                var b = i / 3;
                SetFunction(modules, isFunction, a, b, bit);
                SetFunction(modules, isFunction, b, a, bit);
            }
        }

        private static void remnantShift(ref int rem)
        {
            rem = (rem << 1) ^ (((rem >> 11) & 1) * 0x1F25);
        }

        private static int Penalty(bool[,] modules)
        {
            var size = modules.GetLength(0);
            var score = 0;
            for (var y = 0; y < size; y++)
            {
                var run = 0;
                var color = false;
                for (var x = 0; x < size; x++)
                {
                    if (x == 0 || modules[y, x] != color)
                    {
                        color = modules[y, x];
                        run = 1;
                    }
                    else
                    {
                        run++;
                        if (run == 5) score += 3;
                        else if (run > 5) score++;
                    }
                }
            }
            for (var x = 0; x < size; x++)
            {
                var run = 0;
                var color = false;
                for (var y = 0; y < size; y++)
                {
                    if (y == 0 || modules[y, x] != color)
                    {
                        color = modules[y, x];
                        run = 1;
                    }
                    else
                    {
                        run++;
                        if (run == 5) score += 3;
                        else if (run > 5) score++;
                    }
                }
            }
            for (var y = 0; y < size - 1; y++)
            {
                for (var x = 0; x < size - 1; x++)
                {
                    var v = modules[y, x];
                    if (v == modules[y, x + 1] && v == modules[y + 1, x] && v == modules[y + 1, x + 1])
                        score += 3;
                }
            }
            score += FinderPenalty(modules, true) + FinderPenalty(modules, false);
            var dark = 0;
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                    if (modules[y, x]) dark++;
            var k = (Math.Abs(dark * 20 - size * size * 10) + size * size - 1) / (size * size) - 1;
            score += k * 10;
            return score;
        }

        private static int FinderPenalty(bool[,] modules, bool horizontal)
        {
            var size = modules.GetLength(0);
            var score = 0;
            for (var i = 0; i < size; i++)
            {
                var runHistory = new int[7];
                var n = 0;
                var color = false;
                for (var j = 0; j <= size; j++)
                {
                    var on = j < size && (horizontal ? modules[i, j] : modules[j, i]);
                    if (j == 0)
                    {
                        color = on;
                        n = 1;
                        continue;
                    }
                    if (j < size && on == color)
                    {
                        n++;
                        continue;
                    }
                    Array.Copy(runHistory, 0, runHistory, 1, 6);
                    runHistory[0] = n;
                    color = on;
                    n = 1;
                    if (runHistory[1] == 1 && runHistory[2] == 1 && runHistory[3] == 3 * Math.Max(runHistory[1], 1)
                        && runHistory[4] == 1 && runHistory[5] == 1)
                    {
                        // handled below with ratio 1:1:3:1:1
                    }
                    if (HasFinderRatio(runHistory))
                        score += 40;
                }
            }
            return score;
        }

        private static bool HasFinderRatio(int[] run)
        {
            var n = run[1];
            if (n == 0) return false;
            return run[1] == n && run[2] == n && run[3] == 3 * n && run[4] == n && run[5] == n;
        }

        private static void SetFunction(bool[,] modules, bool[,] isFunction, int x, int y, bool dark)
        {
            modules[y, x] = dark;
            isFunction[y, x] = true;
        }

        private static void MarkFunction(bool[,] isFunction, int x, int y)
        {
            isFunction[y, x] = true;
        }

        private static bool GetBit(int bits, int i) => ((bits >> i) & 1) != 0;

        private static byte[] RsDivisor(int degree)
        {
            var result = new byte[degree];
            result[degree - 1] = 1;
            var root = 1;
            for (var i = 0; i < degree; i++)
            {
                for (var j = 0; j < result.Length; j++)
                {
                    result[j] = (byte)GfMul(result[j] & 0xFF, root);
                    if (j + 1 < result.Length)
                        result[j] ^= result[j + 1];
                }
                root = GfMul(root, 2);
            }
            return result;
        }

        private static byte[] RsRemainder(byte[] data, byte[] divisor)
        {
            var result = new byte[divisor.Length];
            foreach (var b in data)
            {
                var factor = (b ^ result[0]) & 0xFF;
                Array.Copy(result, 1, result, 0, result.Length - 1);
                result[result.Length - 1] = 0;
                if (factor == 0) continue;
                for (var i = 0; i < result.Length; i++)
                    result[i] ^= (byte)GfMul(divisor[i] & 0xFF, factor);
            }
            return result;
        }

        private static int GfMul(int x, int y)
        {
            var z = 0;
            for (var i = 7; i >= 0; i--)
            {
                z = ((z << 1) ^ (((z >> 7) & 1) * 0x11D)) & 0xFF;
                z ^= ((y >> i) & 1) * x;
                z &= 0xFF;
            }
            return z;
        }

        private sealed class BitBuffer
        {
            private readonly List<int> _bits = new List<int>();
            public int Length => _bits.Count;
            public void Append(int value, int count)
            {
                for (var i = count - 1; i >= 0; i--)
                    _bits.Add((value >> i) & 1);
            }
            public int Get(int index) => _bits[index];
        }
    }
}
