var target = Argument("target", "Default");
var solutions = GetFiles("**/*.sln");

Task("Clean")
    .Does(() => {
        var directories = GetDirectories("**/bin")
            .Concat(GetDirectories("**/obj"));
        CleanDirectories(directories);
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

Task("Default")
    .IsDependentOn("Build");

RunTarget(target);