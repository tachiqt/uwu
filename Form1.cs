using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using Arduino_Csharp_Serial_Communication_Control_a_Servo.Repository;
using System.Data.SqlClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Xml.Linq;
using System.Diagnostics.Eventing.Reader;

namespace Arduino_Csharp_Serial_Communication_Control_a_Servo
{
    public partial class Form1 : Form
    {
        private bool isProcessing = false;
        private string lastScannedID = string.Empty;
        private DateTime lastScanTime = DateTime.MinValue;
        private int scanCooldownMs = 3000; // 3 seconds cooldown between scans

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            textBox1.Focus();
        }

        private void comboBox_portLists_DropDown(object sender, EventArgs e)
        {
            string[] portLists = SerialPort.GetPortNames();
            comboBox_portLists.Items.Clear();
            comboBox_portLists.Items.AddRange(portLists);
        }

        private void button_open_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort1.PortName = comboBox_portLists.Text;
                serialPort1.BaudRate = 57600;
                serialPort1.Open();

                string str_degree = "0";

                serialPort1.Write(str_degree + "\n");
                MessageBox.Show("Success Connected to Arduino Board");
            
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (textBox1.Text != "" && !isProcessing)
            {
                string currentID = textBox1.Text;
                DateTime currentTime = DateTime.Now;

                // Check if this is the same ID scanned within cooldown period
                if (currentID == lastScannedID &&
                    (currentTime - lastScanTime).TotalMilliseconds < scanCooldownMs)
                {
                    // Ignore this scan - it's too soon after the previous scan of the same ID
                    textBox1.Text = "";
                    return;
                }

                isProcessing = true;

                try
                {
                    lastScannedID = currentID;
                    lastScanTime = currentTime;

                    using (SqlConnection con = new SqlConnection(AppHelper.ConnectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT * FROM Details WHERE ID =@ID"))
                        {
                            cmd.Parameters.AddWithValue("@ID", textBox1.Text);
                            cmd.CommandType = CommandType.Text;
                            cmd.Connection = con;
                            con.Open();
                            using (SqlDataReader sdr = cmd.ExecuteReader())
                            {
                                if (sdr.Read() && sdr.HasRows)
                                {
                                    string statID = sdr["ID"].ToString();
                                    string statName = sdr["Name"].ToString();
                                    string stat = sdr["Status"].ToString().Trim(); 
                                    if (stat.Equals("Logged", StringComparison.OrdinalIgnoreCase))
                                    {
                                      
                                        using (SqlConnection sqlConnection = new SqlConnection(AppHelper.ConnectionString))
                                        {
                                            using (SqlCommand sqlCmdEdit = new SqlCommand("Update dbo.Details set Status='Out' where ID=@ID", sqlConnection))
                                            {
                                                sqlCmdEdit.Parameters.AddWithValue("@ID", textBox1.Text);
                                                sqlConnection.Open();
                                                sqlCmdEdit.ExecuteNonQuery();
                                                sqlConnection.Close();

                                              
                                                RefreshDataGridView(textBox1.Text);

                                                if (serialPort1.IsOpen)
                                                {
                                                    serialPort1.Write(180 + "\n");
                                                }
                                            }
                                        }

                                       
                                        using (SqlConnection sqlConnection = new SqlConnection(AppHelper.ConnectionString))
                                        {
                                            sqlConnection.Open();
                                            DateTime currentDate = DateTime.Now;
                                            using (SqlCommand sqlCmd = new SqlCommand("Insert into tbllogsheet(ID,Name,TimeOut)values(@ID,@Name,@CurrentDate)", sqlConnection))
                                            {
                                                sqlCmd.Parameters.AddWithValue("@ID", textBox1.Text);
                                                sqlCmd.Parameters.AddWithValue("@Name", statName);
                                                sqlCmd.Parameters.AddWithValue("@CurrentDate", currentDate);
                                                sqlCmd.ExecuteNonQuery();
                                            }
                                            sqlConnection.Close();
                                        }
                                    }
                                    else
                                    {
                                       
                                        using (SqlConnection sqlConnection = new SqlConnection(AppHelper.ConnectionString))
                                        {
                                            using (SqlCommand sqlCmdEdit = new SqlCommand("Update dbo.Details set Status='Logged' where ID=@ID", sqlConnection))
                                            {
                                                sqlCmdEdit.Parameters.AddWithValue("@ID", textBox1.Text);
                                                sqlConnection.Open();
                                                sqlCmdEdit.ExecuteNonQuery();
                                                sqlConnection.Close();

                                               
                                                RefreshDataGridView(textBox1.Text);

                                                if (serialPort1.IsOpen)
                                                {
                                                    serialPort1.Write(0 + "\n");
                                                }
                                            }
                                        }

                          
                                        using (SqlConnection sqlConnection = new SqlConnection(AppHelper.ConnectionString))
                                        {
                                            sqlConnection.Open();
                                            DateTime currentDate = DateTime.Now;
                                            using (SqlCommand sqlCmd = new SqlCommand("Insert into tbllogsheet(ID,Name,TimeIn)values(@ID,@Name,@CurrentDate)", sqlConnection))
                                            {
                                                sqlCmd.Parameters.AddWithValue("@ID", textBox1.Text);
                                                sqlCmd.Parameters.AddWithValue("@Name", statName);
                                                sqlCmd.Parameters.AddWithValue("@CurrentDate", currentDate);
                                                sqlCmd.ExecuteNonQuery();
                                            }
                                            sqlConnection.Close();
                                        }
                                    }

                                    textBox1.Text = "";
                                }
                                else
                                {
                                    textBox1.Text = "";
                                    MessageBox.Show("ID not Found Please Register!");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error processing scan: " + ex.Message);
                }
                finally
                {
                    isProcessing = false;
                }
            }
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            serialPort1.Write(0 + "\n");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            serialPort1.Write(90 + "\n");
        }

        private void EventDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void RefreshDataGridView(string id)
        {
            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Details where ID=@ID", AppHelper.ConnectionString);
            da.SelectCommand.Parameters.AddWithValue("@ID", id);
            DataSet ds = new DataSet();
            da.Fill(ds, "Details");

            EventDataGridView.DataSource = ds.Tables["Details"].DefaultView;
            EventDataGridView.Update();
            EventDataGridView.Refresh();
        }
    }
}