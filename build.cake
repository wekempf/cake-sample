#tool "nuget:?package=xunit.runner.console"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"
#tool coveralls.net

#addin Cake.Coveralls

var target = Argument("target", "Default");
var solutions = GetFiles("**/*.sln");

Task("Clean")
    .Does(() => {
        var directories = GetDirectories("**/bin")
            .Concat(GetDirectories("**/obj"))
            .Concat(GetDirectories("**/packages"));
        CleanDirectories(directories);
        if (DirectoryExists("./test_results")) {
            DeleteDirectory("./test_results", true);
        }
        if (FileExists("./coverage.xml")) {
            DeleteFile("./coverage.xml");
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
    .Does(() => {
        foreach (var sln in solutions) {
            MSBuild(sln);
        }
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
        var testAssemblies = GetFiles("**/bin/**/*.Tests.dll");
        OpenCover(tool => tool.XUnit2(testAssemblies, new XUnit2Settings { ShadowCopy = false }),
            new FilePath("./coverage.xml"),
            new OpenCoverSettings()
                .WithFilter("+[*]*")
                .WithFilter("-[*.Tests]*")
                .WithFilter("-[xunit.*]*"));
        ReportGenerator("./coverage.xml", "./test_results");
        // CoverallsNet("./coverage.xml", CoverallsNetReportType.OpenCover, new CoverallsSettings {
        //      RepoToken = "abdef"
        //});
    });

Task("Default")
    .IsDependentOn("Test");

RunTarget(target);