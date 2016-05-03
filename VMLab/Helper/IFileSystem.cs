using System;
using System.IO;
using System.IO.Compression;
using System.Management.Automation.Remoting.WSMan;
using VMLab.Model;

namespace VMLab.Helper
{
    public interface IFileSystem
    {
        void DeleteFile(string path);
        void DeleteFolder(string path, bool recursive);
        bool FileExists(string path);
        bool FolderExists(string path);
        string ReadFile(string path);
        void SetFile(string path, string content);
        string[] GetSubFolders(string path);
        string[] GetSubFiles(string path);
        void Copy(string source, string destination);
        void CreateFolder(string path);
        string GetPathLeaf(string path);
        void ExtractArchive(string archivepath, string destination);
        void MoveFolder(string originalpath, string newpath);
        void CreateArchive(string sourcefolder, string archivepath);
        void MoveFile(string source, string destination);
        string ConvertPathRelativeToFull(string path);
    }

    public class FileSystem : IFileSystem
    {
        public void DeleteFile(string path) { File.Delete(path); }

        public void DeleteFolder(string path, bool recursive){ Directory.Delete(path, recursive); }

        public bool FileExists(string path) { return File.Exists(path); }

        public bool FolderExists(string path) { return Directory.Exists(path); }

        public string ReadFile(string path) { return File.ReadAllText(path); }

        public void SetFile(string path, string content) { File.WriteAllText(path, content); }

        public string[] GetSubFolders(string path) { return Directory.GetDirectories(path); }

        public string[] GetSubFiles(string path) { return Directory.GetFiles(path); }

        public void Copy(string source, string destination) { File.Copy(source, destination); }

        public void CreateFolder(string path) { Directory.CreateDirectory(path); }

        public string GetPathLeaf(string path) { return Path.GetFileName(path); }

        public void ExtractArchive(string archivepath, string destination)
        {
            ZipFile.ExtractToDirectory(archivepath, destination);
        }

        public void MoveFolder(string originalpath, string newpath)
        {
            Directory.Move(originalpath, newpath);
        }

        public void CreateArchive(string sourcefolder, string archivepath)
        {
            ZipFile.CreateFromDirectory(sourcefolder, archivepath);
        }

        public void MoveFile(string source, string destination)
        {
            File.Move(source, destination);
        }

        public string ConvertPathRelativeToFull(string path)
        {
            var svc = ServiceDiscovery.GetInstance();
            var env = svc.GetObject<IEnvironmentDetails>();
            var currentdir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = env.WorkingDirectory;

            var directory = Path.GetFullPath(path);

            Environment.CurrentDirectory = currentdir;

            return directory;
        }
    }
}
