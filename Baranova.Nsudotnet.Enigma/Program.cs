using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;


namespace Baranova.Nsudotnet.Enigma
{
    class Program
    {
        private static string _mode;
        private static string _inputFile;
        private static string _algorithm;
        private static string _outputFile;
        private static string _keyFile;

        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("encrypt <inputFile> <algorithm> <outputFile>");
            Console.WriteLine("decrypt <inputFile> <algorithm> <keyFile> <outputFile>");
            Console.WriteLine();
            Console.WriteLine("Algoritm may be: AES, DES, RC2, RIJNDAEL.");
        }

        static void Main(string[] args)
        {
            if ((args.Length == 4) || (args.Length == 5))
            {
                _mode = args[0];
                _inputFile = args[1];
                _algorithm = args[2];
                _keyFile = null;
                if (args.Length == 5)
                {
                    _keyFile = args[3];
                    _outputFile = args[4];
                }
                else
                    _outputFile = args[3];
                if ((_mode != "encrypt") && (_mode != "decrypt"))
                {
                    return;
                }

                if (_mode == "encrypt")
                    Encrypt(_inputFile, _algorithm, _outputFile);
                else
                    Decrypt(_inputFile, _algorithm, _keyFile, _outputFile);
            }
            else
            {
                PrintUsage();
            }
        }

        public static Tuple<byte[], byte[]> ReadKey(int length, string keyFile)
        {
            using (FileStream keyStream = new FileStream(keyFile, FileMode.Open, FileAccess.Read))
            {
                byte[] keyBytes = new byte[keyStream.Length - length];
                keyStream.Read(keyBytes, 0, keyBytes.Length);
                byte[] ivBytes = new byte[length];
                keyStream.Read(ivBytes, 0, ivBytes.Length);
                var tuple = new Tuple<byte[], byte[]>(keyBytes, ivBytes);
                return tuple;
            }
        }


        public static void WriteKey(string Key, string dir, string iv)
        {
            string keys = dir + "\\key.txt";
            using (FileStream fsKeys = new FileStream(keys, FileMode.OpenOrCreate, FileAccess.Write))
            {
                byte[] keyArray = Encoding.ASCII.GetBytes(Key);
                byte[] ivArray = Encoding.ASCII.GetBytes(iv);
                fsKeys.Write(keyArray, 0, keyArray.Length);
                fsKeys.Write(ivArray, 0, ivArray.Length);
                fsKeys.Close();
            }
        }


        public static void WriteCryptoData(FileStream fsInput, FileStream fsOutput, ICryptoTransform cryptoTransform)
        {
            using (CryptoStream cryptostream = new CryptoStream(fsOutput, cryptoTransform, CryptoStreamMode.Write))
            {
                fsInput.CopyTo(cryptostream);
                cryptostream.Close();
                fsInput.Close();
                fsOutput.Close();
            }
        }


        static SymmetricAlgorithm GetServiceProvider(string alorithm)
        {
            switch (alorithm.ToUpper())
            {
                case "AES":
                    return new AesCryptoServiceProvider();
                case "DES":
                    return new DESCryptoServiceProvider();
                case "RC2":
                    return new RC2CryptoServiceProvider();
                case "RIJNDAEL":
                    return new RijndaelManaged();
                default:
                    throw new Exception("Incorrect algorithm");
            }
        }


        public static void Encrypt(string inputFile, string algorithm, string outputFile)
        {
            using (SymmetricAlgorithm provider = GetServiceProvider(algorithm))
            {
                string key = ASCIIEncoding.ASCII.GetString(provider.Key);
                string iv = ASCIIEncoding.ASCII.GetString(provider.IV);


                using (FileStream fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream fsOutput = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        string dir = Path.GetDirectoryName(outputFile);
                        WriteKey(key, dir, iv);


                        var AES = GetServiceProvider(algorithm);
                        AES.Key = ASCIIEncoding.ASCII.GetBytes(key);
                        AES.IV = ASCIIEncoding.ASCII.GetBytes(iv);
                        ICryptoTransform aesEncrypt = AES.CreateEncryptor();
                        WriteCryptoData(fsInput, fsOutput, aesEncrypt);
                    }
                }
            }
        }


        public static void Decrypt(string inputFile, string algorithm, string key, string outputFile)
        {
            int length = 8;
            if (algorithm.ToUpper() == "AES" || algorithm.ToUpper() == "RIJNDAEL")
                length += length;
            Tuple<byte[], byte[]> keyIV = ReadKey(length, key);
            var provider = GetServiceProvider(algorithm);
            provider.Key = keyIV.Item1;
            provider.IV = keyIV.Item2;
            using (FileStream fsRead = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            {
                using (FileStream fsWrite = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    using (ICryptoTransform decrypt = provider.CreateDecryptor())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(fsRead, decrypt, CryptoStreamMode.Read))
                        {
                            cryptoStream.CopyTo(fsWrite);
                        }
                    }
                }
            }
        }
    }
}

