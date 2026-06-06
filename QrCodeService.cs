using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;

namespace MunicipalTreasuryLedger
{
    public static class QrCodeService
    {
        private const int Version = 4;
        private const int Size = 33;
        private const int DataCodewords = 80;
        private const int ErrorCorrectionCodewords = 20;

        public static string BuildReceiptPayload(PrintableReportService.PaymentReceipt receipt)
        {
            if (receipt == null)
            {
                return "";
            }

            string amountCents = Decimal.Round(receipt.Amount * 100m, 0).ToString("0");
            string baseText = Safe(receipt.OrNumber) + "|" +
                receipt.DatePaid.ToString("yyyyMMdd") + "|" +
                amountCents + "|" +
                receipt.AssessmentYear.ToString() + "|" +
                Safe(receipt.BusinessName);

            return "MTO|" +
                Shorten(Safe(receipt.OrNumber), 18) + "|" +
                receipt.DatePaid.ToString("yyyyMMdd") + "|" +
                amountCents + "|" +
                VerificationCode(baseText);
        }

        public static string VerificationCode(PrintableReportService.PaymentReceipt receipt)
        {
            if (receipt == null)
            {
                return "";
            }

            string amountCents = Decimal.Round(receipt.Amount * 100m, 0).ToString("0");
            string baseText = Safe(receipt.OrNumber) + "|" +
                receipt.DatePaid.ToString("yyyyMMdd") + "|" +
                amountCents + "|" +
                receipt.AssessmentYear.ToString() + "|" +
                Safe(receipt.BusinessName);
            return VerificationCode(baseText);
        }

        public static Bitmap RenderQrCode(string text, int pixelsPerModule)
        {
            if (pixelsPerModule < 2)
            {
                pixelsPerModule = 2;
            }

            int[,] modules = GenerateModules(text ?? "");
            int quietZone = 4;
            int imageSize = (Size + quietZone * 2) * pixelsPerModule;
            Bitmap bitmap = new Bitmap(imageSize, imageSize);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (SolidBrush white = new SolidBrush(Color.White))
            using (SolidBrush black = new SolidBrush(Color.Black))
            {
                graphics.FillRectangle(white, 0, 0, imageSize, imageSize);
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        if (modules[y, x] == 1)
                        {
                            graphics.FillRectangle(
                                black,
                                (x + quietZone) * pixelsPerModule,
                                (y + quietZone) * pixelsPerModule,
                                pixelsPerModule,
                                pixelsPerModule);
                        }
                    }
                }
            }

