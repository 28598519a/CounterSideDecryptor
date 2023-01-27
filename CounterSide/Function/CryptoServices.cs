using System.Text;
using System.Security.Cryptography;

namespace CounterSide
{
	class CryptoServices
	{
		public string MD5Hash(byte[] input)
		{
			MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash(input);

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("x2"));
			}

			return sb.ToString();
		}
	}
}
