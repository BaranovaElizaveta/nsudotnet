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

        public static Tuple<string, string> ReadKey(int length, string keyFile)
        {
            using (FileStream keyStream = new FileStream(keyFile, FileMode.Open, FileAccess.Read))
            {
                /*byte[] keyBytes = new byte[keyStream.Length - length];
                keyStream.Read(keyBytes, 0, keyBytes.Length);
                byte[] ivBytes = new byte[length];
                keyStream.Read(ivBytes, 0, ivBytes.Length);
                var tuple = new Tuple<byte[], byte[]>(keyBytes, ivBytes);*/
                string key;
                string iv;
                using (StreamReader reader = new StreamReader(keyStream))
                {
                    key = reader.ReadLine();
                    iv = reader.ReadLine();
                }
                var tuple = new Tuple<string, string>(key, iv);
                return tuple;
            }
        }


        public static void WriteKey(string Key, string dir, string iv)
        {
            string keys = dir + "\\key.txt";
            using (FileStream fsKeys = new FileStream(keys, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (var writer = new StreamWriter(fsKeys))
                {
                    writer.WriteLine(Key);
                    writer.WriteLine(iv);
                }
            }
        }


        public static void WriteCryptoData(FileStream fsInput, FileStream fsOutput, ICryptoTransform cryptoTransform)
        {
            using (CryptoStream cryptostream = new CryptoStream(fsOutput, cryptoTransform, CryptoStreamMode.Write))
            {
                fsInput.CopyTo(cryptostream);
                
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
                /*string key = ASCIIEncoding.ASCII.GetString(provider.Key);
                string iv = ASCIIEncoding.ASCII.GetString(provider.IV);*/

                string key = Convert.ToBase64String(provider.Key);
                string iv = Convert.ToBase64String(provider.IV);

                using (FileStream fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream fsOutput = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        string dir = Path.GetDirectoryName(outputFile);
                        WriteKey(key, dir, iv);
                        
                        
                        ICryptoTransform aesEncrypt = provider.CreateEncryptor();
                        WriteCryptoData(fsInput, fsOutput, aesEncrypt);
                    }
                }
            }
        }


        public static void Decrypt(string inputFile, string algorithm, string key, string outputFile)
        {
            int length = 8; //for DES and RC2
            if (algorithm.ToUpper() == "AES" || algorithm.ToUpper() == "RIJNDAEL")
                length *= 2;
            Tuple<string, string> keyIV = ReadKey(length, key);
            using (var provider = GetServiceProvider(algorithm))
            {
                provider.Key = Convert.FromBase64String(keyIV.Item1);
                provider.IV = Convert.FromBase64String(keyIV.Item2);
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
}