            return bitmap;
        }

        private static int[,] GenerateModules(string text)
        {
            byte[] dataCodewords = EncodeDataCodewords(text);
            byte[] ecc = ReedSolomon(dataCodewords, ErrorCorrectionCodewords);
            byte[] allCodewords = new byte[DataCodewords + ErrorCorrectionCodewords];
            Array.Copy(dataCodewords, allCodewords, dataCodewords.Length);
            Array.Copy(ecc, 0, allCodewords, dataCodewords.Length, ecc.Length);

            int[,] modules = new int[Size, Size];
            bool[,] reserved = new bool[Size, Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    modules[y, x] = -1;
                }
            }

            DrawFinder(modules, reserved, 0, 0);
            DrawFinder(modules, reserved, Size - 7, 0);
            DrawFinder(modules, reserved, 0, Size - 7);
            DrawTiming(modules, reserved);
            DrawAlignment(modules, reserved, 26, 26);
            SetReserved(modules, reserved, 8, 4 * Version + 9, 1);
            ReserveFormatAreas(reserved);
            PlaceData(modules, reserved, allCodewords, 0);
            DrawFormatBits(modules, reserved, 0);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (modules[y, x] < 0)
                    {
                        modules[y, x] = 0;
                    }
                }
            }

            return modules;
        }

        private static byte[] EncodeDataCodewords(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text ?? "");
            if (bytes.Length > 78)
            {
                throw new InvalidOperationException("QR receipt payload is too long.");
            }

            BitBuffer bits = new BitBuffer();
            bits.Append(0x4, 4);
            bits.Append(bytes.Length, 8);
            foreach (byte value in bytes)
            {
                bits.Append(value, 8);
            }

            int capacityBits = DataCodewords * 8;
            bits.Append(0, Math.Min(4, capacityBits - bits.Length));
            while (bits.Length % 8 != 0)
            {
                bits.Append(0, 1);
            }

            byte[] data = bits.ToBytes();
            byte[] result = new byte[DataCodewords];
            Array.Copy(data, result, data.Length);
            for (int i = data.Length; i < result.Length; i++)
            {
                result[i] = (byte)((i - data.Length) % 2 == 0 ? 0xEC : 0x11);
            }

            return result;
        }

        private static void DrawFinder(int[,] modules, bool[,] reserved, int x, int y)
        {
            for (int dy = -1; dy <= 7; dy++)
            {
                for (int dx = -1; dx <= 7; dx++)
                {
                    int xx = x + dx;
                    int yy = y + dy;
                    if (xx < 0 || yy < 0 || xx >= Size || yy >= Size)
                    {
                        continue;
                    }

                    bool dark = dx >= 0 && dx <= 6 && dy >= 0 && dy <= 6 &&
                        (dx == 0 || dx == 6 || dy == 0 || dy == 6 || (dx >= 2 && dx <= 4 && dy >= 2 && dy <= 4));
                    SetReserved(modules, reserved, xx, yy, dark ? 1 : 0);
                }
            }
        }

        private static void DrawTiming(int[,] modules, bool[,] reserved)
        {
            for (int i = 8; i < Size - 8; i++)
            {
                SetReserved(modules, reserved, i, 6, i % 2 == 0 ? 1 : 0);
                SetReserved(modules, reserved, 6, i, i % 2 == 0 ? 1 : 0);
            }
        }

        private static void DrawAlignment(int[,] modules, bool[,] reserved, int centerX, int centerY)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    bool dark = Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1;
                    SetReserved(modules, reserved, centerX + dx, centerY + dy, dark ? 1 : 0);
                }
            }
        }

        private static void ReserveFormatAreas(bool[,] reserved)
        {
            for (int i = 0; i <= 8; i++)
            {
                if (i != 6)
                {
                    reserved[8, i] = true;
                    reserved[i, 8] = true;
                }
            }

            for (int i = 0; i < 8; i++)
            {
                reserved[8, Size - 1 - i] = true;
                reserved[Size - 1 - i, 8] = true;
            }
        }

        private static void PlaceData(int[,] modules, bool[,] reserved, byte[] codewords, int mask)
        {
            int bitIndex = 0;
            int direction = -1;
            int row = Size - 1;

            for (int col = Size - 1; col > 0; col -= 2)
            {
                if (col == 6)
                {
                    col--;
                }

                while (row >= 0 && row < Size)
                {
                    for (int c = 0; c < 2; c++)
                    {
                        int x = col - c;
                        int y = row;
                        if (!reserved[y, x])
                        {
                            int bit = 0;
                            if (bitIndex < codewords.Length * 8)
                            {
                                bit = (codewords[bitIndex / 8] >> (7 - (bitIndex % 8))) & 1;
                            }

                            if (Mask(mask, x, y))
                            {
                                bit ^= 1;
                            }

                            modules[y, x] = bit;
                            bitIndex++;
                        }
                    }

                    row += direction;
                }

                row -= direction;
                direction = -direction;
            }
        }

        private static void DrawFormatBits(int[,] modules, bool[,] reserved, int mask)
        {
            int bits = FormatBits(mask);
            for (int i = 0; i < 15; i++)
            {
                int bit = (bits >> i) & 1;
                if (i < 6)
                {
                    SetReserved(modules, reserved, 8, i, bit);
                }
                else if (i == 6)
                {
                    SetReserved(modules, reserved, 8, 7, bit);
                }
                else if (i == 7)
                {
                    SetReserved(modules, reserved, 8, 8, bit);
                }
                else if (i == 8)
                {
                    SetReserved(modules, reserved, 7, 8, bit);
                }
                else
                {
                    SetReserved(modules, reserved, 14 - i, 8, bit);
                }

                if (i < 8)
                {
                    SetReserved(modules, reserved, Size - 1 - i, 8, bit);
                }
                else
                {
                    SetReserved(modules, reserved, 8, Size - 15 + i, bit);
                }
            }
        }

        private static int FormatBits(int mask)
        {
            int data = (1 << 3) | mask;
            int value = data << 10;
            int generator = 0x537;
            for (int i = 14; i >= 10; i--)
            {
                if (((value >> i) & 1) != 0)
                {
                    value ^= generator << (i - 10);
                }
            }

            return ((data << 10) | value) ^ 0x5412;
        }

        private static bool Mask(int mask, int x, int y)
        {
            return ((x + y) & 1) == 0;
        }

        private static void SetReserved(int[,] modules, bool[,] reserved, int x, int y, int value)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size)
            {
                return;
            }

            modules[y, x] = value;
            reserved[y, x] = true;
        }

        private static byte[] ReedSolomon(byte[] data, int degree)
        {
            byte[] generator = ReedSolomonGenerator(degree);
            byte[] result = new byte[degree];

            foreach (byte dataByte in data)
            {
                int factor = dataByte ^ result[0];
                for (int i = 0; i < degree - 1; i++)
                {
                    result[i] = result[i + 1];
                }

                result[degree - 1] = 0;
                for (int i = 0; i < degree; i++)
                {
                    result[i] ^= GfMultiply(generator[i], factor);
                }
            }

            return result;
        }

        private static byte[] ReedSolomonGenerator(int degree)
        {
            byte[] result = new byte[] { 1 };
            for (int i = 0; i < degree; i++)
            {
                byte[] next = new byte[result.Length + 1];
                for (int j = 0; j < result.Length; j++)
                {
                    next[j] ^= result[j];
                    next[j + 1] ^= GfMultiply(result[j], GfPow(i));
                }

                result = next;
            }

            byte[] trimmed = new byte[degree];
            Array.Copy(result, 1, trimmed, 0, degree);
            return trimmed;
        }

        private static byte GfPow(int exponent)
        {
            int value = 1;
            for (int i = 0; i < exponent; i++)
            {
                value = GfMultiply((byte)value, 2);
            }

            return (byte)value;
        }

        private static byte GfMultiply(int x, int y)
        {
            int result = 0;
            while (y > 0)
            {
                if ((y & 1) != 0)
                {
                    result ^= x;
                }

                x <<= 1;
                if ((x & 0x100) != 0)
                {
                    x ^= 0x11D;
                }

                y >>= 1;
            }

            return (byte)result;
        }

        private static string VerificationCode(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""));
                return BitConverter.ToString(hash, 0, 5).Replace("-", "");
            }
        }

        private static string Shorten(string value, int maxLength)
        {
            value = Safe(value);
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private static string Safe(string value)
        {
            return value == null ? "" : value.Trim();
        }

        private class BitBuffer
        {
            private readonly System.Collections.Generic.List<int> bits = new System.Collections.Generic.List<int>();

            public int Length
            {
                get { return bits.Count; }
            }

            public void Append(int value, int length)
            {
                for (int i = length - 1; i >= 0; i--)
                {
                    bits.Add((value >> i) & 1);
                }
            }

            public byte[] ToBytes()
            {
                byte[] result = new byte[(bits.Count + 7) / 8];
                for (int i = 0; i < bits.Count; i++)
                {
                    result[i / 8] |= (byte)(bits[i] << (7 - (i % 8)));
                }

                return result;
            }
        }
    }
}
