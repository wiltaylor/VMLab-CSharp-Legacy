using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VMLab.Helper;
using VMLab.Model;

namespace VMLab.Test.Model
{
    [TestClass]
    public class PackageManagerTests
    {

        public IPackageManager Manager;
        public Mock<IFileSystem> FileSystem;
        public Mock<IScriptHelper> ScriptHelper;
        public Mock<IEnvironmentDetails> Environment;

        [TestInitialize]
        public void Setup()
        {

            FileSystem = new Mock<IFileSystem>();
            ScriptHelper = new Mock<IScriptHelper>();
            Environment = new Mock<IEnvironmentDetails>();

            Environment.Setup(e => e.PackageRepository).Returns(new Dictionary<string, object>());

            Manager = new PackageManager(FileSystem.Object, ScriptHelper.Object, Environment.Object);
        }

        [TestMethod]
        public void CallingAddRepositoryWillAddRepository()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\repo")).Returns(true);
            Environment.Setup(e => e.PackageRepository).Returns(new Dictionary<string, object>());
            Manager.AddRepository("TestRepo", "c:\\repo");
            Assert.IsTrue(Manager.GetRepository().Any(r => r.Name == "TestRepo" && r.Path == "c:\\repo"));
        }

        [TestMethod]
        [ExpectedException(typeof(DuplicateRepositoryException))]
        public void CallingAddRepositoryWillThrowOnDuplicates()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\repo")).Returns(true);
            Environment.Setup(e => e.PackageRepository).Returns(new Dictionary<string, object>());
            Manager.AddRepository("TestRepo", "c:\\repo");
            Manager.AddRepository("TestRepo", "c:\\repo");
            Assert.IsTrue(Manager.GetRepository().Length == 1);
        }

        [TestMethod]
        public void CallingAddRepositoryWillAddRepositoryToEnvironment()
        {
            var dict = new Mock<IDictionary<string,object>>();
            FileSystem.Setup(f => f.FolderExists("c:\\repo")).Returns(true);
            Environment.Setup(e => e.PackageRepository).Returns(dict.Object);
            Manager.AddRepository("TestRepo", "c:\\repo");
            
            dict.Verify(d => d.Add("TestRepo", "c:\\repo"));
        }

        [TestMethod]
        public void CallingRemoveRepositoryWillRemoveRepositoryFromEnvironment()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\repo")).Returns(true);
            Environment.Setup(e => e.PackageRepository).Returns(new Dictionary<string, object>());
            Manager.AddRepository("TestRepo", "c:\\repo");
            Assert.IsTrue(Manager.GetRepository().Any(r => r.Name == "TestRepo" && r.Path == "c:\\repo"));

            Manager.RemoveRepository("TestRepo");

            Assert.IsFalse(Manager.GetRepository().Any(r => r.Name == "TestRepo" && r.Path == "c:\\repo"));
        }

        [TestMethod]
        public void CallingRemoveRepositoryWillUpdateEnvironment()
        {
            var dict = new Mock<IDictionary<string, object>>();
            FileSystem.Setup(f => f.FolderExists("c:\\repo")).Returns(true);
            Environment.Setup(e => e.PackageRepository).Returns(dict.Object);
            Manager.AddRepository("TestRepo", "c:\\repo");
            Manager.RemoveRepository("TestRepo");

            dict.Verify(d => d.Remove("TestRepo"));
        }

        [TestMethod]

        public void CallingRemoveRepositoryCallsPersistOnEnvironment()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\repo")).Returns(true);
            Environment.Setup(e => e.PackageRepository).Returns(new Dictionary<string, object>());
            Manager.AddRepository("TestRepo", "c:\\repo");
            Manager.RemoveRepository("TestRepo");

            Environment.Verify(e => e.PersistEnvironment(), Times.Exactly(2));
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void CallingAddRepositoryWillThrowIfPathDoesntExist()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\repo")).Returns(false);
            Manager.AddRepository("TestRepo", "c:\\repo");
        }

        [TestMethod]
        public void CallingInstallPackageWillRunTheInstallScript()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\repo")).Returns(true);
            Environment.Setup(e => e.PackageRepository).Returns(new Dictionary<string, object>());
            FileSystem.Setup(f => f.GetSubFolders("c:\\repo")).Returns(new[] { "c:\\repo\\MyPackage" });
            FileSystem.Setup(f => f.GetSubFolders("c:\\repo\\MyPackage")).Returns(new[] { "c:\\repo\\MyPackage\\1.0.0.0" });
            FileSystem.Setup(f => f.FileExists("c:\\repo\\MyPackage\\1.0.0.0\\package.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\repo\\MyPackage\\1.0.0.0\\package.json")).Returns("{ \"Name\": \"MyPackage\", \"Version\": \"1.0.0.0\"}");
            FileSystem.Setup(f => f.ReadFile("c:\\repo\\MyPackage\\1.0.0.0\\package.ps1")).Returns("#script");

            Manager.AddRepository("TestRepo", "c:\\repo");
            Manager.ScanPackages();
            Manager.RunPackageAction("MyPackage", "1.0.0.0", "MyAction", "MyVM");

            ScriptHelper.Verify(s => s.Invoke(It.IsAny<ScriptBlock>(), It.Is<Dictionary<string, object>>(p => p["Action"].ToString() == "MyAction" && p["VMName"].ToString() == "MyVM")));
        }

        [TestMethod]
        public void CallingScanPackagesWillSearchForNewPackagesAgain()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\repo")).Returns(true);
            Environment.Setup(e => e.PackageRepository).Returns(new Dictionary<string, object>());
            FileSystem.Setup(f => f.GetSubFolders("c:\\repo")).Returns(new[] { "c:\\repo\\MyPackage" });
            FileSystem.Setup(f => f.GetSubFolders("c:\\repo\\MyPackage")).Returns(new[] { "c:\\repo\\MyPackage\\1.0.0.0" });
            FileSystem.Setup(f => f.FileExists("c:\\repo\\MyPackage\\1.0.0.0\\package.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\repo\\MyPackage\\1.0.0.0\\package.json")).Returns("{ \"Name\": \"MyPackage\", \"Version\": \"1.0.0.0\"}");
            Manager.AddRepository("TestRepo", "c:\\repo");

            Manager.ScanPackages();

            FileSystem.Verify(f => f.ReadFile("c:\\repo\\MyPackage\\1.0.0.0\\package.json"));
        }

        [TestMethod]
        public void CallingGetPackagesWillReturnArrayOfPackages()
        {
            FileSystem.Setup(f => f.FolderExists("c:\\repo")).Returns(true);
            Environment.Setup(e => e.PackageRepository).Returns(new Dictionary<string, object>());
            FileSystem.Setup(f => f.GetSubFolders("c:\\repo")).Returns(new[] { "c:\\repo\\MyPackage" });
            FileSystem.Setup(f => f.GetSubFolders("c:\\repo\\MyPackage")).Returns(new[] { "c:\\repo\\MyPackage\\1.0.0.0" });
            FileSystem.Setup(f => f.FileExists("c:\\repo\\MyPackage\\1.0.0.0\\package.json")).Returns(true);
            FileSystem.Setup(f => f.ReadFile("c:\\repo\\MyPackage\\1.0.0.0\\package.json")).Returns("{ \"Name\": \"MyPackage\", \"Version\": \"1.0.0.0\"}");
            Manager.AddRepository("TestRepo", "c:\\repo");

            Manager.ScanPackages();

            Assert.IsTrue(Manager.GetPackages().Any(p => p.Name == "MyPackage" && p.Version == "1.0.0.0"));
        }

    }
}
