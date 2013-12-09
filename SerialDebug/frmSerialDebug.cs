using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;


namespace SerialDebug
{
    public partial class frmMain : Form
    {

        private UInt64 RxCounter = 0;
        private UInt64 TxCounter = 0;
        private List<byte[]> reBytesList = new List<byte[]>();
        private Thread recThread;

        private delegate void TextBoxAppendDel(string str);             // �ı��������ַ�
        TextBoxAppendDel txtReceiveAppend;

        private delegate void SetLableTextDel(Label lab, string Text);
        SetLableTextDel SetLableText;

        private bool IsAutoSend = false;    // �Զ�����
        bool HyperTerminalMode = false;      // �����ն�ģʽ

        private void SetMode(bool IsHyperTerminalMode)
        {
            groupReceive.Visible = !IsHyperTerminalMode;
            groupSend.Visible = !IsHyperTerminalMode;
            splitContainer1.Panel2Collapsed = IsHyperTerminalMode;
            groupHyperTerminal.Visible = IsHyperTerminalMode;


            HyperTerminalMode = IsHyperTerminalMode;
        }


        public frmMain()
        {
            InitializeComponent();

            this.Controls.Add(groupHyperTerminal);
            groupHyperTerminal.Location = groupReceive.Location;
            groupHyperTerminal.Visible = false;

            SetMode(false);

            ////���벨����
            //cbBaudRate.Items.Add(110);
            //cbBaudRate.Items.Add(300);
            //cbBaudRate.Items.Add(600);
            //cbBaudRate.Items.Add(1200);
            //cbBaudRate.Items.Add(2400);
            //cbBaudRate.Items.Add(4800);
            //cbBaudRate.Items.Add(9600);
            //cbBaudRate.Items.Add(14400);
            //cbBaudRate.Items.Add(19200);
            //cbBaudRate.Items.Add(28800);
            //cbBaudRate.Items.Add(38400);
            //cbBaudRate.Items.Add(56000);
            //cbBaudRate.Items.Add(57600);
            //cbBaudRate.Items.Add(128000);
            //cbBaudRate.Items.Add(115200);
            //cbBaudRate.Items.Add(256000);
            ////cbBaudRate.SelectedItem = 9600;
            //cbBaudRate.Text = Convert.ToString(9600);

            //��ż����λ
            cbParity.Items.Add(System.IO.Ports.Parity.Even);
            cbParity.Items.Add(System.IO.Ports.Parity.Mark);
            cbParity.Items.Add(System.IO.Ports.Parity.None);
            cbParity.Items.Add(System.IO.Ports.Parity.Odd);
            cbParity.Items.Add(System.IO.Ports.Parity.Space);
            cbParity.SelectedItem = System.IO.Ports.Parity.None;


            //����λ
            cbDataBit.Items.Add(5);
            cbDataBit.Items.Add(6);
            cbDataBit.Items.Add(7);
            cbDataBit.Items.Add(8);
            cbDataBit.SelectedItem = 8;

            //ֹͣλ
            //cbStopBit.Items.Add(System.IO.Ports.StopBits.None);
            cbStopBit.Items.Add(System.IO.Ports.StopBits.One);
            cbStopBit.Items.Add(System.IO.Ports.StopBits.OnePointFive);
            cbStopBit.Items.Add(System.IO.Ports.StopBits.Two);
            cbStopBit.SelectedItem = System.IO.Ports.StopBits.One;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            picPortState.Image = ImageList.Images["close"];
            picTop.Image = imglistTop.Images["nailoff"];
            cbComName.DataSource = SerialPort.GetPortNames();
            cbStreamControl.SelectedIndex = 0;
            serialPort.RtsEnable = chkRTS.Checked;
            this.Text = Application.ProductName + " V" + Application.ProductVersion.Substring(0, 3);

            CheckForIllegalCrossThreadCalls = false;


            txtReceiveAppend = new TextBoxAppendDel(TextBoxReceiveAppend);
            SetLableText = new SetLableTextDel(setLableText);
            cbHTEOFChars.SelectedIndex = 0;


        }


        #region ����������

        /// <summary>
        /// �򿪹رմ��ڲ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPortOpt_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnPortOpt.Text == "�򿪴���")
                {

                    recThread = new Thread(new ThreadStart(ReceiveThreadHandle));
                    recThread.IsBackground = true;
                    recThread.Start();


                    serialPort.PortName = cbComName.SelectedItem.ToString();
                    serialPort.BaudRate = Convert.ToInt32(cbBaudRate.Text);
                    serialPort.Parity = (System.IO.Ports.Parity)cbParity.SelectedItem;
                    serialPort.DataBits = (int)cbDataBit.SelectedItem;
                    serialPort.StopBits = (System.IO.Ports.StopBits)cbStopBit.SelectedItem;

                    serialPort.ReadBufferSize = 4 * 1024 * 1024;//33554432;           // 32M
                    serialPort.Open();

                }
                else
                {
                    serialPort.Close();
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (serialPort.IsOpen)
                {
                    picPortState.Image = ImageList.Images["open"];
                    btnPortOpt.Text = "�رմ���";
                    cbComName.Enabled = false;
                    UpdatalabText();
                }
                else
                {
                    picPortState.Image = ImageList.Images["close"];
                    btnPortOpt.Text = "�򿪴���";
                    cbComName.Enabled = true;
                    UpdatalabText();
                }
            }
        }

