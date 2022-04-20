using Newtonsoft.Json;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ROCKET
{
    internal class Program
    {
        #region Exception
        static void FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (connectedToLIFTOFF)
            {
                writer.WriteLine("false," + e.Exception.ToString());
                writer.Flush();

                if (SteamClient.IsValid) SteamClient.Shutdown();
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine(e.Exception.ToString());
                if (SteamClient.IsValid) SteamClient.Shutdown();
                Environment.Exit(0);
            }
        }
        #endregion

        #region Main
        static NamedPipeClientStream client;
        static StreamWriter writer;
        static bool connectedToLIFTOFF = false;
        private static List<Tuple<string, string, string, string, string>> CMDArgs = new List<Tuple<string, string, string, string, string>>();


        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.FirstChanceException += FirstChanceException;

            string[] tmpArgs = string.Join(" ", args).Split('#', StringSplitOptions.RemoveEmptyEntries);

            Console.WriteLine("[" + DateTime.Now + "] ROCKET: ARGUMENTS: " + tmpArgs[0]);

            foreach (string arg in tmpArgs)
            {
                string[] argParts = arg.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                List<string> argPartList = new List<string>(Enumerable.Repeat("", 5));

                int index = 0;
                foreach (string tmpArg in argParts)
                {
                    argPartList[index] = tmpArg;
                    index++;
                }

                Tuple<string, string, string, string, string> newArg = new Tuple<string, string, string, string, string>(argPartList[0], argPartList[1], argPartList[2], argPartList[3], argPartList[4]);
                CMDArgs.Add(newArg);

                //Argument handler
                switch (newArg.Item1)
                {
                    case "join":
                        JoinServer(newArg.Item2, uint.Parse(newArg.Item3), newArg.Item4, newArg.Item5);
                        break;
                    case "getmods":
                        GetMods(uint.Parse(newArg.Item2));
                        break;
                }
            }
            Console.ReadLine();
        }
        #endregion

        #region Functions

        static async Task GetMods(uint appID)
        {
            Console.WriteLine("[" + DateTime.Now + "] ROCKET: Getting mods...");
            Console.WriteLine("[" + DateTime.Now + "] ROCKET: Subscribing via Steam Workshop...");

            SteamClient.Init(appID);
        }


        static async Task JoinServer(string ipAndPort, uint appID, string filename, string arguments)
        {
            Console.WriteLine("[" + DateTime.Now + "] ROCKET: Joining server... (IP: " + ipAndPort + " | AppID: " + appID + ")");
            Console.WriteLine("[" + DateTime.Now + "] ROCKET: File Location: " + filename);

            await Task.Factory.StartNew(() =>
            {
                Console.WriteLine("[" + DateTime.Now + "] ROCKET: Connecting to Named Pipe...");
                client = new NamedPipeClientStream("LIFTOFF");
                client.Connect();
                Console.WriteLine("[" + DateTime.Now + "] ROCKET: Connected to LIFTOFF!");
                connectedToLIFTOFF = true;
                writer = new StreamWriter(client);
            });

            Console.WriteLine("[" + DateTime.Now + "] ROCKET: Connecting to Steam...");
            SteamClient.Init(appID);

            Process game = new Process();

            game.StartInfo.FileName = SteamApps.AppInstallDir(appID) + "\\" + filename;
            game.StartInfo.Arguments = "-connect=" + ipAndPort + " " + arguments;

            Console.WriteLine("[" + DateTime.Now + "] ROCKET: Starting game...");
            game.Start();

            Console.WriteLine("[" + DateTime.Now + "] ROCKET: Informing LIFTOFF of successful launch...");
            writer.WriteLine("true");
            writer.Flush();

            Console.WriteLine("[" + DateTime.Now + "] ROCKET: Disconnecting from Steam...");
            SteamClient.Shutdown();

            Console.WriteLine("[" + DateTime.Now + "] ROCKET: Waiting for game to close...");
            await game.WaitForExitAsync();

            Console.WriteLine("[" + DateTime.Now + "] ROCKET: All work done, closing...");
            Environment.Exit(0);
        }
        #endregion
    }
}
