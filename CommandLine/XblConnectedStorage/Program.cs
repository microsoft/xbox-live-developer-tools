// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConnectedStorage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Xml;
    using CommandLine;
    using CommandLine.Text;
    using Microsoft.Xbox.Services.DevTools.Authentication;
    using Microsoft.Xbox.Services.DevTools.TitleStorage;

    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Options options = null;

            try
            {
                // Only assign the option and verb here, as the commandlineParser doesn't support async callback yet.
                ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(parsedOptions => 
                    {
                        options = parsedOptions;
                    });

                if (options == null)
                {
                    Console.Error.WriteLine("Parsing parameters error.");
                    return -1;
                }

                if (options.GamerTag.Contains("#"))
                {
                    Console.Error.WriteLine("Modern gamertags are not supported. Please use a legacy gamertag.");
                    return -1;
                }

                DevAccount account = ToolAuthentication.LoadLastSignedInUser();
                if (account == null)
                {
                    Console.Error.WriteLine("Didn't found dev sign in info, please use \"XblDevAccount.exe signin\" to initiate.");
                    return -1;
                }

                Console.WriteLine($"Using Dev account {account.Name} from {account.AccountSource}");

                return await OnDownload(options);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error: XblConnectedStorage failed.");

                if (ex.Message.Contains(Convert.ToString((int)HttpStatusCode.Unauthorized)))
                {
                    Console.WriteLine(
                        $"Unable to authorize the account with XboxLive service with scid : {options?.ServiceConfigurationId} and sandbox : {options?.Sandbox}, please contact your administrator.");
                }
                else if (ex.Message.Contains(Convert.ToString((int)HttpStatusCode.Forbidden)))
                {
                    Console.WriteLine(
                        "Your account doesn't have access to perform the operation, please contact your administrator.");
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }

                return -1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: unexpected error found.");
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }

        private static async Task<int> OnDownload(Options options)
        {
            string gamertag = options.GamerTag;
            Guid scid = options.ServiceConfigurationId;
            string sandbox = options.Sandbox;

            List<TitleBlobInfo> savedGameInfos = await ConnectedStorage.ListSavedGamesAsync(gamertag, scid.ToString(), sandbox, string.Empty, 0, 0);

            if (savedGameInfos.Count == 0)
            {
                Console.WriteLine("No saves found.");
                return 0;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(options.OutputFile));
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
            {
                Indent = true
            };
            using (XmlWriter xbStorageXmlWriter = XmlWriter.Create(options.OutputFile, xmlWriterSettings))
            {
                xbStorageXmlWriter.WriteStartDocument();
                xbStorageXmlWriter.WriteStartElement("XbConnectedStorageSpace");
                xbStorageXmlWriter.WriteStartElement("ContextDescription");
                xbStorageXmlWriter.WriteStartElement("Account");
                xbStorageXmlWriter.WriteAttributeString("msa", "change@me.com");
                xbStorageXmlWriter.WriteEndElement(); // Account
                xbStorageXmlWriter.WriteStartElement("Title");
                xbStorageXmlWriter.WriteAttributeString("scid", scid.ToString());
                xbStorageXmlWriter.WriteEndElement(); // Title
                xbStorageXmlWriter.WriteEndElement(); // ContextDescription
                xbStorageXmlWriter.WriteStartElement("Data");
                xbStorageXmlWriter.WriteStartElement("Containers");

                foreach (TitleBlobInfo savedGameInfo in savedGameInfos)
                {
                    string savedGameName = savedGameInfo.FileName.Substring(0, savedGameInfo.FileName.IndexOf(','));

                    xbStorageXmlWriter.WriteStartElement("Container");
                    xbStorageXmlWriter.WriteAttributeString("name", savedGameName);
                    xbStorageXmlWriter.WriteStartElement("Blobs");

                    SavedGameV2Response savedGame = await ConnectedStorage.DownloadSavedGameAsync(gamertag, scid, sandbox, savedGameName);

                    foreach (ExtendedAtomInfo atomInfo in savedGame.Atoms)
                    {
                        xbStorageXmlWriter.WriteStartElement("Blob");
                        xbStorageXmlWriter.WriteAttributeString("name", atomInfo.Name);

                        byte[] atomContent = await ConnectedStorage.DownloadAtomAsync(gamertag, scid, sandbox, atomInfo.Atom);

                        xbStorageXmlWriter.WriteCData(Convert.ToBase64String(atomContent));
                        xbStorageXmlWriter.WriteEndElement(); // Blob
                    }

                    xbStorageXmlWriter.WriteEndElement(); // Blobs
                    xbStorageXmlWriter.WriteEndElement(); // Container
                }

                xbStorageXmlWriter.WriteEndElement(); // Containers
                xbStorageXmlWriter.WriteEndElement(); // Data
                xbStorageXmlWriter.WriteEndElement(); // XbConnectedStorageSpace
            }

            Console.WriteLine($"Operation complete. Downloaded {savedGameInfos.Count} save containers to {options.OutputFile}.");

            return 0;
        }

        private class Options
        {
            [Option('c', "scid", Required = true,
                HelpText = "The service configuration ID (SCID) of the title")]
            public Guid ServiceConfigurationId { get; set; }

            [Option('s', "sandbox", Required = true,
                HelpText = "The target sandbox for title storage")]
            public string Sandbox { get; set; }

            [Option('g', "gamertag", Required = true,
                HelpText = "The legacy gamertag of the player to retrieve storage for")]
            public string GamerTag { get; set; }

            [Option('o', "output", HelpText = "The output file to write the xbstorage xml file to.", Default = "./output/xbstorage.xml")]
            public string OutputFile { get; set; }

            [Usage(ApplicationAlias = "XblConnectedStorage")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example(
                        @"Downloads the Connected Storage data for this user to ./output/xbstorage.xml",
                        new Options { ServiceConfigurationId = Guid.Parse("00000000-0000-0000-0000-0000628cd0f2"), Sandbox = "TEST.0", GamerTag = "CrazyGiraffe", OutputFile = @"./output/xbstorage.xml" });
                }
            }
        }
    }
}
