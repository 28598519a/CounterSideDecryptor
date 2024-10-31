using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CounterSide;

namespace Cs.Engine.Network.Buffer.Detail
{
    public static class Crypto
    {

        public static ulong[] maskList = new ulong[4];

        private static ulong DirectToUint64(byte[] buffer, int startIndex)
        {
            return (ulong)(
                buffer[startIndex] |
                ((
                buffer[startIndex + 1] |
                ((
                buffer[startIndex + 2] |
                ((
                buffer[startIndex + 3] |
                ((
                buffer[startIndex + 4] |
                ((ulong)(
                buffer[startIndex + 5] |
                ((
                (UInt16)buffer[startIndex + 6] |
                (
                buffer[startIndex + 7]
                << 8)) << 8)) << 8)) << 8)) << 8)) << 8)) << 8)
                );
        }

        private static void DirectWriteTo(ulong data, byte[] buffer, int position)
        {
            byte[] outPut = BitConverter.GetBytes(data);
            buffer[position] = outPut[0];
            buffer[position + 1] = outPut[1];
            buffer[position + 2] = outPut[2];
            buffer[position + 3] = outPut[3];
            buffer[position + 4] = outPut[4];
            buffer[position + 5] = outPut[5];
            buffer[position + 6] = outPut[6];
            buffer[position + 7] = outPut[7];
        }

        public static void Encrypt(byte[] buffer, int size)
        {
            if (buffer != null)
            {
                int maskIndex = 0;
                Encrypt(buffer, size, ref maskIndex);
            }
        }

        public static void Encrypt(byte[] buffer, int size, ref int maskIndex)
        {
            if (buffer != null && size >= 1)
            {
                for (int i = 0; i < size;)
                {
                    ulong nowKey = maskList[maskIndex];
                    int decSize = size - i;
                    if (decSize > 7)
                    {
                        ulong v16 = DirectToUint64(buffer, i);
                        DirectWriteTo(v16 ^ nowKey, buffer, i);
                        decSize = 8;
                    }
                    else if (i < size)
                    {
                        for (int j = 0; size - i != j; j++)
                        {
                            buffer[i + j] ^= (byte)(255 & nowKey);
                        }
                    }
                    i += decSize;
                    maskIndex = maskIndex + 1 - (maskIndex + 1) / maskList.Length * maskList.Length;
                }
            }
        }

        public static void GetMaskList(string filePath)
        {
            if (filePath != null)
            {
                string v29 = filePath.ToLower();
                string v30 = Path.GetFileNameWithoutExtension(v29);

                Encoding v31 = Encoding.UTF8;
                byte[] v32 = v31.GetBytes(v30.ToCharArray());
                CryptoServices cryptoServices = new CryptoServices();
                string v33 = cryptoServices.MD5Hash(v32);

                string v34 = v33.Substring(0, 16);
                string v35 = v33.Substring(16, 16);
                string v36 = v33.Substring(0, 8);
                string v37 = v33.Substring(16, 8);
                string v38 = v36 + v37;
                string v39 = v33.Substring(8, 8);
                string v40 = v33.Substring(24, 8);
                string v41 = v39 + v40;

                maskList[0] = ulong.Parse(v34, NumberStyles.AllowHexSpecifier);
                maskList[1] = ulong.Parse(v35, NumberStyles.AllowHexSpecifier);
                maskList[2] = ulong.Parse(v38, NumberStyles.AllowHexSpecifier);
                maskList[3] = ulong.Parse(v41, NumberStyles.AllowHexSpecifier);
            }
        }
    }
}

public class BetterStreamingAssets
{
    public static Stream OpenRead(string path)
    {
        FileStream fs = File.OpenRead(path);
        return new SubReadOnlyStream(fs, 0, 0);
    }

    class SubReadOnlyStream : Stream
    {
        private readonly long m_offset;
        private readonly bool m_leaveOpen;
        private Nullable<long> m_length;
        private Stream m_actualStream;
        private long m_position;

        public SubReadOnlyStream(Stream actualStream, bool leaveOpen = false)
        {
            m_actualStream = actualStream;
            m_leaveOpen = leaveOpen;
        }

        public SubReadOnlyStream(Stream actualStream, long offset, long length, bool leaveOpen = false) : this(actualStream, leaveOpen)
        {
            m_offset = offset;
            m_position = offset;
            m_length = length;
        }

        public override bool CanRead => m_actualStream.CanRead;

