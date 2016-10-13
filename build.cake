#tool "nuget:?package=xunit.runner.console"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"
#tool "nuget:?package=GitVersion.CommandLine"
#tool coveralls.net

#addin Cake.Coveralls
#addin nuget:?package=Cake.Git

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var isRunningOnBuildServer = AppVeyor.IsRunningOnAppVeyor;
var updateAssemblyInfo = HasArgument("updateassemblyinfo") || isRunningOnBuildServer;
var solutions = GetFiles("**/*.sln")
    .Except(GetFiles("./.tools/**/packages"), FilePathComparer.Default);
var testResultsDir = Directory("./TestResults");
var coverageFile = testResultsDir + File("coverage.xml");
var releaseNuGetSource = "https://www.myget.org/F/wekempf/api/v2/package";
var releaseNuGetApiKey = "985f10ae-a47c-4005-ab62-1689c9f141b2";//EnvironmentVariable("NuGetApiKey");
var unstableNuGetSource = releaseNuGetSource;
var unstableNuGetApiKey = releaseNuGetApiKey;
GitVersion version = null;

Task("Clean")
    .Does(() => {
        var binDirs = GetDirectories("**/bin");
        var objDirs = GetDirectories("**/obj");
        var testDirs = GetDirectories("**/TestResults");
        var packageDirs = GetDirectories("**/packages")
            .Except(GetDirectories("./.tools/**/packages"), DirectoryPathComparer.Default);
        var directories = binDirs
            .Concat(objDirs)
            .Concat(testDirs)
            .Concat(packageDirs);
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
            Information("Building " + sln.FullPath + ".");
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
    .IsDependentOn("Test")
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
            Symbols = false,
            NoPackageAnalysis = false,
            Files = new[] {
                new NuSpecContent { Source = outputDirectory + "/Bowling.dll", Target = "lib/net461" }
            },
            BasePath = outputDirectory,
            OutputDirectory = outputDirectory
        });
    });

Task("NuGetPush")
    .IsDependentOn("NuGetPack")
    .Does(() => {
        if (!isRunningOnBuildServer) {
            Warning("Not running on build server. Skipping.");
        } else {
            var branch = GitBranchCurrent(".").FriendlyName;
            NuGetPushSettings settings = null;
            if (branch == "master") {
                Information("Pushing release package.");
                settings = new NuGetPushSettings {
                    Source = releaseNuGetSource,
                    ApiKey = releaseNuGetApiKey
                };
            } else {
                Information("Pushing unstable package.");
                settings = new NuGetPushSettings {
                    Source = unstableNuGetSource,
                    ApiKey = unstableNuGetApiKey
                };
            }
            Information("Pushing to " + settings.Source + ".");
            var packages = GetFiles("**/*.nupkg")
                .Except(GetFiles("**/packages/**/*.nupkg"), FilePathComparer.Default)
                .Except(GetFiles("./.tools/**/*.nupkg"), FilePathComparer.Default)
                .ToArray();
            foreach (var package in packages) {
                Information("Pushing package " + package.FullPath + ".");
            }
            NuGetPush(packages, settings);
        }
    });

Task("Default")
    .IsDependentOn("NuGetPush");

RunTarget(target);

public class FilePathComparer : IEqualityComparer<FilePath>
{
    public bool Equals(FilePath x, FilePath y)
    {
        return string.Equals(x.FullPath, y.FullPath, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(FilePath x)
    {
        return x.FullPath.GetHashCode();
    }

    private static FilePathComparer instance = new FilePathComparer();
    public static FilePathComparer Default
    {
        get { return instance; }
    }
}

public class DirectoryPathComparer : IEqualityComparer<DirectoryPath>
{
    public bool Equals(DirectoryPath x, DirectoryPath y)
    {
        return string.Equals(x.FullPath, y.FullPath, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(DirectoryPath x)
    {
        return x.FullPath.GetHashCode();
    }

    private static DirectoryPathComparer instance = new DirectoryPathComparer();
    public static DirectoryPathComparer Default
    {
        get { return instance; }
    }
}
