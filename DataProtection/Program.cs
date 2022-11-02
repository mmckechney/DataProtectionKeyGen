using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.CommandLine.NamingConventionBinder;

public class Program
{
    private static Option<int> dayOptions = new Option<int>(new string[]{"--days", "-d"}, "Number of days until key expiration"){ IsRequired = true};
    private static Option<DirectoryInfo> folderOption = new Option<DirectoryInfo>(new string[]{"--path", "-p"},  "Path to save key file to"){ IsRequired = true}.ExistingOnly();
    private static Option<bool> keepFilesOption = new Option<bool>(new string[]{"-k", "--keep-files"}, () => false, "Keep files in file system (vs. delete once complete");
    
    public static int Main(string[] args)
    {
        var rootCmd = new RootCommand("Generate a DataProtection Key File");
        rootCmd.Handler = CommandHandler.Create<int, DirectoryInfo, bool>(GenerateKeyFile);
        rootCmd.Add(dayOptions);
        rootCmd.Add(folderOption);
        rootCmd.Add(keepFilesOption);

        var parser = new CommandLineBuilder(rootCmd)
            .UseTypoCorrections()
            .UseDefaults()
            .UseHelp().Build();
        return parser.Invoke(args);
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Using DAPI, compile for Windows only")]
    private static int GenerateKeyFile(int days, DirectoryInfo path, bool keepFiles)
    {
        try
        {
            if (!keepFiles && path.Exists)
            {
                ClearFiles(path);
            }
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection()
                // point at a specific folder and use DPAPI to encrypt keys
                .PersistKeysToFileSystem(path)
                .ProtectKeysWithDpapi();
            var services = serviceCollection.BuildServiceProvider();

            //// get a reference to the key manager
            var keyManager = services.GetService<IKeyManager>();

            keyManager?.CreateNewKey(activationDate: DateTimeOffset.Now, expirationDate: DateTimeOffset.Now.AddDays(days));

            // list all keys in the key ring
            var allKeys = keyManager?.GetAllKeys();
            allKeys = allKeys?.OrderBy(k => k.CreationDate).Reverse().ToList();
            foreach (var key in allKeys) 
            {
                //Get only the newest active key file
                if (!key.IsRevoked && key.ExpirationDate > DateTime.Today)
                {
                    string keyFileContents = File.ReadAllText(Path.Combine(path.FullName, $"key-{key.KeyId}.xml"));
                    Console.WriteLine(keyFileContents);
                    break;
                }
            }
            
            if (!keepFiles)
            {
                ClearFiles(path);
            }
        }
        catch(Exception exe)
        {
            Console.Error.WriteLine($"Error creating key file: {exe.Message}");
            return -1;
        }
        return 0;
    }
    private static void ClearFiles(DirectoryInfo folderPath)
    {
        var keyFiles = folderPath.GetFiles("key-*.xml").ToList();
        keyFiles.AddRange(folderPath.GetFiles("revocation-*.xml").ToList());
        for(int i=0;i<keyFiles.Count;i++)
        {
            keyFiles[i].Delete();
        }
    }
}