        /// <summary>
        /// ѡ��ͨ�ſڡ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbComName_SelectedIndexChanged(object sender, EventArgs e)
        {
            string portname = serialPort.PortName;
            try
            {
                serialPort.PortName = cbComName.SelectedItem.ToString();
                UpdatalabText();
            }
            catch (Exception ex)
            {
                cbComName.SelectedItem = portname;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// �����ʡ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbBaudRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            int bps = serialPort.BaudRate;
            try
            {
                serialPort.BaudRate = Convert.ToInt32(cbBaudRate.Text);
                UpdatalabText();
            }
            catch (Exception ex)
            {
                cbBaudRate.Text = bps.ToString();
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// У��λ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbParity_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.IO.Ports.Parity pt = serialPort.Parity;
            try
            {
                serialPort.Parity = (System.IO.Ports.Parity)cbParity.SelectedItem;
                UpdatalabText();
            }
            catch (Exception ex)
            {
                serialPort.Parity = pt;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ����λ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbDataBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            int db = serialPort.DataBits;
            try
            {
                serialPort.DataBits = Convert.ToInt32(cbDataBit.SelectedItem);
                UpdatalabText();
            }
            catch (Exception ex)
            {
                serialPort.DataBits = db;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ֹͣλ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbStopBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.IO.Ports.StopBits sb = serialPort.StopBits;
            try
            {
                serialPort.StopBits = (System.IO.Ports.StopBits)cbStopBit.SelectedItem;
                UpdatalabText();
            }
            catch (Exception ex)
            {
                serialPort.StopBits = sb;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbStreamControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                switch (cbStreamControl.SelectedIndex)
                {
                    case 0:
                        serialPort.Handshake = Handshake.None;
                        serialPort.RtsEnable = chkRTS.Checked;
                        serialPort.DtrEnable = chkDTR.Enabled;
                        break;
                    case 1:
                        serialPort.Handshake = Handshake.XOnXOff;
                        serialPort.RtsEnable = chkRTS.Checked;
                        serialPort.DtrEnable = chkDTR.Enabled;
                        break;
                    case 2:
                        serialPort.Handshake = Handshake.RequestToSend;
                        break;
                    case 3:
                        serialPort.Handshake = Handshake.RequestToSendXOnXOff;
                        break;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ���ں�������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbComName_DropDown(object sender, EventArgs e)
        {
            cbComName.DataSource = SerialPort.GetPortNames();
        }

        /// <summary>
        /// ����RTS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkRTS_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort.Handshake == Handshake.RequestToSend || serialPort.Handshake == Handshake.RequestToSendXOnXOff)
                {
                    chkRTS.Checked = !chkRTS.Checked;
                    MessageBox.Show("��������ѡ��Ӳ������Ӳ����������ʱ�޷���ȡ������DTS", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    serialPort.RtsEnable = chkRTS.Checked;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ����DTR
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkDTR_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                serialPort.DtrEnable = chkDTR.Checked;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        /// <summary>
        /// ˢ��״̬����
        /// </summary>
        private void UpdatalabText()
        {
            if (serialPort.IsOpen)
            {
                string str = string.Format("ͨ������({0},{1},{2},{3},{4})",
                      serialPort.PortName, serialPort.BaudRate, serialPort.Parity, (int)serialPort.DataBits,
                      (float)serialPort.StopBits);

                labIsSerialOpen.Text = str;
            }
            else
            {
                labIsSerialOpen.Text = "ͨ�ſ��ѹر�";
            }

        }


        #endregion


        #region �Ҽ��˵�����

        private TextBox txtBoxMenu = new TextBox();

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuUndo_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Undo();
        }

        /// <summary>
        /// ���С�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuCut_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Cut();
        }

        /// <summary>
        /// ���ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuCopy_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Copy();
        }

        /// <summary>
        /// ճ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuPaste_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Paste();
        }

        /// <summary>
        /// ɾ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDelet_Click(object sender, EventArgs e)
        {
            txtBoxMenu.SelectedText = "";
        }

        /// <summary>
        /// ȫѡ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSelectAll_Click(object sender, EventArgs e)
        {
            txtBoxMenu.SelectAll();
        }

        /// <summary>
        /// �ַ���תʮ�����ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuStringToHex_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] bytes = System.Text.ASCIIEncoding.Default.GetBytes(txtBoxMenu.SelectedText);
                txtBoxMenu.Paste(BitConverter.ToString(bytes).Replace('-', ' ').TrimEnd());

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ������ת�ַ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuHexToString_Click(object sender, EventArgs e)
        {
            try
            {
                //string[] strArray = txtBoxMenu.SelectedText.TrimEnd().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string[] strArray = txtBoxMenu.SelectedText.TrimEnd().Replace(Environment.NewLine, " ").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                byte[] bytes = Array.ConvertAll<string, byte>(strArray, new Converter<string, byte>(HexStringToByte));
                txtBoxMenu.Paste(System.Text.ASCIIEncoding.Default.GetString(bytes));

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ������תʮ���ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuHexToDec_Click(object sender, EventArgs e)
        {
            try
            {
                string[] strArray = txtBoxMenu.SelectedText.TrimEnd().Replace(Environment.NewLine, " ").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder sb = new StringBuilder(strArray.Length);
                foreach (string str in strArray)
                {
                    sb.Append(Convert.ToByte(str, 16).ToString() + " ");
                }
                txtBoxMenu.Paste(sb.ToString().TrimEnd());

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ����תʮ�����ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDecToHex_Click(object sender, EventArgs e)
        {
            try
            {
                string[] strArray = txtBoxMenu.SelectedText.TrimEnd().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                byte[] bytes = Array.ConvertAll<string, byte>(strArray, new Converter<string, byte>(DecStringToByte));
                txtBoxMenu.Paste(BitConverter.ToString(bytes).Replace('-', ' ').TrimEnd());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// �ַ���תʮ���ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuStringToDec_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] bytes = System.Text.ASCIIEncoding.Default.GetBytes(txtBoxMenu.SelectedText);
                StringBuilder sb = new StringBuilder(bytes.Length * 2);
                foreach (byte mbyte in bytes)
                {
                    sb.Append(mbyte.ToString() + " ");
                }
                txtBoxMenu.Paste(sb.ToString().TrimEnd());

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ����ת�ַ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDecToString_Click(object sender, EventArgs e)
        {
            try
            {
                string[] strArray = txtBoxMenu.SelectedText.TrimEnd().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                byte[] bytes = Array.ConvertAll<string, byte>(strArray, new Converter<string, byte>(DecStringToByte));
                txtBoxMenu.Paste(System.Text.ASCIIEncoding.Default.GetString(bytes));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// �����Ҽ��˵���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmenuStrip_Opened(object sender, EventArgs e)
        {
            menuUndo.Enabled = txtBoxMenu.CanUndo;

            if (txtBoxMenu.SelectionLength > 0)
            {
                menuCut.Enabled = true;
                menuCopy.Enabled = true;
                menuDelet.Enabled = true;
                menuStringToHex.Enabled = true;
                menuHexToString.Enabled = true;
                menuHexToDec.Enabled = true;
                menuDecToHex.Enabled = true;
                menuStringToDec.Enabled = true;
                menuDecToString.Enabled = true;
            }
            else
            {
                menuCut.Enabled = false;
                menuCopy.Enabled = false;
                menuDelet.Enabled = false;
                menuStringToHex.Enabled = false;
                menuHexToString.Enabled = false;
                menuHexToDec.Enabled = false;
                menuDecToHex.Enabled = false;
                menuStringToDec.Enabled = false;
                menuDecToString.Enabled = false;
            }

            if (txtBoxMenu.Text == "")
            {
                menuSelectAll.Enabled = false;
            }
            else
            {
                menuSelectAll.Enabled = true;
            }

        }

        /// <summary>
        /// �������������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtReceive_MouseEnter(object sender, EventArgs e)
        {
            txtBoxMenu = txtReceive;
        }

        /// <summary>
        /// �����뷢������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtSend_MouseEnter(object sender, EventArgs e)
        {
            txtBoxMenu = txtSend;
        }

        #endregion


        #region ���ܺ���

        /// <summary>
        /// ʮ�������ַ���תʮ���ơ�
        /// </summary>
        /// <param name="hexStr"></param>
        /// <returns></returns>
        byte HexStringToByte(string hexStr)
        {
            return Convert.ToByte(hexStr, 16);
        }

        /// <summary>
        /// ʮ�����ַ���תʮ���ơ�
        /// </summary>
        /// <param name="decStr"></param>
        /// <returns></returns>
        byte DecStringToByte(string decStr)
        {
            return Convert.ToByte(decStr, 10);
        }



        #endregion


        #region ״̬������

        /// <summary>
        /// ���ý���������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picReceiveFont_Click(object sender, EventArgs e)
        {
            fontDlg.Font = txtReceive.Font;
            if (fontDlg.ShowDialog() == DialogResult.OK)
            {
                txtReceive.Font = fontDlg.Font;
            }
        }

        /// <summary>
        /// �ö�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picTop_Click(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
            if (this.TopMost == true)
            {
                this.Text = Application.ProductName + " V" + Application.ProductVersion.Substring(0, 3) + "  [�ö�]";
                picTop.Image = imglistTop.Images["nailon"];
            }
            else
            {
                this.Text = Application.ProductName + " V" + Application.ProductVersion.Substring(0, 3);
                picTop.Image = imglistTop.Images["nailoff"];
            }

            string textToolTip;
            if (this.TopMost)
            {
                textToolTip = "ȡ���ö�";
            }
            else
            {
                textToolTip = "�ö�";
            }
            ToolTip.SetToolTip(picTop, textToolTip);
        }


        /// <summary>
        /// ��ս�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labClearReceive_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtReceive.Clear();
        }

        /// <summary>
        /// ��շ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labClearSend_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtSend.Clear();
        }

        /// <summary>
        /// ��ռ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClear_Click(object sender, EventArgs e)
        {
            labRx.Text = "RX:0";
            RxCounter = 0;
            labTx.Text = "TX:0";
            TxCounter = 0;
        }

        /// <summary>
        /// ��ս��ռ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labRx_DoubleClick(object sender, EventArgs e)
        {
            labRx.Text = "RX:0";
            RxCounter = 0;
        }

        /// <summary>
        /// ��շ��ͼ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labTx_DoubleClick(object sender, EventArgs e)
        {
            labTx.Text = "TX:0";
            TxCounter = 0;
        }


        /// <summary>
        /// ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnHelp_Click(object sender, EventArgs e)
        {
            bool topmost = this.TopMost;

            this.TopMost = false;
            AboutBox myAboutBox = new AboutBox();
            myAboutBox.ShowDialog();
            this.TopMost = topmost;
        }

        /// <summary>
        /// �ر�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEnd_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion


        #region ���ڽ�����ʾ


        /// <summary>
        /// ���ڽ����жϡ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesLen = 0;
                byte[] bytes;
                do
                {
                    bytesLen = serialPort.BytesToRead;
                    if (bytesLen >= 4096)
                    {

                        bytes = new byte[bytesLen];
                        if (bytesLen <= 0)
                        {
                            return;
                        }
                        serialPort.Read(bytes, 0, bytesLen);
                        lock (reBytesList)
                        {
                            reBytesList.Add(bytes);
                        }
                    }
                    else
                    {
                        Thread.Sleep(30);
                    }
                } while (bytesLen != serialPort.BytesToRead);

                if (bytesLen <= 0)
                {
                    return;
                }
                bytes = new byte[bytesLen];
                serialPort.Read(bytes, 0, bytesLen);
                lock (reBytesList)
                {
                    reBytesList.Add(bytes);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("���ڽ���" + ex.Message);
            }
        }

        /// <summary>
        /// ���մ����̡߳�
        /// </summary>
        private void ReceiveThreadHandle()
        {
            byte[] bytes = null;
            int bytesLen = 0;

            while (true)
            {
                try
                {
                    //bytes = null;
                    lock (reBytesList)
                    {
                        if (reBytesList.Count > 0)
                        {
                            if (reBytesList[0] != null)
                            {
                                bytes = reBytesList[0];
                                reBytesList[0] = null;
                            }
                            reBytesList.RemoveAt(0);
                        }
                    }

                    if (bytes != null)
                    {
                        bytesLen = bytes.Length;
                        if (chkDisplay.Checked)
                        {
                            if (chkReceiveHex.Checked)
                            {
                                if (bytesLen > 0)
                                {
                                    //txtReceiveAppend(BitConverter.ToString(bytes).Replace('-', ' '));
                                    //txtReceiveAppend(" ");
                                    TextBoxReceiveAppend(BitConverter.ToString(bytes).Replace('-', ' '));
                                    TextBoxReceiveAppend(" ");
                                }
                            }
                            else
                            {
                                //txtReceiveAppend(System.Text.ASCIIEncoding.Default.GetString(bytes));
                                TextBoxReceiveAppend(System.Text.ASCIIEncoding.Default.GetString(bytes));
                            }

                            if (chkWrap.Checked)                    // �Զ�����
                            {
                                //txtReceiveAppend(Environment.NewLine);
                                TextBoxReceiveAppend(Environment.NewLine);
                            }
                        }

                        RxCounter = RxCounter + (UInt64)bytesLen;
                        // myUpdateRx(RxCounter);
                        setLableText(labRx, string.Format("RX:{0}", RxCounter));

                        bytes = null;
                        Thread.Sleep(10);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("���ݴ����̣߳�" + ex.Message);
                }
            }
        }

        /// <summary>
        /// ���½��ճ��ȡ�
        /// </summary>
        /// <param name="count"></param>
        private void UpdateRx(UInt64 count)
        {
            labRx.Text = "RX:" + count.ToString();
            labRx.Refresh();
        }


        private void setLableText(Label lab, string text)
        {
            if (lab.InvokeRequired)
            {
                ////this.BeginInvoke(SetLableText(lab,text));
                //this.BeginInvoke(new SetLableTextDel(setLableText));
                lab.BeginInvoke(new MethodInvoker(delegate
                {
                    lab.Text = text;
                }));
                return;
            }
            else
            {
                lab.Text = text;
            }
        }


        /// <summary>
        /// ���½����ı���
        /// </summary>
        /// <param name="appendText"></param>
        private void TextBoxReceiveAppend(string appendText)
        {
            if (txtReceive.InvokeRequired)
            {
                txtReceive.BeginInvoke(new MethodInvoker(delegate
                {
                    //txtReceive.AppendText(appendText);
                    TextBoxReceiveAppend(appendText);
                }));
                return;
            }
            else
            {

                if (HyperTerminalMode == true)
                {
                    HyperTerminalShowText(appendText);
                }
                else
                {
                    txtReceive.AppendText(appendText);
                }
            }
        }





        #endregion



        #region ���ڷ���

        private Thread mySendThread;

        /// <summary>
        /// ����״̬�����ա�
        /// </summary>
        /// <param name="count"></param>
        private void UpdateTx(UInt64 count)
        {
            labTx.Text = "TX:" + count.ToString();
            labTx.Refresh();
        }


        private bool SerialSendAbort = false;
        /// <summary>
        /// �����ʼ���ͻ���ֹͣ���͡�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (btnSend.Text == "��ʼ����")
            {

                if (serialPort.IsOpen == false)
                {
                    MessageBox.Show("����δ�򿪣����ȴ򿪴���", "��������", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                if (txtSend.Text == "")
                {
                    MessageBox.Show("û�пɷ��͵�����", "��������", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                SerialSendAbort = false;
                mySendThread = new Thread(new ThreadStart(SendThreadHandle));
                mySendThread.IsBackground = true;
                mySendThread.Start();

                SetSendEnable(true);
                setLableText(labTx, string.Format("TX:{0}", TxCounter));
            }
            else
            {
                //mySendThread.Abort();
                SerialSendAbort = true;
                SetSendEnable(false);
                setLableText(labTx, string.Format("TX:{0}", TxCounter));
            }
        }

        /// <summary>
        /// �Զ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkAutoSend_CheckedChanged(object sender, EventArgs e)
        {
            IsAutoSend = chkAutoSend.Checked;
        }

        /// <summary>
        /// ���÷���ʹ�ܡ�
        /// </summary>
        /// <param name="IsEnable">��ΪTrueʱ��ʾ��ʼ���ͣ�False��ʾֹͣ���͡�</param>
        private void SetSendEnable(bool IsEnable)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(delegate()
                {
                    SetSendEnable(IsEnable);
                }));
                return;
            }

            if (IsEnable == true)
            {
                chkSendHex.Enabled = false;
                chkAutoSend.Enabled = false;
                numSendCount.Enabled = false;
                numSendInterval.Enabled = false;
                numSendOnceBytes.Enabled = false;
                btnSend.Text = "ֹͣ����";
            }
            else
            {
                chkSendHex.Enabled = true;
                chkAutoSend.Enabled = true;
                numSendCount.Enabled = true;
                numSendInterval.Enabled = true;
                numSendOnceBytes.Enabled = true;
                btnSend.Text = "��ʼ����";
            }

        }

        /// <summary>
        /// ����һ�����ݡ�
        /// </summary>
        /// <param name="sendBuff"></param>
        private void SendOnce(byte[] sendBuff)
        {
            if (Convert.ToUInt64(numSendOnceBytes.Value) == 0)        // һ�η��������ֽ�
            {
                TxCounter += (UInt64)sendBuff.Length;
                setLableText(labTx, string.Format("TX:{0}", TxCounter));

                serialPort.Write(sendBuff, 0, sendBuff.Length);
            }
            else
            {
                /*�ֶ�η����ı�������*/

                int AlreadySendIndex = 0;                                               // �Ѿ����͵��ֽ���
                int BytesToSendPerOnce = Convert.ToInt32(numSendOnceBytes.Value);       // ÿ�η��͵��ֽ���
                int SendInterval = Convert.ToInt32(numSendInterval.Value);              // ���ͼ��
                while (AlreadySendIndex < sendBuff.Length)
                {
                    if ((AlreadySendIndex + BytesToSendPerOnce) < sendBuff.Length)      // ��δ�����һ������
                    {
                        TxCounter += (UInt64)BytesToSendPerOnce;
                        setLableText(labTx, string.Format("TX:{0}", TxCounter));

                        serialPort.Write(sendBuff, AlreadySendIndex, BytesToSendPerOnce);
                        AlreadySendIndex += BytesToSendPerOnce;
                        Thread.Sleep(SendInterval);
                    }
                    else
                    {
                        int cnt = sendBuff.Length - AlreadySendIndex;

                        TxCounter += (UInt64)cnt;
                        setLableText(labTx, string.Format("TX:{0}", TxCounter));

                        serialPort.Write(sendBuff, AlreadySendIndex, cnt);
                        AlreadySendIndex += cnt;
                    }



                }
            }
        }

        /// <summary>
        /// ���������̡߳�
        /// </summary>
        private void SendThreadHandle()
        {
            SerialSendAbort = false;

            byte[] sendBuff;
            try
            {
                if (chkSendHex.Checked)
                {
                    //string[] strArray = txtSend.Text.TrimEnd().Split(new char[] { ' '}, StringSplitOptions.RemoveEmptyEntries);
                    string[] strArray = txtSend.Text.TrimEnd().Replace(Environment.NewLine, " ").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    sendBuff = Array.ConvertAll<string, byte>(strArray, new Converter<string, byte>(HexStringToByte));
                }
                else
                {
                    sendBuff = System.Text.ASCIIEncoding.Default.GetBytes(txtSend.Text);
                }

                int sendLen = sendBuff.Length;

            }
            catch (Exception ex)
            {

                SetSendEnable(false);
                MessageBox.Show(ex.Message, "��������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //if (mySendThread.ThreadState != System.Threading.ThreadState.Aborted)
                //{
                //    mySendThread.Abort();
                //}
                SerialSendAbort = true;
                return;
            }

            while (SerialSendAbort == false)
            {
                try
                {
                    if (IsAutoSend)        // �Զ�����
                    {
                        int reSendCnt = Convert.ToInt32(numSendCount.Value);
                        int sleepTime = Convert.ToInt32(numSendInterval.Value);
                        if (reSendCnt == 0)
                        {
                            while (SerialSendAbort == false)
                            {
                                SendOnce(sendBuff);
                                Thread.Sleep(sleepTime);
                            }
                        }
                        else
                        {
                            while (reSendCnt > 0)
                            {
                                SendOnce(sendBuff);
                                reSendCnt--;
                                if (reSendCnt > 0)
                                {
                                    Thread.Sleep(sleepTime);
                                }
                            }
                            SetSendEnable(false);
                            //mySendThread.Abort();
                            SerialSendAbort = true;
                            return;
                        }
                    }
                    else//�ֶ�����
                    {
                        SendOnce(sendBuff);
                        SetSendEnable(false);
                        //mySendThread.Abort();
                        return;
                    }


                }
                catch (Exception ex)
                {
                    SetSendEnable(false);
                    //MessageBox.Show(ex.Message, "��������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine("�����̳߳�����" + ex.Message);
                    if (mySendThread.ThreadState != System.Threading.ThreadState.Aborted)
                    {
                        mySendThread.Abort();
                    }
                    return;
                }
            }
        }

        #endregion



        #region �����ļ��ʹ��ļ�


        /// <summary>
        /// ��������˵���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lnkSaveData_Enter(object sender, EventArgs e)
        {
            if (txtReceive.Text == "")
            {
                cmenuSave.Enabled = false;
            }
            else
            {
                cmenuSave.Enabled = true;
            }
            this.cmenuSave.Show(lnkSaveData, 0, lnkSaveData.Height);
        }


        /// <summary>
        /// ��ԭʼ��ʾ���档
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSaveStringToText_Click(object sender, EventArgs e)
        {

            sFileDlg.Filter = "�ı��ļ�(*.txt)|*.txt";
            if (sFileDlg.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sFileDlg.FileName, txtReceive.Text);
                MessageBox.Show("�ļ��ѱ��浽\n" + sFileDlg.FileName, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        /// <summary>
        /// ��������תΪ��������ʾ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSaveStringToBinary_Click(object sender, EventArgs e)
        {

            sFileDlg.Filter = "�������ļ�(*.bin)|*.bin";
            if (sFileDlg.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(sFileDlg.FileName, FileMode.OpenOrCreate);
                BinaryWriter bw = new BinaryWriter(fs);
                try
                {
                    byte[] bytes = System.Text.ASCIIEncoding.Default.GetBytes(txtReceive.Text);
                    bw.Write(bytes);
                    MessageBox.Show("�ļ��ѱ��浽\n" + sFileDlg.FileName, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    bw.Close();
                    fs.Close();
                }
            }

        }


        /// <summary>
        /// ��ʮ�����Ƶ��������ļ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSaveHexToBinary_Click(object sender, EventArgs e)
        {

            sFileDlg.Filter = "�������ļ�(*.bin)|*.bin";
            if (sFileDlg.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(sFileDlg.FileName, FileMode.OpenOrCreate);
                BinaryWriter bw = new BinaryWriter(fs);
                try
                {

                    //string[] strArray = txtReceive.Text.TrimEnd().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] strArray = txtReceive.Text.TrimEnd().Replace(Environment.NewLine, "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    byte[] bytes = Array.ConvertAll<string, byte>(strArray, new Converter<string, byte>(HexStringToByte));
                    bw.Write(bytes);
                    MessageBox.Show("�ļ��ѱ��浽\n" + sFileDlg.FileName, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    bw.Close();
                    fs.Close();
                }
            }
        }




        private void lnkOpen_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            oFileDlg.Filter = "�ı��ļ�(*.txt)|*.txt|�������ļ�(*.bin)|*.bin|�����ļ�(*.*)|*.*";
            if (oFileDlg.ShowDialog() == DialogResult.OK)
            {
                string strExt = System.IO.Path.GetExtension(oFileDlg.FileName).ToUpper();
                if (strExt == ".TXT")
                {
                    txtSend.Text = File.ReadAllText(oFileDlg.FileName);
                }
                else
                {
                    FileStream fs = new FileStream(oFileDlg.FileName, FileMode.Open);
                    BinaryReader br = new BinaryReader(fs);
                    try
                    {
                        byte[] bytes = br.ReadBytes((int)fs.Length);
                        txtSend.Text = BitConverter.ToString(bytes).Replace('-', ' ').TrimEnd();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, oFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        br.Close();
                        fs.Close();
                    }

                }

            }
        }

        #endregion


        #region �����ն�ģʽ


        /// <summary>
        /// �����ն���ʾ�ı�
        /// </summary>
        /// <param name="appendText"></param>
        private void HyperTerminalShowText(string appendText)
        {

            HyperTerminal_HandleMessage(appendText);
            return;

            #region �����ն�ģʽ��ʾ
            string[] textBoxArray = txtReceive.Lines;
            int indexLines = txtReceive.Lines.Length;
            if (indexLines > 0)
            {
                indexLines = indexLines - 1;
            }

            int strIndex = -1;

            string[] textLines = appendText.Split(new string[] { "\r\n", "\n\r", "\n" }, StringSplitOptions.None);

            int index = 0;
            foreach (string strLine in textLines)
            {
                string str = strLine;
                textBoxArray = txtReceive.Lines;

                do
                {
                    strIndex = str.IndexOf('\b');
                    if (strIndex == 0)
                    {
                        string tempStr = string.Empty;

                        str = str.Remove(0, 1);
                        if (txtReceive.Text.Length > 0)
                        {
                            txtReceive.Text = txtReceive.Text.Remove(txtReceive.Text.Length - 1, 1);
                            textBoxArray = txtReceive.Lines;
                            indexLines = txtReceive.Lines.Length;
                            if (indexLines > 0)
                            {
                                indexLines = indexLines - 1;
                            }
                        }
                    }
                    else if (strIndex > 0)
                    {
                        str = str.Remove(strIndex - 1, 2);
                    }
                } while (strIndex >= 0);

                strIndex = str.IndexOf('\r');
                if (strIndex >= 0)
                {
                    string[] rArray = str.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    string rText = string.Empty;
                    foreach (string rStr in rArray)
                    {
                        if (rStr.Length > rText.Length)
                        {
                            rText = rStr;
                        }
                        else
                        {
                            rText.Replace(rText.Substring(0, rStr.Length), rStr);
                        }
                    }
                    if (rText.Length != 0)
                    {
                        textBoxArray[indexLines] = rText;
                        txtReceive.Lines = textBoxArray;
                        if (str.EndsWith("\r") == false)
                        {
                            txtReceive.AppendText(Environment.NewLine);
                            indexLines++;
                        }
                    }
                    else
                    {
                        txtReceive.SelectionStart = txtReceive.GetFirstCharIndexFromLine(indexLines);
                    }
                }
                else
                {
                    txtReceive.AppendText(str);
                    if (index < textLines.Length - 1)
                    {
                        txtReceive.AppendText(Environment.NewLine);
                        indexLines++;
                    }
                }
                index++;
            }
            #endregion


        }

        /// <summary>
        /// V3.1����
        /// </summary>
        /// <param name="message"></param>
        private void HyperTerminal_HandleMessage(string message)
        {
           
            string[] txtArray = txtReceive.Lines;
            string[] appendLines = message.Split(new string[] { "\r\n", "\n\r", "\n" }, StringSplitOptions.None);

            int appendLineIndex = 0;
            int rowIndex = 0;
            int searchCharIndex = -1;
            string outStr;
            string inStr;
            foreach (string appendStr in appendLines)
            {
                inStr = appendStr;
                rowIndex = 0;
                do
                {
                    if (rowIndex >= appendStr.Length) break;

                    searchCharIndex = appendStr.IndexOf('\b', rowIndex);
                    if (searchCharIndex > 0)
                    {
                        inStr = inStr.Remove(searchCharIndex - 1, 2);

                    }
                    else if (searchCharIndex == 0)
                    {
                        inStr = inStr.Remove(0, 1);
                        if (txtReceive.SelectionStart > 0)
                        {
                            txtReceive.SelectionStart--;
                            txtReceive.SelectionLength = 1;
                            txtReceive.SelectedText = "";
                        }
                    }
                    rowIndex += searchCharIndex + 1;

                } while (searchCharIndex >= 0);


                outStr = inStr;
                rowIndex = 0;
                do
                {
                    if (rowIndex >= inStr.Length)
                    {
                        outStr = "";
                        break;
                    }
                    else
                    {
                        outStr = inStr.Substring(rowIndex, inStr.Length - rowIndex);
                    }

                    searchCharIndex = inStr.IndexOf('\r', rowIndex);
                    if (searchCharIndex < 0)
                    {
                        break;
                    }
  

                    outStr = inStr.Substring(rowIndex + 1);
                    if (searchCharIndex == 0)
                    {
                        txtReceive.SelectionStart = txtReceive.GetFirstCharIndexOfCurrentLine();
                    }
                    else if (searchCharIndex > 0)
                    {
                        txtReceive.SelectionLength = searchCharIndex - rowIndex;
                        txtReceive.SelectedText = inStr.Substring(rowIndex, searchCharIndex - rowIndex);
                        txtReceive.SelectionStart = txtReceive.GetFirstCharIndexOfCurrentLine();
                    }
                    rowIndex += searchCharIndex + 1;


                } while (searchCharIndex >= 0);

                appendLineIndex++;
                txtReceive.SelectionLength = outStr.Length;
                txtReceive.SelectedText = outStr;
                if (appendLineIndex < appendLines.Length)
                {
                    txtReceive.SelectionStart = txtReceive.Text.Length;
                    txtReceive.SelectedText = Environment.NewLine;
                }

            }

        }


        string htSendString = string.Empty;
        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtReceive_KeyPress(object sender, KeyPressEventArgs e)
        {
            //txtSend.Text = string.Format("KeyChar:{0}", (int)e.KeyChar);
            if (HyperTerminalMode == false || serialPort.IsOpen == false)
            {
                return;
            }

            if (chkSendByEnter.Checked)
            {
                if (e.KeyChar == 8)     // �˸�
                {
                    if (htSendString.Length > 0)
                    {
                        htSendString = htSendString.Remove(htSendString.Length - 1, 1);
                    }
                }
                if (e.KeyChar == 13)    // �س�
                {
                    serialPort.Write(string.Format("{0}{1}", htSendString, HtEofChars));
                    htSendString = string.Empty;
                }
                else
                {
                    htSendString += (char)e.KeyChar;
                }
            }
            else
            {
                serialPort.Write(new byte[] { (byte)e.KeyChar }, 0, 1);
            }
            if (chkHTShowback.Checked == false)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// ��ͨģʽ�������ն�ģʽ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNormalToHyperTerminal_Click(object sender, EventArgs e)
        {
            SetMode(true);
        }

        /// <summary>
        /// �����ն�ģʽ����ͨģʽ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnHyperTerminalToNormal_Click(object sender, EventArgs e)
        {
            SetMode(false);
        }

        string HtEofChars = string.Empty;       // �س�����ʱ������ֹ��
        /// <summary>
        /// ѡ�������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbHTEOFChars_SelectedIndexChanged(object sender, EventArgs e)
        {
            //NONE
            //NULL��\0��
            //LF��\n��
            //CR+LF��\r\n��
            //LF+CR��\n\r��
            //CR��\r��

            switch (cbHTEOFChars.SelectedIndex)
            {
                case 0:
                    HtEofChars = string.Empty;
                    break;
                case 1:
                    HtEofChars = "\0";
                    break;
                case 2:
                    HtEofChars = "\n";
                    break;
                case 3:
                    HtEofChars = "\r\n";
                    break;
                case 4:
                    HtEofChars = "\n\r";
                    break;
                case 5:
                    HtEofChars = "\r";
                    break;


            }
        }


        #endregion




        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }

                if (recThread != null)
                {
                    if (recThread.IsAlive)
                    {
                        recThread.Abort();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                recThread = null;
            }
        }









    }
}