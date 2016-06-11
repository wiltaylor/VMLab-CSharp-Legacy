/**********************************************************************************************************************************
* This is the VMLab build script. It uses the Cake build system.
***********************************************************************************************************************************/
var target = Argument("target", "Default");

var ReleaseFolder = "./Release";
var UnitTestFolder = "./UnitTest";
var SolutionFile = "VMLab.sln";

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTest");

Task("Clean")
    .IsDependentOn("CleanBuild")
    .IsDependentOn("CleanUnitTestBuild");
    
Task("CleanBuild")
    .Does(() => {
        CleanDirectory(ReleaseFolder);
    });
    
Task("CleanUnitTestBuild")
    .Does(() => {
       CleanDirectory(UnitTestFolder);
    });

Task("Build")
    .IsDependentOn("CleanBuild")
    .IsDependentOn("CopyManifest")
    .IsDependentOn("CopyTools")
    .IsDependentOn("BuildMain")
    .IsDependentOn("BuildDriversDebug");

Task("BuildDebug")
    .IsDependentOn("CleanBuild")
    .IsDependentOn("CopyManifest")
    .IsDependentOn("CopyTools")
    .IsDependentOn("BuildMainDebug")
    .IsDependentOn("BuildDriversDebug");
    
Task("BuildMainDebug")
    .Does(() => {
       MSBuild(SolutionFile, config =>
            config.SetConfiguration("Debug")
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2015)
            .WithTarget("VMLab")
            .WithProperty("OutDir", "../" + ReleaseFolder)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
            .SetPlatformTarget(PlatformTarget.MSIL));
            
    });
    
Task("BuildMain")
    .Does(() => {
       MSBuild(SolutionFile, config =>
            config.SetConfiguration("Release")
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2015)
            .WithTarget("VMLab")
            .WithProperty("OutDir", "../" + ReleaseFolder)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
            .SetPlatformTarget(PlatformTarget.MSIL));
            
    });
    
Task("CopyManifest")
    .Does(() => CopyFileToDirectory("./vmlab.psd1", ReleaseFolder));
    
Task("CopyTools")
    .Does(() => CopyFiles("./VMLab/Tools/*.*", ReleaseFolder));
    
Task("BuildDriversDebug")
    .IsDependentOn("BuildVMwareWorkstationDriverDebug");
    
Task("BuildDrivers")
    .IsDependentOn("BuildVMwareWorkstationDriver");
    
Task("BuildVMwareWorkstationDriverDebug")
    .Does(() => {
        MSBuild(SolutionFile, config =>
            config.SetConfiguration("Debug")
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2015)
            .WithTarget("VMLab_Driver_VMWareWorkstation")
            .WithProperty("OutDir", "../" + ReleaseFolder)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
            .SetPlatformTarget(PlatformTarget.MSIL));
    });


Task("BuildVMwareWorkstationDriver")
    .Does(() => {
        MSBuild(SolutionFile, config =>
            config.SetConfiguration("Release")
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2015)
            .WithTarget("VMLab_Driver_VMWareWorkstation")
            .WithProperty("OutDir", "../" + ReleaseFolder)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
            .SetPlatformTarget(PlatformTarget.MSIL));
    });
    
Task("UnitTest")
    .IsDependentOn("BuildUnitTest")
    .Does(() => MSTest(UnitTestFolder + "/*.Test.dll", new MSTestSettings  { NoIsolation = false }));
    
Task("BuildUnitTest")
    .IsDependentOn("CleanUnitTestBuild")
    .Does(() => {
        MSBuild(SolutionFile, config =>
            config.SetConfiguration("Release")
            .SetVerbosity(Verbosity.Minimal)
            .UseToolVersion(MSBuildToolVersion.VS2015)
            .WithProperty("OutDir", "../" + UnitTestFolder)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
            .SetPlatformTarget(PlatformTarget.MSIL));
    });
    
Task("Install") 
    .IsDependentOn("Build")
    .Does(() => 
    {
        var modulepath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Documents\\WindowsPowerShell\\Modules\\vmlab";
        CleanDirectory(modulepath);
        CopyFiles(ReleaseFolder + "/*.*", modulepath);
    });

if(HasArgument("Interactive"))
{
    RunTarget("BuildDebug");
    Console.WriteLine("Entering test Powershell Environment. Type exit to return!");
    StartProcess("powershell.exe", "-noexit -command \"Import-Module \".\\Release\\vmlab.psd1\"");
} else {
    RunTarget(target);
}
    