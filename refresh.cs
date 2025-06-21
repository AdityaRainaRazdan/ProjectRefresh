using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Integration_Tool.ProjectRefresh.Frames;
using Integration_Tool.main;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using System.Configuration;
using System.Threading;
using Integration_Tool.I_Tool;
using System.Text.RegularExpressions;
using Integration_Tool;
using System.Security.AccessControl;
using Integration.Tool.Common;
using Microsoft.SqlServer.Management.Common;

namespace Project_Refresh
{
    public partial class ProjectRefreshFrame : Form
    {

        private String configurationPath = ConfigurationManager.AppSettings["CONFIGURATIONS"];
        //private String scriptPath = ConfigurationManager.AppSettings["CONFIGURATIONS"] + @"\sanitization_scripts\";
        private String processingIcon = @".\Configurations\images\progress.png";
        private String scriptPath = @".\Configurations\sanitization_scripts\";
        private String CompletedIcon = @".\Configurations\images\success.png";
        private String ErrorIcon = @".\Configurations\images\error.png";
        private String Queue = @".\Configurations\images\queued.png";
        private String Processing = @".\Configurations\images\started.png";
        private String Processed = @".\Configurations\images\completed.png";
        private string localEXEPath = ConfigurationManager.AppSettings["LOCAL.INTERNAL.EXE"];
        private bool allStepsPassed = true;
        //private System.Threading.Timer timer;
        //private const string commandText = "SELECT session_id, command, percent_complete FROM sys.dm_exec_requests WHERE command = 'RESTORE DATABASE'";
        string Status;
        string a;
        int count;
        List<string> databases = new List<string> { };
        Connection con = new Connection();

        public ProjectRefreshFrame()
        {

            InitializeComponent();
            groupBoxTruncate.Visible = false;
            InfoText.Text = "";
            resetfnc();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {
            buttonPreview.Enabled = false;
        }
        private void groupbox1_valid(object sender, EventArgs e)
        {
            if (textBoxTargetSrvrInput.Text != "" && textBoxDumpLoc.Text != "")
            {
                buttonPreview.Enabled = true;
            }
            else
            {
                buttonPreview.Enabled = false;
            }
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog diag = new FolderBrowserDialog();
            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxDumpLoc.Text = diag.SelectedPath;
            }
            else
            {
                textBoxDumpLoc.Text = " ";
            }
        }

        private void Preview(object sender, EventArgs e)
        {

            checkedListBoxDbs.Items.Clear();
            //  textBoxProjectOut.Text = "";

            if (comboBoxProduct.Text == "Select The Product")
            {
                MessageBox.Show("Please Select the Product");

            }
            else
            {
                try
                {

                    resetRefreshProcessSteps();
                    groupBox2.Show();
                    groupBox3.Hide();

                    DataFileFolderTxt.Text = "D:\\Database Files";
                    LogFileFolderTxt.Text = "D:\\Database Files";
                    DataFileFolderTxt.Enabled = false;
                    LogFileFolderTxt.Enabled = false;
                    //var filePaths = Directory.GetFiles(textBoxDumpLoc.Text, "*.bak");
                    var filePaths = Directory.GetFiles(textBoxDumpLoc.Text.Trim());
                    if (textBoxDumpLoc.Text.Trim().Contains(":"))
                    {
                        MessageBox.Show("Error : D drive location are not allowed in Dumps location", "Invalid Location");
                        return;
                    }
                    foreach (string filepath in filePaths)
                    {
                        string filename = Path.GetFileName(filepath);
                        //string filename = Path.GetFileNameWithoutExtension(filepath);
                        if (filename.EndsWith(".bak") || filename.EndsWith("") && !filename.EndsWith(".json") && !filename.EndsWith(".txt") && !filename.EndsWith(".ini") && !filename.EndsWith(".odt") && !filename.EndsWith(".sql"))
                        {
                            string files = Path.GetFileNameWithoutExtension(filepath);
                            checkedListBoxDbs.Items.Add(files);
                        }
                        textBoxProjectOut.Text = filename.Split('_')[0];
                    }
                }
                catch (System.UnauthorizedAccessException)
                {
                    groupBox2.Hide();
                    groupBox3.Hide();
                    MessageBox.Show("You do not have access to this location..!");
                }
                catch (Exception)
                {
                    groupBox2.Hide();
                    groupBox3.Hide();
                    MessageBox.Show("Please Enter a valid Location");
                }
            }
        }

private void buttonAudit_Click(object sender, EventArgs e)
        {
            AuditTrailDataFrame atd = new AuditTrailDataFrame();
            atd.ShowDialog();
        }

        private void checkedListBoxDbs_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            String itemname = checkedListBoxDbs.Items[e.Index].ToString();
            if (!itemname.Contains("archive_sa") && itemname.Contains("archive"))
            {
                groupBoxTruncate.Visible = e.NewValue == CheckState.Checked;
            }
            else
            {
                radioBtnDisable.Checked = true;
            }

        }

