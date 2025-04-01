using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.GameServer.Engine;
using System.IO;
using Newtonsoft.Json;
using System.Text;

namespace WindowsGSM.Plugins
{
    public class Wreckfest2 : SteamCMDAgent
    {
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.Wreckfest2", // WindowsGSM.XXXX
            author = "raziel7893",
            description = "WindowsGSM plugin for supporting Wreckfest2 Dedicated Server",
            version = "1.0.2",
            url = "https://github.com/Raziel7893/WindowsGSM.Wreckfest2", // Github repository link (Best practice) TODO
            color = "#34FFeb" // Color Hex
        };

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => true;
        public override string AppId => "3519390"; // Game server appId Steam

        // - Standard Constructor and properties
        public Wreckfest2(ServerConfig serverData) : base(serverData) => base.serverData = serverData;

        // - Game server Fixed variables
        //public override string StartPath => "Wreckfest2Server.exe"; // Game server start path
        public override string StartPath => "Wreckfest2.exe";
        public string FullName = "Wreckfest2 Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation

        // - Game server default values
        public string Port = "30100"; // Default port

        public string Additional = "--server --save-dir=d:\\MyServer"; // Additional server start parameter

        // TODO: Following options are not supported yet, as ther is no documentation of available options
        public string Maxplayers = "16"; // Default maxplayers        
        public string QueryPort = "27015"; // Default query port. This is the port specified in the Server Manager in the client UI to establish a server connection.
        // TODO: Unsupported option
        public string Defaultmap = "747"; // Default map name
        // TODO: Undisclosed method
        public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()



        // - Create a default cfg for the game server after installation
        public async void CreateServerCFG()
        {
            string gameContent = $@"{{
    ""bbmeta"": ""scnf v0 n1"",
    ""net server config"": [
        {{
            ""bbmeta"": ""ncnf v0 n1"",
            ""name"": ""{serverData.ServerName}"",
            ""description"": ""This is a dedicated server"",
            ""game port"": {serverData.ServerPort}
        }}
    ],
    ""game server config"": [
        {{
            ""bbmeta"": ""gcnf v0 n1"",
            ""event rotation name"": """",
            ""default event"": [
                {{
                    ""bbmeta"": ""ecnf v0 n1"",
                    ""level"": [
                        {{
                            ""bbmeta"": ""mlvl v1 n1"",
                            ""level id"": ""track07_1"",
                            ""weather path"": """",
                            ""ai set path"": """",
                            ""game mode id"": """"
                        }}
                    ],
                    ""rules"": [
                        {{
                            ""bbmeta"": ""evru v4 n1"",
                            ""laps"": 3,
                            ""time limit"": 3,
                            ""number of teams"": 2,
                            ""max number of participants"": 24,
                            ""flags"": """",
                            ""car reset delay (seconds)"": 5,
                            ""vehicle damage id"": ""normal""
                        }}
                    ],
                    ""bot count"": 8
                }}
            ],
            ""countdown time"": 100000,
            ""flags"": ""leader enabled""
        }}
    ]
}}";
            string gameIniFile = Functions.ServerPath.GetServersServerFiles(serverData.ServerID, "server_config.scnf");
            File.WriteAllText(gameIniFile, gameContent);
        }

        // - Start server function, return its Process to WindowsGSM
        public async Task<Process> Start()
        {
            string shipExePath = Functions.ServerPath.GetServersServerFiles(serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }

            //Try gather a password from the gui
            var sb = new StringBuilder(serverData.ServerParam);
            //no real parameters to pass, everything seems config based

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = false,
                    WorkingDirectory = ServerPath.GetServersServerFiles(serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = sb.ToString(),
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (serverData.EmbedConsole)
            {
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.CreateNoWindow = true;
                var serverConsole = new ServerConsole(serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
            }

            // Start Process
            try
            {
                p.Start();
                if (serverData.EmbedConsole)
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                }
                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }

        // - Stop server function
        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                Functions.ServerConsole.SendWaitToMainWindow("^c");
                p.WaitForExit(2000);
                if (!p.HasExited)
                    p.Kill();
            });
        }
    }
}
