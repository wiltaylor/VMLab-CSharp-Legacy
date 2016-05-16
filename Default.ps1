Properties {
	$build_dir = Split-Path $psake.build_script_file
	$build_artifacts_dir = "$build_dir\Release\"
    $build_unittest_dir = "$build_dir\UnitTest\"
	$MSBuildPath = 'C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe'
    $MSTestPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio 14.0\Common7\IDE\MSTest.exe"
}
Task Default -depends Build, UnitTest

Task Clean -depends CleanBuild, CleanUnitTestBuild

Task CleanBuild {
    if(Test-Path $build_artifacts_dir) { 
	    Remove-Item $build_artifacts_dir -Force -Recurse
    }
	New-Item $build_artifacts_dir -ItemType Directory
}

Task CleanUnitTestBuild {
    if(Test-Path $build_unittest_dir) { 
	    Remove-Item $build_unittest_dir -Force -Recurse
    }
	New-Item $build_unittest_dir -ItemType Directory
}

Task BuildDebug -depends CleanBuild, CopyManifest, CopyTools, BuildMainDebug, BuildVMwareWorkstationDriverDebug

Task Build -depends CleanBuild, CopyManifest, CopyTools, BuildMain, BuildVMwareWorkstationDriver

Task BuildMainDebug {
	Exec { &$MSBuildPath VMLab.sln /t:VMLab /p:Configuration=Debug /p:OutDir=$build_artifacts_dir }
}

Task BuildMain {
	Exec { &$MSBuildPath VMLab.sln /t:VMLab /p:Configuration=Release /p:OutDir=$build_artifacts_dir }
}

Task BuildVMwareWorkstationDriverDebug {
	Exec { &$MSBuildPath VMLab.sln /t:VMLab_Driver_VMWareWorkstation /p:Configuration=Debug /p:OutDir=$build_artifacts_dir }
}

Task BuildVMwareWorkstationDriver {
    Exec { &$MSBuildPath VMLab.sln /t:VMLab_Driver_VMWareWorkstation /p:Configuration=Release /p:OutDir=$build_artifacts_dir }
}

Task BuildUnitTest -depends CleanUnitTestBuild, CopyManifest, CopyTools {
	Exec { &$MSBuildPath VMLab.sln /t:build /p:Configuration=Release /p:OutDir=$build_unittest_dir }
}

Task CopyManifest {
   Copy-Item "$build_dir\vmlab.psd1" "$build_artifacts_dir\vmlab.psd1" 
}

Task Interactive -depends BuildDebug {
    &powershell.exe -noexit -command "Import-Module `"$build_artifacts_dir\vmlab.psd1`""

}

Task CopyTools {
    Copy-Item -Path "$build_dir\vmlab\tools\*.*" $build_artifacts_dir -Recurse
}

Task UnitTest -depends BuildUnitTest {
    Exec { &$MSTestPath /testcontainer:"$build_unittest_dir\VMLab.Test.dll" }
}

Task Install -depends Build {
        Remove-Item "$env:USERPROFILE\Documents\WindowsPowerShell\Modules\VMLab" -Force -Recurse -ErrorAction SilentlyContinue
        Copy-Item "$build_dir\Release" "$env:USERPROFILE\Documents\WindowsPowerShell\Modules\VMLab" -Recurse -Force   
}

Task Restore {
    Exec { &nuget.exe restore }
}