        private void resetfnc()
        {

            textBoxDumpLoc.Clear();
            textBoxTargetSrvrInput.Clear();
            comboBoxProduct.Text = "Select The Product";
            textBoxProjectOut.Clear();
            checkedListBoxDbs.Items.Clear();
            resetRefreshProcessSteps();
            DataFileFolderTxt.Text = "D:\\Database Files";
            LogFileFolderTxt.Text = "D:\\Database Files";
            DataFileFolderTxt.Enabled = false;
            LogFileFolderTxt.Enabled = false;
            btnDBbackup.Enabled = true;

            groupBox2.Hide();
            groupBox3.Hide();


        }
        private void resetRefreshProcessSteps()
        {
            pictureBoxPre.Image = null;
            pictureBox2.Image = null;
            pictureBoxrestore.Image = null;
            pictureboxpost.Image = null;
            postpicturebox.Image = null;
            labelpre.Text = "";
            Verificationprestatus.Text = "";
            lblStatus.Text = "";
            labelpost.Text = "";
            poststatuspermission.Text = "";
            restore1.Image = null;
            restore2.Image = null;
            restore3.Image = null;
            restore4.Image = null;
            restore5.Image = null;
            restore6.Image = null;
            dbrestorename1.Text = "";
            dbrestorename2.Text = "";
            dbrestorename3.Text = "";
            dbrestorename4.Text = "";
            dbrestorename5.Text = "";
            dbrestorename6.Text = "";

        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {

            DataTable dataTable = SmoApplication.EnumAvailableSqlServers(true);
            btnDBbackup.Enabled = false;
            resetRefreshProcessSteps();
            groupBox3.Show();
            RefreshComplete.Hide();
            string username = UserCredentials.getUser();
            string password = UserCredentials.getPassword();
            string sqlserver = textBoxTargetSrvrInput.Text;
            //  SqlConnection sa = new SqlConnection("Data Source=" + sqlserver + ";User Id=" + username + ";Password=" + password + ";");
            SqlConnection sa = new SqlConnection("Data Source=" + sqlserver + ";Integrated Security=True");
            SqlCommand time = new SqlCommand();
            time.CommandTimeout = 300;

            int chkFlag = 0;

            allStepsPassed = true;

            if (textBoxDumpLoc.Text.Trim().Contains(":"))
            {
                MessageBox.Show("Error : D drive location are not allowed in Dumps location", "Invalid Location");
                return;
            }

            //get checked DB's in list
            List<string> checkedDb = new List<string>();
            for (int i = 0; i < checkedListBoxDbs.Items.Count; i++)
            {
                if (checkedListBoxDbs.GetItemChecked(i))
                {

                    checkedDb.Add(checkedListBoxDbs.Items[i].ToString());

                    chkFlag = 1;
                }

            }
            //foreach (String checkedListBoxDbs in checkedDb)
            //{
            //    RestortionProcessing(sa, checkedListBoxDbs);
            //}
            if (chkFlag == 0)
            {
                groupBox3.Hide();
                MessageBox.Show("Please review the database");
            }
            else
            {
                string Serverpath;
                if (textBoxTargetSrvrInput.Text.Contains("\\"))
                {
                    String Serverpath1 = "\\\\" + textBoxTargetSrvrInput.Text.Split('\\')[0].ToUpper() + "\\code_store\\";
                    Serverpath = Serverpath1;
                }
                else
                {
                    String Serverpath1 = "\\\\" + textBoxTargetSrvrInput.Text + "\\d\\";
                    Serverpath = Serverpath1;
                }

                if (Directory.Exists(Serverpath))
                {
                    String FolderpathRename = Serverpath + textBoxProjectOut.Text;

                    if (!Directory.Exists(FolderpathRename))
                    {
                        MessageBox.Show("Project Path " + FolderpathRename + " Rename is completed");

                        if (RenameFolder())
                        {
                            // MessageBox.Show("Let's test  your folder Renaming :)");
                            //MessageBox.Show("In db server,Prepare a cmd prompt(do NOT rename manually)Enter in ‘ D: ’ to navigate to the D: driveType(do NOT enter): ren Projectname projectname_rename", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            // renameBackFolder();
                            try
                            {

                                sa.Open();
                                DateTime startTime = DateTime.Now;
                                
                                ////MessageBox.Show("You did a great job with folder renaming.....ConnectionEstablished with server Successfully", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                List<string> productList = new List<string>();
                                StringBuilder sb = new StringBuilder();
                                foreach (string name in comboBoxProduct.Items)
                                {
                                    sb.Append(name);
                                    sb.Append(" ");
                                }
                                // MessageBox.Show(productList.ToString());
                                string selectedItem = comboBoxProduct.Items[comboBoxProduct.SelectedIndex].ToString();


                                if (comboBoxProduct.SelectedItem.ToString() != "")
                                {
                                    int abc = checkedDb.Count;
                                    for (int i = 0; i < abc; i++)
                                    {
                                        string finalDb7 = checkedDb[i].Replace("_full_dump", "");
                                        string newStr7 = finalDb7.Replace("_1", "");
                                        string newStr9 = newStr7.Replace("_0", "");
                                        string newStr10 = newStr9.Replace("_2", "");
                                        string newStr11 = newStr10.Replace("_3", "");
                                        string newStr12 = newStr11.Replace("_4", "");
                                        string newStr13 = newStr12.Replace("_5", "");
                                        string newStr14 = newStr13.Replace("_6", "");
                                        string newStr15 = newStr14.Replace("_7", "");
                                        string newStr16 = newStr15.Replace("_8", "");
                                        string newStr8 = newStr16.Replace("_9", "");
                                        string finalDb1 = newStr8;
                                        Image image1 = Image.FromFile(Queue);
                                        if (i == 0)
                                        {
                                            dbrestorename1.Text = newStr8;
                                            restore1.Image = image1;
                                        }
                                        else
                                        {
                                            if (i == 1)
                                            {
                                                dbrestorename2.Text = newStr8;
                                                restore2.Image = image1;
                                            }
                                            else
                                            {
                                                if (i == 2)
                                                {
                                                    dbrestorename3.Text = newStr8;
                                                    restore3.Image = image1;
                                                }
                                                else
                                                {
                                                    if (i == 3)
                                                    {
                                                        dbrestorename4.Text = newStr8;
                                                        restore4.Image = image1;
                                                    }
                                                    else
                                                    {
                                                        if (i == 4)
                                                        {
                                                            dbrestorename5.Text = newStr8;
                                                            restore5.Image = image1;
                                                        }
                                                        else
                                                        {
                                                            if (i == 5)
                                                            {
                                                                dbrestorename6.Text = newStr8;
                                                                restore6.Image = image1;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }




                                    //pre script !!
                                    Logger.InitializeLogFile(textBoxProjectOut.Text);
                                    try
                                    {
                                        string scriptName = scriptPath + selectedItem + "-" + "pre" + "-sanitization.sql";
                                        Logger.Log("PRE-Script", $"Starting....");
                                        Task.Delay(50).Wait();


                                        Image image = Image.FromFile(processingIcon);

                                        pictureBoxPre.Image = image;
                                        Task.Delay(50).Wait();
                                        labelpre.Text = "Processing..";
                                        Task.Delay(50).Wait();

                                        ExecutePrePostSQLFiles(scriptName);

                                        Image images = Image.FromFile(CompletedIcon);
                                        pictureBoxPre.Image = images;
                                        //Thread.Sleep(1000);
                                        labelpre.Text = "Completed..";
                                        
                                        Task.Delay(50).Wait();
                                        Logger.Log("PRE-Script", $" Pre Script Executed successfully.");
                                        // MessageBox.Show("pre script run successfully!!", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }

                                     catch (Exception ex)
                                    {
                                        Image images = Image.FromFile(ErrorIcon);
                                        pictureBoxPre.Image = images;
                                        labelpre.Text = "Failed..";
                                        Logger.LogError("PRE-Script", ex);
                                        MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        allStepsPassed = false;
                                        string Status = allStepsPassed ? "Pass" : "Fail";
                                        DateTime endTime = DateTime.Now;
                                        Audit(username, sa, checkedDb, startTime, selectedItem, textBoxProjectOut.Text, count, a, endTime, Status);
                                        return;
                                    }
                                }
                                    try
                                    {
                                    Verificationprestatus.Text = "Processing..";
                                    Image image5 = Image.FromFile(processingIcon);
                                    pictureBox2.Image = image5;
                                    Task.Delay(50).Wait();
                                    string scriptName1 = scriptPath + "production_cloning_script.sql";

                                    ExecutePrePostSQLFiles(scriptName1);

                                    Task.Delay(50).Wait();
                                    }
                                    catch
                                    {
                                    pictureBox2.Image = Image.FromFile(ErrorIcon);
                                    allStepsPassed = false;
                                    string Status = allStepsPassed ? "Pass" : "Fail";
                                    DateTime endTime = DateTime.Now;
                                    Audit(username, sa, checkedDb, startTime, selectedItem, textBoxProjectOut.Text, count, a, endTime, Status);
                                    return;
                                    //throw;
                                    }

                                //DB UserPermissions
                                using (SqlCommand cmd = new SqlCommand("usp_ITool_SET_DBUserPermissions", sa))
                                {
                                    try
                                    {
                                        Logger.Log("PRE-Permission", $"Starting Permission check");
                                        cmd.CommandType = CommandType.StoredProcedure;




                                        Image images7 = Image.FromFile(CompletedIcon);
                                        pictureBox2.Image = images7;
                                        //Thread.Sleep(1000);
                                        Verificationprestatus.Text = "Completed..";
                                        //MessageBox.Show("Database permission pre done", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        Task.Delay(50).Wait();
                                        Logger.Log("PRE-permission", $"Premission granted ");
                                        //MessageBox.Show("Restoration start", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.LogError("PRE-permission", ex);
                                        Verificationprestatus.Text = "Failed to set DB permissions.";
                                        MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        Verificationprestatus.Text = "Failed..";
                                        Image image5 = Image.FromFile(ErrorIcon);
                                        pictureBox2.Image = image5;
                                        allStepsPassed = false;
                                        string Status = allStepsPassed ? "Pass" : "Fail";
                                        DateTime endTime = DateTime.Now;
                                        Audit(username, sa, checkedDb, startTime, selectedItem, textBoxProjectOut.Text, count, a, endTime, Status);
                                        return;
                                        //throw;
                                    }

                                    // DB Restoration start
                                    string projectName = textBoxProjectOut.Text;
                                    string floc = textBoxDumpLoc.Text.Trim() + "\\";
                                    int xyz = checkedDb.Count;

                                    try
                                    {
                                        Logger.Log("Restore", $"starting restoring");
                                        for (int i = 0; i < xyz; i++)
                                        {
                                            count = i;


                                            string str = checkedDb[i];
                                            string newStr1 = str.Replace("_full_dump", "");
                                            string finalDb3 = checkedDb[i].Replace("_full_dump", "");
                                            string finalDb2 = finalDb3.Replace("_1", "");
                                            string newStr9 = finalDb2.Replace("_0", "");
                                            string newStr10 = newStr9.Replace("_2", "");
                                            string newStr11 = newStr10.Replace("_3", "");
                                            string newStr12 = newStr11.Replace("_4", "");
                                            string newStr13 = newStr12.Replace("_5", "");
                                            string newStr14 = newStr13.Replace("_6", "");
                                            string newStr15 = newStr14.Replace("_7", "");
                                            string newStr16 = newStr15.Replace("_8", "");
                                            string newStr = newStr16.Replace("_9", "");
                                            a = newStr;
                                            string finalDb1 = newStr;
                                            string newstr2 = projectName + "_archive_sa";
                                            string newstr3 = projectName + "_websight";
                                            string newstr4 = floc + checkedDb[i] + ".bak";
                                            string sql1 = $"USE [OATI] " +
                                                    $"DECLARE @return_value int " +
                                                    $"EXEC @return_value = [dbo].[usp_ITool_DBUserPermissions] " +
                                                    $"@action = N'GET'," +
                                                    $"@database = N'{newStr}'," +
                                                    $"@islocal = N'1' ";

                                            Logger.Log("SQl", $"Executing:{sql1}");

                                            using (SqlCommand command1 = new SqlCommand(sql1, sa))
                                            {
                                                command1.CommandTimeout = 6000;
                                                command1.ExecuteNonQuery();
                                            }


                                            string sql2 = $"USE [OATI] " +
                                                    $"ALTER DATABASE {newStr} SET SINGLE_USER WITH ROLLBACK IMMEDIATE " +
                                                    $"ALTER DATABASE {newStr} MODIFY NAME = {newStr}_1";



                                            using (SqlCommand command1 = new SqlCommand(sql2, sa))
                                            {
                                                command1.CommandTimeout = 6000;
                                                command1.ExecuteNonQuery();
                                            }

                                            string sql3 = $"USE [OATI] " +
                                                    $"ALTER DATABASE {newStr}_1 MODIFY NAME = {newStr} " +
                                                    $"ALTER DATABASE {newStr} SET MULTI_USER";

                                            using (SqlCommand command1 = new SqlCommand(sql3, sa))
                                            {
                                                command1.CommandTimeout = 6000;
                                                command1.ExecuteNonQuery();
                                            }
                                            Image image2 = Image.FromFile(Processing);
                                            if (i == 0)
                                            {
                                                restore1.Image = image2;
                                            }
                                            else
                                            {
                                                if (i == 1)
                                                {
                                                    restore2.Image = image2;
                                                }
                                                else
                                                {
                                                    if (i == 2)
                                                    {
                                                        restore3.Image = image2;
                                                    }
                                                    else
                                                    {
                                                        if (i == 3)
                                                        {
                                                            restore4.Image = image2;
                                                        }
                                                        else
                                                        {
                                                            if (i == 4)
                                                            {
                                                                restore5.Image = image2;
                                                            }
                                                            else
                                                            {
                                                                if (i == 5)
                                                                {
                                                                    restore6.Image = image2;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            string settings_query = $"SELECT  is_broker_enabled,is_trustworthy_on FROM sys.databases WHERE name='{finalDb1}';";
                                            using (SqlCommand command = new SqlCommand(settings_query, sa))
                                            {
                                                SqlDataReader reader = command.ExecuteReader();
                                                if (reader.HasRows)
                                                {
                                                    reader.Read();
                                                    bool isBrokerEnabledCurrent = (bool)reader["is_broker_enabled"] /*? 1:0 */;
                                                    bool isTrustworthyOnCurrent = (bool)reader["is_trustworthy_on"] /*? 1:0 */;
                                                    lblbrok.Text = isBrokerEnabledCurrent.ToString();
                                                    lbltrst.Text = isTrustworthyOnCurrent.ToString();
                                                    lblbrok.Hide();
                                                    lbltrst.Hide();
                                                }
                                                reader.Close();
                                            }
                                            //Task.Delay(50).Wait();
                                            lblStatus.Text = "Processing..";
                                            Image image = Image.FromFile(processingIcon);
                                            pictureBoxrestore.Image = image;
                                            //Task.Delay(50).Wait();
                                            for (int j = 0; j < checkedListBoxDbs.Items.Count; j++)
                                            {
                                                if (checkedListBoxDbs.GetItemChecked(j))
                                                {

                                                    checkedDb.Add(checkedListBoxDbs.Items[j].ToString());

                                                    chkFlag = 1;
                                                }

                                            }
                                            //foreach (String checkedListBoxDbs in checkedDb)
                                            //{
                                            //    RestortionProcessing(sa, checkedListBoxDbs);
                                            //}
                                            if (chkFlag == 0)
                                            {
                                                MessageBox.Show("Please review the database");
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    await Task.Run(() =>
                                                    {
                                                        //Thread.Sleep(8000);
                                                        RestorationStep(sa, checkedDb, floc, i, newStr, newstr4);
                                                    });
                                                    Image images2 = Image.FromFile(CompletedIcon);
                                                    pictureBoxrestore.Image = images2;
                                                    lblStatus.Text = "Completed..";
                                                    Logger.Log("Restore", $"restoration completed");
                                                }
                                                catch
                                                {
                                                    pictureBoxrestore.Image = Image.FromFile(ErrorIcon);
                                                    allStepsPassed = false;
                                                    string Status = allStepsPassed ? "Pass" : "Fail";
                                                    DateTime endTime = DateTime.Now;
                                                    Audit(username, sa, checkedDb, startTime, selectedItem, textBoxProjectOut.Text, count, a, endTime, Status);
                                                    return;
                                                    //throw;
                                                }
                                            }
                                            if (radioBtnEnble.Checked && newStr.Contains("archive") && !newStr.Contains("archive_sa"))
                                            {

                                                //string scriptName1 = scriptPath + "cloning" + "-" + "pre" + "-sanitization.sql";
                                                string scriptNameTrunc = scriptPath + "Truncate-Archive-database-Hailu.sql";
                                                // MessageBox.Show("truncate script started");
                                                ExecutePrePostSQLFiles(scriptNameTrunc);
                                                // MessageBox.Show("truncate script ended");
                                            }
                                            if (radioBtnDisable.Checked)
                                            {
                                            }
                                            try
                                            {
                                                try
                                                {
                                                    await Task.Run(() =>
                                                    {
                                                        DateTime endTime = DateTime.Now;
                                                        PermissionVerification(username, sa, checkedDb, startTime, selectedItem, projectName, i, finalDb1, endTime, Status);
                                                    });
                                                }
                                                catch
                                                {
                                                    postpicturebox.Image = Image.FromFile(ErrorIcon);
                                                    allStepsPassed = false;
                                                    string Status = allStepsPassed ? "Pass" : "Fail";
                                                    DateTime endTime = DateTime.Now;
                                                    Audit(username, sa, checkedDb, startTime, selectedItem, textBoxProjectOut.Text, count, a, endTime, Status);
                                                    return;
                                                }
                                                try
                                                {
                                                    await Task.Run(() =>
                                                    {
                                                        PostScriptStep(sa, checkedDb, selectedItem, textBoxProjectOut.Text, textBoxDumpLoc.Text.Trim() + "\\", checkedDb.Count);
                                                    });
                                                    RefreshComplete.Show();
                                                    btnDBbackup.Enabled = true;
                                                    //MessageBox.Show("Post script run successfully", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                                    //MessageBox.Show("Backup restore successfully....  :(", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                                    //MessageBox.Show("Backup restore successfully....  :(", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                    renameBackFolder();
                                                }
                                                catch
                                                {
                                                    pictureboxpost.Image = Image.FromFile(ErrorIcon);
                                                    allStepsPassed = false;
                                                    string Status = allStepsPassed ? "Pass" : "Fail";
                                                    DateTime endTime = DateTime.Now;
                                                    Audit(username, sa, checkedDb, startTime, selectedItem, textBoxProjectOut.Text, count, a, endTime, Status);
                                                    return;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.LogError("Restore", ex);
                                                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                allStepsPassed = false;
                                                string Status = allStepsPassed ? "Pass" : "Fail";
                                                DateTime endTime = DateTime.Now;
                                                Audit(username, sa, checkedDb, startTime, selectedItem, textBoxProjectOut.Text, count, a, endTime, Status);
                                                return;
                                            }
                                        }
                                    }

                                    catch (Exception ex)
                                    {
                                        Logger.LogError("Restore", ex);
                                        MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        allStepsPassed = false;
                                        string Status = allStepsPassed ? "Pass" : "Fail";
                                        DateTime endTime = DateTime.Now;
                                        Audit(username, sa, checkedDb, startTime, selectedItem, textBoxProjectOut.Text, count, a, endTime, Status);
                                        return;
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            try
                            {
                                // MessageBox.Show("Rename your folder before Proceeding....  :(", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message, "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Check for Project Folder Rename in server, you can use this command for renaming in cmd : " +
                            " ren " + textBoxProjectOut.Text + " " + textBoxProjectOut.Text + "_rename ");
                    }

                }
                else
                {
                    MessageBox.Show("User don't have permission for D: Drive");
                }
            }
            
        }
        private void RestorationStep(SqlConnection sa, List<string> checkedDb, string floc, int i, string newStr, string newstr4)
        {
            try
            {
                // Logger.Log("Restore", $"starting restore of {newStr}");

                sa.InfoMessage += (sender2, e2) =>
                {

                    foreach (SqlError err in e2.Errors)
                    {
                        Logger.Log("Restore-progress", err.Message);
                    }
                };

                sa.FireInfoMessageEventOnUserErrors = true;
                if (File.Exists(newstr4))
                {
                    string sql = $"ALTER DATABASE {newStr} SET SINGLE_USER WITH ROLLBACK IMMEDIATE " +
                                 $" RESTORE DATABASE {newStr} FROM DISK='{floc}{checkedDb[i]}.bak' " +
                                          $" with FILE=1, " +
                                          $" NOUNLOAD, " +
                                          $" REPLACE, STATS = 5 " +
                                          $" ALTER DATABASE {newStr} SET MULTI_USER ";
                    using (SqlCommand command = new SqlCommand(sql, sa))
                    {
                        command.CommandTimeout = 60000;
                        command.ExecuteNonQuery();
                        //Task.Delay(60000).Wait();
                        //Image images2 = Image.FromFile(CompletedIcon);
                        //pictureBoxrestore.Image = images2;
                        // lblStatus.Text = "Completed..";
                    }
                }
                else
                {
                    string sql = $"ALTER DATABASE {newStr} SET SINGLE_USER WITH ROLLBACK IMMEDIATE " +
                                 $" RESTORE DATABASE {newStr} FROM DISK='{floc}{checkedDb[i]}' " +
                                              $" with FILE=1, " +
                                          $" NOUNLOAD, " +
                                          $" REPLACE, STATS = 5 " +
                                          $" ALTER DATABASE {newStr} SET MULTI_USER ";

                    using (SqlCommand command = new SqlCommand(sql, sa))
                    {
                        command.CommandTimeout = 60000;
                        command.ExecuteNonQuery();
                        //Task.Delay(60000).Wait();
                        //Image images2 = Image.FromFile(CompletedIcon);
                        //pictureBoxrestore.Image = images2;
                        // lblStatus.Text = "Completed..";
                    }
                }
            }
            catch (Exception ex)
            {
                Task.Delay(50).Wait();
                lblStatus.Text = "Error..";

                Image images1 = Image.FromFile(ErrorIcon);
                pictureBoxrestore.Image = images1;
                Logger.LogError("Restore", ex);
                MessageBox.Show(ex.Message);
                //Application.Exit();
                //Process.GetCurrentProcess().Kill();

                allStepsPassed = false;
                //throw;

            }

            Image image3 = Image.FromFile(Processed);
            restore1.Image = image3;
            if (i == 0)
            {
                restore1.Image = image3;
            }
            else
            {
                if (i == 1)
                {
                    restore2.Image = image3;
                }
                else
                {
                    if (i == 2)
                    {
                        restore3.Image = image3;
                    }
                    else
                    {
                        if (i == 3)
                        {
                            restore4.Image = image3;
                        }
                        else
                        {
                            if (i == 4)
                            {
                                restore5.Image = image3;
                            }
                            else
                            {
                                if (i == 5)
                                {
                                    restore6.Image = image3;
                                }
                            }
                        }
                    }
                }
            }


            //MessageBox.Show("Database " + newStr + " Restored Successfully", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void Audit(string username, SqlConnection sa, List<string> checkedDb, DateTime startTime, string selectedItem, string projectName, int i, string finalDb1, DateTime endTime, string Status)
        {
            string insertquery = $"insert into Job_Management.dbo.AuditTrail" + $" values ( '{startTime}' , '{endTime}' , '{projectName}' , '{username}' , '{checkedDb[i]}' , '{selectedItem}','{Status}' )";
            using (SqlConnection connection = new SqlConnection(con.connectionString))
            {
                try
                {
                    connection.Open();
                    {
                        using (SqlCommand cmd1 = new SqlCommand(insertquery, connection))
                        {
                            cmd1.CommandTimeout = 6000;
                            cmd1.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);

                    Image image01 = Image.FromFile(ErrorIcon);
                    pictureBoxError.Image = image01;
                    throw;
                }
            }
        }
        private void PermissionVerification(string username, SqlConnection sa, List<string> checkedDb, DateTime startTime, string selectedItem, string projectName, int i, string finalDb1, DateTime endTime, string Status)
        {
            try
            {
                //string insertquery = $"insert into Job_Management.dbo.AuditTrail" + $" values ( '{startTime}' , '{endTime}' , '{projectName}' , '{username}' , '{checkedDb[i]}' , '{selectedItem}','{Status}' )";
                //using (SqlConnection connection = new SqlConnection(con.connectionString))
                //{
                //    try
                //    {
                //        connection.Open();
                //        {
                //            using (SqlCommand cmd1 = new SqlCommand(insertquery, connection))
                //            {
                //                cmd1.CommandTimeout = 6000;
                //                cmd1.ExecuteNonQuery();
                //            }
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        MessageBox.Show(ex.Message);

                //        Image image01 = Image.FromFile(ErrorIcon);
                //        pictureBoxError.Image = image01;
                //        throw;
                //    }
                //}

                if (lblbrok.Text == "True")
                {
                    string alterbroker = $"ALTER DATABASE " + finalDb1 + $" SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE";
                    SqlCommand at = new SqlCommand(alterbroker, sa);
                    at.CommandTimeout = 6000;
                    at.ExecuteNonQuery();
                }
                if (lblbrok.Text == "False")
                {
                    string alterbroker = $"ALTER DATABASE " + finalDb1 + $" SET DISABLE_BROKER WITH ROLLBACK IMMEDIATE";
                    SqlCommand at = new SqlCommand(alterbroker, sa);
                    at.CommandTimeout = 600;
                    at.ExecuteNonQuery();
                }
                if (lbltrst.Text == "True")
                {
                    string altertrustworthy = $"ALTER DATABASE " + finalDb1 + $" SET TRUSTWORTHY ON ";
                    SqlCommand ex = new SqlCommand(altertrustworthy, sa);
                    ex.CommandTimeout = 6000;
                    ex.ExecuteNonQuery();
                }
                if (lbltrst.Text == "False")
                {
                    string altertrustworthy = $"ALTER DATABASE " + finalDb1 + $" SET TRUSTWORTHY OFF ";
                    SqlCommand ex = new SqlCommand(altertrustworthy, sa);
                    ex.CommandTimeout = 6000;
                    ex.ExecuteNonQuery();
                }
                string recoverymode = $"ALTER DATABASE " + finalDb1 + $" SET RECOVERY SIMPLE ";
                SqlCommand rm = new SqlCommand(recoverymode, sa);
                rm.CommandTimeout = 6000;
                rm.ExecuteNonQuery();
            }
            catch
            {
                allStepsPassed = false;
                //throw;
            }
        }
        private void PostScriptStep(SqlConnection sa, List<string> checkedDb, string selectedItem, string projectName, string floc, int xyz)
        {
            string username = UserCredentials.getUser();
            DateTime startTime = DateTime.Now; 
            for (int k = 0; k < xyz; k++)
            {
                string str = checkedDb[k];
                string newStr1 = str.Replace("_full_dump", "");
                string finalDb3 = checkedDb[k].Replace("_full_dump", "");
                string finalDb1 = finalDb3.Replace("_1", "");
                string newStr5 = finalDb3.Replace("_1", "");
                string newStr9 = finalDb1.Replace("_0", "");
                string newStr10 = newStr9.Replace("_2", "");
                string newStr11 = newStr10.Replace("_3", "");
                string newStr12 = newStr11.Replace("_4", "");
                string newStr13 = newStr12.Replace("_5", "");
                string newStr14 = newStr13.Replace("_6", "");
                string newStr15 = newStr14.Replace("_7", "");
                string newStr16 = newStr15.Replace("_8", "");
                string newStr = newStr16.Replace("_9", "");
                string test = newStr;
                string newstr2 = projectName + "_archive_sa";
                string newstr3 = projectName + "_websight";
                string newstr4 = floc + checkedDb[k] + ".bak";

                // Task.Delay(50).Wait();
                Image images8 = Image.FromFile(processingIcon);
                // postpicturebox.Image = images8;
               // poststatuspermission.Text = "Processing..";
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        postpicturebox.Image = images8;
                        poststatuspermission.Text = "Processing..";
                    }));
                }
                else
                {
                    postpicturebox.Image = images8;
                    poststatuspermission.Text = "Processing..";
                }           
                string sql3 = "";
                Logger.Log("post-permission", $"starting post-restore permission cleanup for {newStr} ");
                sql3 = $"USE [OATI] " +
                                            $"DECLARE @return_value int " +
                                            $"EXEC @return_value = [dbo].[usp_ITool_DBUserPermissions] " +
                                            $"@action = N'REMOVE'," +
                                            $"@database = N'{newStr}'," +
                                            $"@islocal = N'0'";
                Logger.Log("SQl", $"Executing:{sql3}");
                using (SqlCommand command3 = new SqlCommand(sql3, sa))
                {
                    command3.CommandTimeout = 6000;
                    command3.ExecuteNonQuery();
                }

                string query = $"SELECT  MEMBER_NAME,ROLE_NAME FROM OATI..tbl_ITool_Database_Permissions WHERE [DATABASE] ='{newStr}';";
                Logger.Log("SQL", $"Executing:{query}");
                try
                {
                    using (SqlCommand command = new SqlCommand(query, sa))
                    {

                        command.CommandTimeout = 6000;
                        command.ExecuteNonQuery();
                        SqlDataReader reader = command.ExecuteReader();
                        //MessageBox.Show(query, "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        var isBrokerEnabledCurrent = "";
                        var isTrustworthyOnCurrent = "";
                        List<string> asdf = new List<string> { };
                        string sql2 = "";
                        try
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {

                                    isBrokerEnabledCurrent = reader["MEMBER_NAME"].ToString();
                                    isTrustworthyOnCurrent = reader["ROLE_NAME"].ToString();


                                    sql2 = $"USE [OATI] " +
                                                    $"DECLARE @return_value int " +
                                                    $"EXEC @return_value = [dbo].[usp_ITool_SET_DBUserPermissions] " +
                                                    $"@database = N'{newStr}'," +
                                                    $"@member_principal_name = N'{isBrokerEnabledCurrent}'," +
                                                    $"@member_role_name = N'{isTrustworthyOnCurrent}'," +
                                                    $"@status = N'Done'," +
                                                    $"@message = N'Settings applied' ";

                                    asdf.Add(sql2);

                                    Logger.Log("SQl", $"Executing:{sql2}");
                                }



                                reader.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        string total = "";
                        for (int j = 0; j < asdf.Count; j++)
                        {
                            total = total + asdf[j] + "\n";
                            using (SqlCommand command3 = new SqlCommand(asdf[j], sa))
                            {
                                command3.CommandTimeout = 6000;
                                command3.ExecuteNonQuery();
                            }
                        }

                        //Task.Delay(50).Wait();
                        Image images9 = Image.FromFile(CompletedIcon);
                      //  postpicturebox.Image = images9;
                       // poststatuspermission.Text = "Completed..";
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() =>
                            {
                                postpicturebox.Image = images9;
                                poststatuspermission.Text = "Completed..";
                            }));
                        }
                        else
                        {
                            postpicturebox.Image = images9;
                            poststatuspermission.Text = "Completed..";
                        }
                        Logger.Log("Post-permission", $" post-restore permission cleanup passed for {newStr}");

                    }

                }
                catch (Exception ex)
                {
                    Logger.LogError("post-permission", ex);
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            postpicturebox.Image = Image.FromFile(ErrorIcon);
                            poststatuspermission.Text = "Error..";
                            MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                    }
                    throw;
                }

            }
            //  MessageBox.Show("post script started..");
            try
            {
                string scriptName = scriptPath + selectedItem + "-" + "post" + "-sanitization.sql";
                Logger.Log("Post-Script", $"Starting post-script:{scriptName}");
                Image image4 = Image.FromFile(processingIcon);
                //  pictureboxpost.Image = image4;
                //Task.Delay(50).Wait();
                // labelpost.Text = "Processing..";
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        pictureboxpost.Image = image4;
                        labelpost.Text = "Processing..";
                    }));
                }
                else
                {
                    pictureboxpost.Image = image4;
                    labelpost.Text = "Processing..";
                }
                //Task.Delay(50).Wait();

                ExecutePrePostSQLFiles(scriptName);

                Image images = Image.FromFile(CompletedIcon);
                // pictureboxpost.Image = images;
                Thread.Sleep(1000);
                //  labelpost.Text = "Completed..";
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        pictureboxpost.Image = images;
                        labelpost.Text = "Completed..";
                    }));
                }
                else
                {
                    pictureboxpost.Image = images;
                    labelpost.Text = "Completed..";
                }
                Logger.Log("Post-Script", $"complted post-script:{scriptName}");
                string Status = allStepsPassed ? "Pass" : "Fail";
                DateTime endTime = DateTime.Now;
                Audit(username, sa, checkedDb, startTime, selectedItem, textBoxProjectOut.Text, count, a, endTime, Status);
            }
            catch (Exception ex)
            {
                Logger.LogError("Post-Script", ex);
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        Image images = Image.FromFile(ErrorIcon);
                        pictureboxpost.Image = images;
                        labelpost.Text = "Error..";
                        MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        
                    }));
                }

                // MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                allStepsPassed = false;
                string Status = allStepsPassed ? "Pass" : "Fail";
                DateTime endTime = DateTime.Now;
                Audit(username, sa, checkedDb, startTime, selectedItem, textBoxProjectOut.Text, count, a, endTime, Status);
            }
        }
        public void ExecutePrePostSQLFiles(string scriptName)
        {
            try
            {
                Logger.Log("SQLScripts", $"Executing :{scriptName}");
                //Read script file from the file and replaceholder with actual project name 
                string script = File.ReadAllText(scriptName);
                //save the modified script to  a temporary file
                string tempfile = @"D:\MyTest.sql";
                File.WriteAllText(tempfile, script);
                Logger.Log("SQl Scripts", $"Temp script written to:{tempfile}");
                //Flag to track if any error is detected in output
                bool scriptHadErrors = false;
                using (FileStream fs = File.Create(tempfile)) ;
                ProcessStartInfo prInfor = new ProcessStartInfo();
                prInfor.Arguments = "-r";
                prInfor.FileName = "cmd";
                prInfor.CreateNoWindow = true;
                prInfor.UseShellExecute = false;
                prInfor.WorkingDirectory = Environment.CurrentDirectory;


                script = script.Replace("$(project)", textBoxProjectOut.Text);
                using (StreamWriter writer = new StreamWriter(tempfile, false))
                {
                    writer.Write(script);
                    writer.Flush();
                    writer.Close();
                }


                var process = new Process
                {

                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sqlcmd",
                        // Arguments = $@"-i{tempfile} -S {textBoxTargetSrvrInput.Text.Trim()} -o \\i-lokeshy\shared\logs_i_tool\output.txt",
                        Arguments = $"-V 16 -i \"{tempfile}\" -S {textBoxTargetSrvrInput.Text.Trim()} -E",// -o \\i-lokeshy\shared\logs_i_tool\output.txt",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = Environment.CurrentDirectory
                    },
                    EnableRaisingEvents = true
                };
                //Event to process standard output line by line
                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        Logger.LogRaw($"[[SQLCMD OUTPUT]]", args.Data.Trim());

                        if (args.Data.Contains("Msg") && args.Data.Contains("Level"))
                        {

                            Logger.LogRaw("[[SQLCMD DETECTED ERROR IN OUTPUT]]", args.Data.Trim());
                            scriptHadErrors = true;
                        }

                    }


                };

                //Event to Error standard output line by line
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        Logger.LogRaw($"[[SQLCMD ERROR]]", args.Data.Trim());

                        //check sql error msg
                        if (args.Data.Contains("Msg") && args.Data.Contains("Level"))
                        {

                            Logger.LogRaw("[[SQLCMD DETECTED ERROR IN STDERR]]", args.Data.Trim());
                            scriptHadErrors = true;
                        }

                    }


                };

                //start the process and reading output asynchronously
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();  //wait until the script finishes

                if (process.ExitCode != 0 || scriptHadErrors)
                {
                    MessageBox.Show($"Sql script", $"Script exited with {(scriptHadErrors ? "detected error in output" : $"exit code process.ExitCode")}");
                    throw new Exception("SQl script execution failed.");
                }
                Logger.Log("SQlScript", $"Successfully completed");
            }
            catch (Exception ex)
            {
                Logger.LogError("SqlScript", ex);
                throw;
            }
        }


        private bool RenameFolder()
        {

            try
            {

                // textBoxProjectOut.Text = "";
                var filePaths = Directory.GetFiles(textBoxDumpLoc.Text.Trim(), "*.bak");
                foreach (string filepath in filePaths)
                {
                    string filename = Path.GetFileNameWithoutExtension(filepath);

                    textBoxProjectOut.Text = filename.Split('_')[0];

                }



                string targetdb = textBoxTargetSrvrInput.Text.Split('\\')[0].ToUpper();
                //string  textBox = targetdb.Split('\\')[0].ToUpper();
                string relativePath = "D";
                string projectName = textBoxProjectOut.Text;
                string absolutePath = "\\\\" + targetdb + '\\' + relativePath + '\\' + projectName;
                string newpath = "\\\\" + targetdb + '\\' + relativePath;
                try
                {
                    DirectorySecurity security = Directory.GetAccessControl(newpath);


                    string userNameToCheck = targetdb;
                    AuthorizationRuleCollection accesssRules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));
                    bool hasAccess = false;
                    foreach (FileSystemAccessRule rule in accesssRules)
                    {
                        if (rule.IdentityReference.Value.Equals(userNameToCheck, StringComparison.OrdinalIgnoreCase) &&
                                (rule.FileSystemRights & FileSystemRights.Read) != 0)
                        {
                            hasAccess = true;
                            break;
                        }
                    }

                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("User Dont have access", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;


                }
                if (Directory.Exists(absolutePath))
                {
                    return false;
                }
                else
                {
                    return true;
                }


            }
            catch (Exception)
            {

            }
            return true;
        }
        public dynamic executeSanitizationProcess()
        {

            return true;
        }
        private void postlbl_Click(object sender, EventArgs e)
        {

        }

        private void comboBoxProduct_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void ProjectRefreshFrame_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                this.Dispose();
            }

        }


        private void btnReset_Click_1(object sender, EventArgs e)
        {
            resetfnc();
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void RelocatePathChk_CheckedChanged(object sender, EventArgs e)
        {
            DataFileFolderTxt.Enabled = true;
            LogFileFolderTxt.Enabled = true;

        }

        private void dbrestorename2_Click(object sender, EventArgs e)
        {

        }

        private void RestorationDetail_Enter(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void tick6_Click(object sender, EventArgs e)
        {

        }

        private void restore4_Click(object sender, EventArgs e)
        {

        }

        private void InformativePic_MouseHover(object sender, EventArgs e)
        {
            InfoText.Text = " - For Local Refresh it will verify the folder renaming at code_store location not in D: Drive. \n - Please do not use two product / Project at same folder. \n - The tool will work only for max 6 DBs refresh.";
            //  InfoText.BorderStyle= BorderStyle.FixedSingle;
        }

        private void InformativePic_MouseLeave(object sender, EventArgs e)
        {
            InfoText.Text = "";
            // InfoText.BorderStyle = BorderStyle.None;

        }

        private void btnDBbackup_Click(object sender, EventArgs e)
        {
            String exePath = localEXEPath + @"\DBBackupDisplay.exe";
            if (File.Exists(exePath))
            {
                executeProcess(exePath);
            }
        }
        private void executeProcess(String command)
        {

            try
            {
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = command;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

            }
            catch (Exception ex)
            {
                new MyError(ex).ShowDialog();
            }
        }
        private void renameBackFolder()
        {
            if (checkBox1.Checked == true)
            {
                string targetdb = textBoxTargetSrvrInput.Text.Split('\\')[0].ToUpper();
                //string  textBox = targetdb.Split('\\')[0].ToUpper();
                string CodeStorePath = "code_store";
                string projectName = textBoxProjectOut.Text;
                string absolutePath1 = "";
                if (Directory.Exists("\\\\" + targetdb + '\\' + CodeStorePath))
                {
                    absolutePath1 = "\\\\" + targetdb + '\\' + CodeStorePath + '\\' + projectName + "_rename";
                }
                else
                {
                    absolutePath1 = "\\\\" + targetdb + '\\' + 'D' + '\\' + projectName + "_rename";
                }
                // string absolutePath = "\\\\" + targetdb + '\\' + relativePath + '\\' + projectName + "_rename";
                string currentPath = absolutePath1;
                try
                {
                    string currentFolderName = Path.GetFileName(currentPath);
                    string newFolderName = projectName;
                    DirectorySecurity directorySecurity = new DirectoryInfo(currentPath).GetAccessControl();
                    string newFolderPath = Path.Combine(Path.GetDirectoryName(currentPath), newFolderName);
                    Directory.Move(currentPath, newFolderPath);
                    Directory.SetAccessControl(newFolderPath, directorySecurity);

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public static class Logger
        {
            // private static readonly string LogDir = @"C:\Logs\MyApp";
            private static readonly string LogDir = @"\\i-lokeshy\Shared\lokeshy_testing";

            public static string CurrentLogFile { get; private set; }
            static Logger()
            {
                if (!Directory.Exists(LogDir))
                    Directory.CreateDirectory(LogDir);
            }

            public static void InitializeLogFile(string projectName)
            {
                string username = Environment.UserName;
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string filename = $"{username}_{projectName}_{timestamp}_P2D.log";
                CurrentLogFile = Path.Combine(LogDir, filename);

                Log("INFO", $"Log file intialized:{CurrentLogFile}");


            }
            public static void Log(string stage, string message)

            {
                if (string.IsNullOrWhiteSpace(CurrentLogFile))
                    return;
                File.AppendAllText(CurrentLogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{stage}]{message}{Environment.NewLine}");

            }

            public static void LogError(string stage, Exception ex)
            {
                if (string.IsNullOrWhiteSpace(CurrentLogFile))
                    return;
                File.AppendAllText(CurrentLogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{stage}][Error]{ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");

            }

            private static readonly object _logLock = new object();
            public static void LogRaw(string tag, string message)
            {
                lock (_logLock)
                {
                    File.AppendAllText(CurrentLogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}[{tag}] {message}{Environment.NewLine}");
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start("\\\\i-lokeshy\\shared\\lokeshy_testing");
        }
    }
}
