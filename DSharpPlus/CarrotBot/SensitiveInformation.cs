using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CarrotBot
{
    public static class SensitiveInformation //Yes I know it's ironic that a class for holding sensitive information is public
    {                                        //Wow past me writing this comment for literally just me and no one else because this file is hidden
                                             //lmao I love that past me thought I would never have any collaborators
                                             //As of CB 1.6 betas neither this file nor any other source code files in this project directly contain any sensitive information, allowing it to be released
                                             //So great job on the comments, past me

        public static readonly string? botToken = File.Exists("00_token.cb") ? AES256ReadFile("00_token.cb") : null;
        public static readonly string? betaToken = File.Exists("00_token-beta.cb") ? AES256ReadFile("00_token-beta.cb") : null;
        public static readonly string? catAPIKey = File.Exists("00_cat-api-key.cb") ? AES256ReadFile("00_cat-api-key.cb") : null;
        private static byte[]? encryptionKey = null;

        private static byte[] EncryptionKey
        {
            get
            {
                if (encryptionKey is not null) return encryptionKey;
                Assembly assembly = Assembly.GetExecutingAssembly();
                using Stream? stream = assembly.GetManifestResourceStream("CarrotBot.carrotbot-encryption-key.key");
                if (stream is null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine("ERROR: No encryption key was embedded in this build. The application will not be able to encode or decode data files.");
                    Console.ResetColor();
                    return Array.Empty<byte>();
                }

                using BinaryReader reader = new(stream);
                encryptionKey = reader.ReadBytes(32);
                return encryptionKey;
            }
        }

        public static async Task AES256WriteFileAsync(string path, string text)
        {
            await using FileStream fileStream = new(path, FileMode.Create);
            using Aes aes = Aes.Create();
            aes.Key = EncryptionKey;
            byte[] iv = aes.IV;
            fileStream.Write(iv, 0, iv.Length);
            await using CryptoStream cryptoStream = new(
                fileStream,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write);
            await using StreamWriter encryptWriter = new(cryptoStream, Encoding.UTF8);
            await encryptWriter.WriteAsync(text);
        }

        public static void AES256WriteFile(string path, string text)
        {
            using FileStream fileStream = new(path, FileMode.Create);
            using Aes aes = Aes.Create();
            aes.Key = EncryptionKey;
            byte[] iv = aes.IV;
            fileStream.Write(iv, 0, iv.Length);
            using CryptoStream cryptoStream = new(
                fileStream,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write);
            using StreamWriter encryptWriter = new(cryptoStream, Encoding.UTF8);
            encryptWriter.Write(text);
        }

        public static async Task<string> AES256ReadFileAsync(string path)
        {
            await using FileStream fileStream = new(path, FileMode.Open);
            using Aes aes = Aes.Create();
            byte[] iv = new byte[aes.IV.Length];
            int numBytesToRead = aes.IV.Length;
            int numBytesRead = 0;
            while (numBytesToRead > 0)
            {
                int n = fileStream.Read(iv, numBytesRead, numBytesToRead);
                if (n == 0) break;

                numBytesRead += n;
                numBytesToRead -= n;
            }
            await using CryptoStream cryptoStream = new(
                fileStream,
                aes.CreateDecryptor(EncryptionKey, iv),
                CryptoStreamMode.Read);
            using StreamReader decryptReader = new(cryptoStream);
            return await decryptReader.ReadToEndAsync();
        }

        public static string AES256ReadFile(string path)
        {
            using FileStream fileStream = new(path, FileMode.Open);
            using Aes aes = Aes.Create();
            byte[] iv = new byte[aes.IV.Length];
            int numBytesToRead = aes.IV.Length;
            int numBytesRead = 0;
            while (numBytesToRead > 0)
            {
                int n = fileStream.Read(iv, numBytesRead, numBytesToRead);
                if (n == 0) break;

                numBytesRead += n;
                numBytesToRead -= n;
            }
            using CryptoStream cryptoStream = new(
                fileStream,
                aes.CreateDecryptor(EncryptionKey, iv),
                CryptoStreamMode.Read);
            using StreamReader decryptReader = new(cryptoStream, Encoding.UTF8);
            return decryptReader.ReadToEnd();
        }
    }
}
