using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Integration_Tool.ProjectRefresh.Frames
{
    public partial class AuditTrailDataFrame : Form
    {
        public AuditTrailDataFrame()
        {
            InitializeComponent();



        }
        private DataTable auditdatatable = new DataTable();
        private void AuditTrailDataFrame_Load(object sender, EventArgs e)
        {
            Connection con = new Connection();
            string query = "select * from Job_Management.dbo.AuditTrail  order by ID DESC";
            using (SqlConnection connection = new SqlConnection(con.connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.Fill(auditdatatable);
                        dataGridViewAudit.DataSource = auditdatatable;
                        dataGridViewAudit.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                        dataGridViewAudit.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
            }

        private void btnApply_Click(object sender, EventArgs e)
        {
            auditdatatable.Clear();
            Connection con = new Connection();
            string selectedProduct = comboBox1.SelectedItem.ToString();
            string query = $"select * from Job_Management.dbo.AuditTrail where Product= '{selectedProduct}' order by ID DESC ";
            using (SqlConnection connection = new SqlConnection(con.connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.Fill(auditdatatable);
                        dataGridViewAudit.DataSource = auditdatatable;
                        dataGridViewAudit.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
                        dataGridViewAudit.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                        
                    }
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
    }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "CSV Files (*.csv)|*.csv";
                sfd.FileName = "ExportedData.csv";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    DataTable dt = (DataTable)dataGridViewAudit.DataSource;
                    StringBuilder csvContent = new StringBuilder();

                    foreach (DataColumn column in dt.Columns)
                    {
                        csvContent.Append(column.ColumnName);
                        csvContent.Append(",");
                    }
                    csvContent.AppendLine();

                    foreach (DataRow row in dt.Rows)
                    {
                        foreach (var item in row.ItemArray)
                        {
                            csvContent.Append(item.ToString());
                            csvContent.Append(",");
                        }
                        csvContent.AppendLine();
                    }
                    File.WriteAllText(sfd.FileName, csvContent.ToString());
                    MessageBox.Show("Data exported to CSV successfully.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
