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
GitVersion version = null;

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
        version = GitVersion(new GitVersionSettings{
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
            MSBuild(sln, new MSBuildSettings {
                Configuration = configuration
            });
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

Task("NuGetPack")
    //.IsDependentOn("Test")
    .IsDependentOn("Version")
    .Does(() => {
        // Bowling
        var outputDirectory = MakeAbsolute(Directory("./src/Bowling/bin/" + configuration)); 
        NuGetPack(new NuGetPackSettings
        {
            Id = "Bowling",
            Version = version.NuGetVersion,
            Title = "Bowling Kata Example",
            Authors = new[] { "William E. Kempf" },
            Owners = new[] { "William E. Kempf" },
            Description = "A sample library for the well known Bowling Kata",
            Summary = "This sample library is created by the sample build found at https://github.com/wekempf/cake-sample.",
            ProjectUrl = new Uri("https://github.com/wekempf/cake-sample"),
            IconUrl = new Uri("https://nuget.org/Content/Images/packageDefaultIcon-50x50.png"),
            LicenseUrl = new Uri("https://github.com/wekempf/cake-sample/blob/master/LICENSE.txt"),
            Copyright = "William E. Kempf 2016",
            ReleaseNotes = new[] { "something" },
            Tags = new[] { "Cake", "Script", "Sample", "Build" },
            RequireLicenseAcceptance = false,
            Symbols = true,
            NoPackageAnalysis = false,
            Files = new[] {
                new NuSpecContent { Source = outputDirectory + "/Bowling.dll", Target = "lib/net461" }
            },
            BasePath = outputDirectory,
            OutputDirectory = outputDirectory
        });
    });

Task("Default")
    .IsDependentOn("NuGetPack");

RunTarget(target);