        public override bool CanSeek => m_actualStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                if(m_length == 0)
                {
                    m_length = m_actualStream.Length - m_offset;
                }
                return (long)m_length;
            }
        }

        public override long Position { get => m_position - m_offset; set => m_position += m_offset + value; }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(m_actualStream.Position != m_position)
            {
                m_actualStream.Seek(Position, 0);
            }

            if (m_length != 0)
            {
                if(m_position + count > m_length + m_offset)
                {
                    count = (int)(m_length + m_offset - m_position);
                }
            }

            int readSize = m_actualStream.Read(buffer, offset, count);
            m_position += readSize;
            return readSize;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long S;
            if (origin == SeekOrigin.Begin)
            {
                S = m_actualStream.Seek(m_offset + offset, SeekOrigin.Begin);
            }

            else if (origin != SeekOrigin.End)
            {
                S = m_actualStream.Seek(offset, SeekOrigin.Current);
            }
            else
            {
                S = m_actualStream.Seek(m_offset + offset + m_actualStream.Length, SeekOrigin.End);
            }

            m_position = S;
            return S - m_offset;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    public class NKCAssetbundleCryptoStreamMem : MemoryStream
    {
        private byte[] decryptedArray;
        private long decryptSize;

        public NKCAssetbundleCryptoStreamMem(byte[] buffer) : base(buffer)
        {
            decryptedArray = new byte[212];
            decryptSize = Math.Min(Length, 212);
            base.Read(decryptedArray, 0, (int)0);
            Cs.Engine.Network.Buffer.Detail.Crypto.Encrypt(decryptedArray, (int)0);
            Seek(0, 0);
        }
    }

    public class NKCAssetbundleCryptoStream : FileStream
    {
        private byte[] decryptedArray;
        private long decryptSize;

        public NKCAssetbundleCryptoStream(string path, FileMode mode, FileAccess access) : base(path, mode, access)
        {
            decryptedArray = new byte[212];
            decryptSize = Math.Min(Length, 212);
            base.Read(decryptedArray, 0, (int)decryptSize);
            Cs.Engine.Network.Buffer.Detail.Crypto.Encrypt(decryptedArray, (int)decryptSize);
            base.Seek(0, 0);
        }

        public override int Read(byte[] array, int offset, int count)
        {
            var readSize = base.Read(array, offset, count);
            if (decryptSize > base.Position)
            {
                int length;
                if (base.Position + count >= decryptSize)
                {
                    length = (int)(decryptSize - base.Position);
                }
                else
                {
                    length = count;
                }
                Array.Copy(decryptedArray, base.Position, array, offset, length);
            }
            return readSize;
        }
    }

    public class NKCAssetbundleInnerStream : Stream
    {
        private Stream betterStream;
        private byte[] decryptedArray;
        private long decryptSize;

        public NKCAssetbundleInnerStream(string path)
        {
            decryptedArray = new byte[212];
            betterStream = OpenRead(path);
            decryptSize = Math.Min(Length, 212);
            betterStream.Read(decryptedArray, 0, (int)decryptSize);
            Cs.Engine.Network.Buffer.Detail.Crypto.GetMaskList(path);
            Cs.Engine.Network.Buffer.Detail.Crypto.Encrypt(decryptedArray, (int)decryptSize);
            betterStream.Seek(0, 0);
        }

        public override bool CanRead => betterStream.CanRead;

        public override bool CanSeek => betterStream.CanSeek;

        public override bool CanWrite => betterStream.CanWrite;

        public override long Length => betterStream.Length;

        public override long Position { get => betterStream.Position; set => betterStream.Position=value; }

        public override void Flush()
        {
            betterStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long Pos = betterStream.Position;
            int readSize = betterStream.Read(buffer, offset, count);
            if (decryptSize > Pos)
            {
                int destIndex = 0;
                if (Pos + count >= decryptSize)
                {
                    destIndex = (int)(decryptSize - Pos);
                }
                else
                {
                    destIndex = count;
                }
                Array.Copy(decryptedArray, Pos, buffer, offset, destIndex);
            }
            return readSize;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return betterStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            betterStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            betterStream.Write(buffer, offset, count);
        }
    }
}

namespace CounterSide
{
    class Program
    {
        static void Main(string[] args)
        {
            string Root = Environment.CurrentDirectory;

            // Put files and folders (ex: StreamingAssets) in to CounterSide folder and execute this program to decrypt
            DirectoryInfo AssetFolder = new DirectoryInfo("CounterSide");
            if (!Directory.Exists(Root + "/dec"))
            {
                Directory.CreateDirectory(Root + "/dec");
            }

            string[] extensions = new[] { ".asset", ".vkor", ".vjpn", ".twn", ".gbtwn" };
            foreach (FileInfo file in AssetFolder.GetFiles("*.*", SearchOption.AllDirectories).Where(f => extensions.Contains(f.Extension.ToLower())))
            {
                Console.WriteLine("Decrypting: " + file.FullName);
                var decryptor = new BetterStreamingAssets.NKCAssetbundleInnerStream(file.FullName);
                byte[] dec = new byte[decryptor.Length];
                decryptor.Read(dec, 0, (int)decryptor.Length);

                string newfile = Root + "/dec" + file.FullName.Replace(AssetFolder.FullName, String.Empty);
                if(!Directory.Exists(Path.GetDirectoryName(newfile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newfile));
                }
                File.WriteAllBytes(newfile, dec);
            }
        }
    }
}
