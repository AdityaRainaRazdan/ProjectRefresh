using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using Integration_Tool.main;

namespace Integration_Tool
{
    static class Program
    {
        private static Mutex mutex = null;
        private static LoginFrame frame = null;
        private static string toolLocation = "";
        private static string configurationsPath = "";
        [STAThread]
        static void Main()
        {
            configurationsPath = System.Configuration.ConfigurationManager.AppSettings["CONFIGURATIONS"];
            toolLocation = System.Configuration.ConfigurationManager.AppSettings["LATEST.TOOL.LOCATION"];
            if (toolLocation.Trim() == "" || !Directory.Exists(toolLocation))
            {
                toolLocation = @"\\wtintegration\WT_Integrators\tools\I-Tool";
            }

            String currentVersionString = ToolVersion.Version;
            String latestVersionString = getLatestVersion();
            int latestVersion = Int32.Parse(latestVersionString.Trim().Replace(".", ""));
            int currentVersion = Int32.Parse(currentVersionString.Trim().Replace(".", ""));
            
            if (latestVersion > 0 && latestVersion > currentVersion)
            {
                String message = "Your application running on old version \"" + currentVersionString + "\", Latset version of application \"" + latestVersionString + "\" is available now."
                +"\n Do you want to get new version ?";
                DialogResult option = MessageBox.Show(message, "Integration Tool, Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if(option.Equals(DialogResult.Yes))
                {
                    System.Diagnostics.Process.Start("explorer.exe", toolLocation);
                }
            }
            else
            {
                if (Directory.Exists(configurationsPath))
                {
                    startApplication();
                }
                else {
                    String message = "Tool unable to find the tool's configurations folder, Please configure the \"Configurations\" Key settings in \"IntegrationTool.exe.config\" properly and Try Again!";
                    MessageBox.Show(message, "Integration Tool, Error", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error);
                }
                
            }
        }
        static void startApplication()
        {
            const string appName = "Integration_Tool";
            bool createdNew;
            mutex = new Mutex(true, appName, out createdNew);
            if (!createdNew)
            {
                //app is already running! Exiting the application  
                MessageBox.Show("Appliation already in running!", "Integration Tool, Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            frame = new LoginFrame();
            Application.Run(frame);
            //Application.Run(new ProdToDev.CopyDeleteContentController());
        }
        static string getLatestVersion()
        {
            String versionXml = toolLocation+@"\Configurations\version\tool_version.xml";
            string latestVersion = "0";
            if (File.Exists(versionXml))
            {
                using (XmlReader reader = XmlReader.Create(versionXml))
                {
                      
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name.ToString())
                            {

                                case "version-number":
                                    latestVersion = reader.ReadString().Trim();
                                    break;
                            }

                        }

                    }

                }
            
        }
            return latestVersion;
        }
    }
}
