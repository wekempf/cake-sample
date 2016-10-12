#tool "nuget:?package=xunit.runner.console"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"
#tool "nuget:?package=GitVersion.CommandLine"
#tool coveralls.net

#addin Cake.Coveralls

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var isRunningOnBuildServer = AppVeyor.IsRunningOnAppVeyor;
var updateAssemblyInfo = HasArgument("updateassemblyinfo") || isRunningOnBuildServer;
var solutions = GetFiles("**/*.sln").Except(GetFiles("./.tools/**/packages"));
var testResultsDir = Directory("./TestResults");
var coverageFile = testResultsDir + File("coverage.xml");

Task("Clean")
    .Does(() => {
        var binDirs = GetDirectories("**/bin");
        var objDirs = GetDirectories("**/obj");
        var testDirs = GetDirectories("**/TestResults");
        var packageDirs = GetDirectories("**/packages").Except(GetDirectories("./.tools/**/packages"));
        var directories = binDirs
            .Concat(objDirs)
            .Concat(testDirs)
            .Concat(packageDirs)
            .Where(d => DirectoryExists(d));
        DeleteDirectories(directories, true);

        if (DirectoryExists(testResultsDir)) {
            DeleteDirectory(testResultsDir, true);
        }
        if (FileExists(coverageFile)) {
            DeleteFile(coverageFile);
        }
    });

Task("Version")
    .Does(() =>{
        var version = GitVersion(new GitVersionSettings{
            UpdateAssemblyInfo = updateAssemblyInfo
        });
        Information("Version: {0}", version.AssemblySemVer);
        if (updateAssemblyInfo) {
            Information("Updated assembly information.");
        }
    });

Task("NuGetRestore")
    .Does(() => {
        foreach (var sln in solutions) {
            NuGetRestore(sln);
        }
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("NuGetRestore")
    .IsDependentOn("Version")
    .Does(() => {
        foreach (var sln in solutions) {
            MSBuild(sln);
        }
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
        var testAssemblies = GetFiles("**/bin/**/*.Tests.dll");
        EnsureDirectoryExists(testResultsDir);

        OpenCover(tool => tool.XUnit2(testAssemblies, new XUnit2Settings { ShadowCopy = false }),
            new FilePath(coverageFile),
            new OpenCoverSettings()
                .WithFilter("+[*]*")
                .WithFilter("-[*.Tests]*")
                .WithFilter("-[xunit.*]*"));

        ReportGenerator(coverageFile, testResultsDir);

        if (isRunningOnBuildServer) {
            CoverallsNet(coverageFile, CoverallsNetReportType.OpenCover, new CoverallsNetSettings {
                RepoToken = EnvironmentVariable("CoverallsToken")
            });
        }
    });

Task("Default")
    .IsDependentOn("Test");

RunTarget(target);