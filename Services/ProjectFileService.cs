using Holst.Models;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Holst.Services
{
    public interface IProjectFileService
    {
        Task<bool> SaveProjectAsync(BaseProject project, string filePath);
        Task<BaseProject> LoadProjectAsync(string filePath);
    }

    public class ProjectFileService : IProjectFileService
    {
        private const int MagicLength = 5;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HOLST");
        private const byte Version = 0x01;
        private readonly string _passphrase;

        public ProjectFileService(string passphrase = "HolstDefaultKey2026!")
        {
            _passphrase = passphrase;
        }

        public async Task<bool> SaveProjectAsync(BaseProject project, string filePath)
        {
            try
            {
                byte[] payload = SerializeRaw(project);
                byte[] encrypted = Encrypt(payload, _passphrase);
                await File.WriteAllBytesAsync(filePath, encrypted);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<BaseProject> LoadProjectAsync(string filePath)
        {
            byte[] encrypted = await File.ReadAllBytesAsync(filePath);
            byte[] payload = Decrypt(encrypted, _passphrase);
            return DeserializeRaw(payload);
        }

        private byte[] SerializeRaw(BaseProject project)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);
            writer.Write(project.Id.ToByteArray());          // 16 bytes
            writer.Write((int)project.Type);                 // 4 bytes
            writer.Write(project.Name ?? string.Empty);      // length-prefixed UTF-8
            writer.Write(project.Author ?? string.Empty);    // length-prefixed UTF-8
            writer.Write(project.CreatedAt.ToBinary());      // 8 bytes
            writer.Write(project.UpdatedAt.ToBinary());      // 8 bytes
            writer.Flush();
            return ms.ToArray();
        }

        private BaseProject DeserializeRaw(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms, Encoding.UTF8);
            var id = new Guid(reader.ReadBytes(16));
            var type = (ProjectType)reader.ReadInt32();
            var name = reader.ReadString();
            var author = reader.ReadString();
            var created = System.DateTime.FromBinary(reader.ReadInt64());
            var updated = System.DateTime.FromBinary(reader.ReadInt64());

            var project = ProjectFactory.CreateProject(type, name, author);
            project.Id = id;
            project.CreatedAt = created;
            project.UpdatedAt = updated;
            return project;
        }

        private byte[] Encrypt(byte[] data, string passphrase)
        {
            byte[] salt = new byte[16];
            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(salt);
            RandomNumberGenerator.Fill(iv);

            using var derive = new Rfc2898DeriveBytes(passphrase, salt, 10000, HashAlgorithmName.SHA256);
            byte[] key = derive.GetBytes(32);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

            using var resultStream = new MemoryStream();
            using var writer = new BinaryWriter(resultStream);
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(salt);
            writer.Write(iv);
            writer.Write(encrypted.Length);
            writer.Write(encrypted);
            writer.Flush();

            return resultStream.ToArray();
        }

        private byte[] Decrypt(byte[] data, string passphrase)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            byte[] fileMagic = reader.ReadBytes(MagicLength);
            bool magicOk = fileMagic.Length == MagicLength;
            for (int i = 0; magicOk && i < MagicLength; i++)
                if (fileMagic[i] != Magic[i]) magicOk = false;

            if (!magicOk)
                throw new InvalidDataException("Invalid Holst file format.");

            byte version = reader.ReadByte();
            if (version != Version)
                throw new InvalidDataException($"Unsupported Holst file version: {version}");

            byte[] salt = reader.ReadBytes(16);
            byte[] iv = reader.ReadBytes(16);
            int length = reader.ReadInt32();
            byte[] encrypted = reader.ReadBytes(length);

            using var derive = new Rfc2898DeriveBytes(passphrase, salt, 10000, HashAlgorithmName.SHA256);
            byte[] key = derive.GetBytes(32);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
        }
    }
}
