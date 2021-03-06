#tool nuget:?package=NUnit.ConsoleRunner&version=3.8.0

// Input args
string target = Argument("target", "Default");
string configuration = Argument("configuration", "Release");

// Define vars
var dirs = new[] 
{
    Directory("./build"),
    Directory("./Xamarin.Forms.Mocks/bin") + Directory(configuration),
    Directory("./Xamarin.Forms.Mocks.Tests/bin") + Directory(configuration),
    Directory("./Xamarin.Forms.Mocks.Xaml/bin") + Directory(configuration),
    Directory("./Xamarin.Forms.Mocks/obj") + Directory(configuration),
    Directory("./Xamarin.Forms.Mocks.Tests/obj") + Directory(configuration),
    Directory("./Xamarin.Forms.Mocks.Xaml/obj") + Directory(configuration),
};
string sln = "./Xamarin.Forms.Mocks.sln";
string version = "3.5.0.1";
string suffix = "";

Task("Clean")
    .Does(() =>
    {
        foreach (var dir in dirs)
            CleanDirectory(dir);
    });

Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        MSBuild(sln, settings => settings.SetConfiguration(configuration).WithRestore());
    });

Task("NUnit")
    .IsDependentOn("Build")
    .Does(() =>
    {
        NUnit3(dirs[2] + File("./net461/Xamarin.Forms.Mocks.Tests.dll"));
    });

Task("NuGet-Package")
    .IsDependentOn("NUnit")
    .Does(() =>
    {
        var settings   = new NuGetPackSettings
        {
            Verbosity = NuGetVerbosity.Detailed,
            Version = version + suffix,
            Files = new [] 
            {
                new NuSpecContent { Source = dirs[1] + File("netstandard2.0/Xamarin.Forms.Core.UnitTests.dll"), Target = "lib/netstandard2.0" },
                new NuSpecContent { Source = dirs[3] + File("netstandard2.0/Xamarin.Forms.Xaml.UnitTests.dll"), Target = "lib/netstandard2.0" },
            },
            OutputDirectory = dirs[0]
        };
            
        NuGetPack("./Xamarin.Forms.Mocks.nuspec", settings);
    });

Task("NuGet-Push")
    .Does(() =>
    {
        var apiKey = TransformTextFile ("./.nugetapikey").ToString();

        NuGetPush("./build/Xamarin.Forms.Mocks." + version + suffix + ".nupkg", new NuGetPushSettings 
        {
            Verbosity = NuGetVerbosity.Detailed,
            Source = "nuget.org",
            ApiKey = apiKey
        });
    });

Task("Default")
    .IsDependentOn("NuGet-Package");

RunTarget(target);