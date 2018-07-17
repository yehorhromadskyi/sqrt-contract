#addin Cake.FileHelpers
#addin Cake.Npm

var target = Argument("target", "Default");
var contract = "Bravo";
var configuration = Argument("configuration", "Release");

Task("Clean")
    .Does(() =>
    {
        var settings = new DotNetCoreCleanSettings
        {
            Configuration = configuration
        };

        DotNetCoreClean("./", settings);
        CleanDirectory(Directory("bin"));
    });

Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        Information("Configuration: " + configuration + "\r\n");

        var settings = new DotNetCoreBuildSettings
        {
            Configuration = configuration
        };

        DotNetCoreBuild("./", settings);
    });

Task("Npm")
    .IsDependentOn("Build")
    .Does(() =>
    {
        Information("Installing solidity compiler\r\n");

        NpmInstall(settings => settings.AddPackage("solc").InstallGlobally());
    });

Task("Contract")
    .IsDependentOn("Npm")
    .Does(() => 
    {
        Information("Compiling " + contract + ".sol " + "contract\r\n");

        StartProcess("powershell", new ProcessSettings 
        {
            Arguments = new ProcessArgumentBuilder()
                .Append(@"solcjs --bin contracts/{0}.sol -o bin;", contract)
                .Append(@"solcjs --abi contracts/{0}.sol -o bin;", contract)
        });
    });

Task("Test")
    .IsDependentOn("Contract")
    .Does(() => 
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new System.Diagnostics.Process { StartInfo = startInfo };
        process.Start();

        var testchainDir = MakeAbsolute(Directory("./testchain"));

        process.StandardInput.WriteLine("cd " + testchainDir);

        var startgeth = "sh " + testchainDir + "/startgeth.sh";
        Information("Run geth: " + startgeth + "\r\n");

        process.StandardInput.WriteLine(startgeth);
        
        var settings = new DotNetCoreTestSettings
        {
            Configuration = configuration
        };

        DotNetCoreTest("./", settings);

        // exit geth
        process.StandardInput.WriteLine("exit");
        // exit powershell
        process.StandardInput.WriteLine("exit");
        process.WaitForExit();
    });

Task("Default")
    .IsDependentOn("Test");

RunTarget(target);