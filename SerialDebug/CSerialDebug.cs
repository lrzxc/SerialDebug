using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;

namespace SerialDebug
{
    class CSerialDebug
    {


        private Queue<SerialDebugReceiveData> receiveQueue = new Queue<SerialDebugReceiveData>();
        private List<CSendParam> sendList = new List<CSendParam>();

        private SerialPort _serialPort = new SerialPort();
        private bool IsReceiveStart = false;
        private Thread receiveThread;
        private Thread parseThread;
        private bool IsSendStart = false;
        private Thread sendThread;
        private TimeSpan delayTime = new TimeSpan(10 * 400);
        private int LoopCount = 0;
        private int _ReceiveTimeOut = 3;

        public delegate void ReceivedEventHandler(object sender, SerialDebugReceiveData e);
        public event ReceivedEventHandler ReceivedEvent;

        public delegate void SendCompletedEventHandler(object sender, SendCompletedEventArgs e);
        public event SendCompletedEventHandler SendCompletedEvent;
        public event EventHandler SendOverEvent;


        private AutoResetEvent waitReceiveEvent = new AutoResetEvent(false);
        private ManualResetEvent waitParseEvent = new ManualResetEvent(false);
        private ManualResetEvent uartReceivedEvent = new ManualResetEvent(false);

        public CSerialDebug(SerialPort sport)
        {
            _serialPort = sport;
        }

        public SerialPort serialPort
        {
            get { return _serialPort; }
            set { _serialPort = value; }
        }

        public int ReceiveTimeOut
        {
            get { return _ReceiveTimeOut; }
            set { _ReceiveTimeOut = value; }
        }

        public void Start()
        {

            try
            {
                IsReceiveStart = true;
                lock (receiveQueue)
                {
                    receiveQueue.Clear();
                }

                lock (sendList)
                {
                    sendList.Clear();
                }

                uartReceivedEvent.Reset();
                waitReceiveEvent.Reset();
                waitParseEvent.Reset();

                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                serialPort.Open();
                serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);

                releaseThread(receiveThread);
                releaseThread(sendThread);

                receiveThread = new Thread(new ThreadStart(ReceiveThreadHandler));
                receiveThread.IsBackground = true;
                receiveThread.Name = "receiveThread";
                receiveThread.Start();

                parseThread = new Thread(new ThreadStart(ParseThreadHandler));
                parseThread.IsBackground = true;
                parseThread.Name = "parseThread";
                parseThread.Start();
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

        }

        public void Stop()
        {
            try
            {
                IsReceiveStart = false;
                IsSendStart = false;

                serialPort.DataReceived -= new SerialDataReceivedEventHandler(serialPort_DataReceived);

                serialPort.Close();

            }
            catch (System.Exception ex)
            {
                throw ex;
            }

        }

        public void StopReceive()
        {
            IsReceiveStart = false;
        }

        public void StopSend()
        {
            IsSendStart = false;
        }
        public void Send(List<CSendParam> list)
        {
            LoopCount = 1;
            Send(list, LoopCount);
        }
        public void Send(List<CSendParam> list, int loop)
        {

            if (sendThread != null)
            {
                if (sendThread.IsAlive)
                {
                    releaseThread(sendThread);
                }
            }


            LoopCount = loop;
            sendList = list;
            IsSendStart = true;

            sendThread = new Thread(new ThreadStart(SendThreadHandler));
            sendThread.IsBackground = true;
            sendThread.Name = "sendThread";
            sendThread.Start();
        }

        void releaseThread(Thread th)
        {
            try
            {
                if (th != null)
                {
                    if (th.IsAlive)
                    {
                        th.Abort();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            uartReceivedEvent.Set();
        }

        /// <summary>
        /// �����߳�
        /// </summary>
        private void ReceiveThreadHandler()
        {

            //byte[] receiveBytes = new byte[4 * 1024];
            while (IsReceiveStart)
            {
                try
                {
                    if (_serialPort.IsOpen)
                    {
                        try
                        {
                            int dataLen;
                            if (uartReceivedEvent.WaitOne())
                            {
                                uartReceivedEvent.Reset();

                                DateTime beginTime = DateTime.Now;
                                int buffSize = 0;
                                do
                                {
                                    buffSize = serialPort.BytesToRead;
                                    Thread.Sleep(ReceiveTimeOut);
                                } while (buffSize != serialPort.BytesToRead);

                                if (buffSize > 0)
                                {
                                    byte[] receiveBytes = new byte[buffSize];
                                    // serialPort.ReadTimeout = ReceiveTimeOut;
                                    dataLen = serialPort.Read(receiveBytes, 0, receiveBytes.Length);
                                    if (dataLen > 0)
                                    {
                                        byte[] bytes = new byte[dataLen];
                                        Array.Copy(receiveBytes, bytes, dataLen);
                                        lock (receiveQueue)
                                        {
                                            receiveQueue.Enqueue(new SerialDebugReceiveData(bytes));
                                        }
                                        waitReceiveEvent.Set();
                                    }
                                }
                            }
                            
                        }
                        catch (System.Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }


                    }
                    else
                    {
                        Thread.Sleep(10);
                    }

                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

            }
        }

        /// <summary>
        /// ������Ϣ�����߳�
        /// </summary>
        private void ParseThreadHandler()
        {
            while (IsReceiveStart)
            {
                try
                {
                    SerialDebugReceiveData data = null;
                    lock (receiveQueue)
                    {
                        if (receiveQueue.Count > 0)
                        {
                            data = receiveQueue.Dequeue();
                        }
                    }

                    if (data != null)
                    {

                        if (ReceivedEvent != null)
                        {
                            ReceivedEvent(this, data);
                        }
                        waitParseEvent.Set();
                    }
                    else
                    {
                        waitReceiveEvent.WaitOne(10);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("���ݴ����̣߳�" + ex.Message);
                }
            }
        }

        /// <summary>
        /// �����߳�
        /// </summary>
        private void SendThreadHandler()
        {
            if (LoopCount == 0)
            {
                LoopCount = int.MaxValue;
            }
            while (LoopCount > 0 && IsSendStart)
            {
                LoopCount--;

                waitParseEvent.Reset();

                int index = 0;
                while (index < sendList.Count && IsSendStart)
                {
                    CSendParam sendParam = null;
                    lock (sendList)
                    {
                        sendParam = sendList[index];
                    }
                    index++;

                    if (sendParam != null)
                    {
                        if (sendParam.Mode == SendParamMode.SendAfterLastSend)
                        {

                        }
                        else if (sendParam.Mode == SendParamMode.SendAfterReceived)
                        {
                            waitParseEvent.WaitOne();
                        }


                        if (sendParam.DelayTime > 0)
                        {
                            DateTime startTime = DateTime.Now;
                            TimeSpan ts = DateTime.Now - startTime;
                            do
                            {
                                Thread.Sleep(delayTime);
                                ts = DateTime.Now - startTime;
                            } while (ts.TotalMilliseconds < sendParam.DelayTime);
                        }


                        if (_serialPort.IsOpen)
                        {
                            _serialPort.Write(sendParam.DataBytes, 0, sendParam.DataBytes.Length);

                            if (SendCompletedEvent != null)
                            {
                                //SendCompletedEvent(this, new SendCompletedEventArgs(sendParam));
                                SendCompletedEventHandler handler = SendCompletedEvent;
                                handler(this, new SendCompletedEventArgs(sendParam));
                            }
                        }
                        else
                        {
                            IsSendStart = false;
                        }
                        waitParseEvent.Reset();
                    }

                }

            }

            if (SendOverEvent != null)
            {
                SendOverEvent(this, null);
            }

            //if (SendCompletedEvent != null)
            //{
            //    SendCompletedEvent(this, new SendCompletedEventArgs(null));
            //}

        }
    }


    public class SerialDebugReceiveData : EventArgs
    {
        private readonly DateTime _ReceiveTime;
        private readonly byte[] _ReceiveData;
        private readonly int _DataLen;

        public SerialDebugReceiveData(byte[] data)
        {
            _ReceiveData = data;
            _ReceiveTime = DateTime.Now;
            if (data != null)
            {
                _DataLen = data.Length;
            }
            else
            {
                _DataLen = 0;
            }
        }

        public byte[] ReceiveData
        {
            get { return _ReceiveData; }
        }

        public DateTime ReceiveTime
        {
            get { return _ReceiveTime; }
        }

        public int DataLen
        {
            get { return _DataLen; }
        }

        public string TimeString
        {
            get
            {
                return string.Format("[{0}.{1:D3}]", _ReceiveTime.ToString("yyyy-MM-dd HH:mm:ss"), _ReceiveTime.Millisecond);
            }
        }

        public string HexString
        {
            get
            {
                return string.Format("{0} ", BitConverter.ToString(_ReceiveData).Replace('-', ' '));
            }
        }

        public string ASCIIString
        {
            get { return System.Text.ASCIIEncoding.Default.GetString(_ReceiveData); }
        }

        public string DecString
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                foreach (byte b in _ReceiveData)
                {
                    sb.AppendFormat("{0} ", Convert.ToInt32(b));
                }

                return sb.ToString();
            }

        }
    }

    public class SendCompletedEventArgs : EventArgs
    {
        private readonly DateTime _SendTime;
        private CSendParam _SendParam;

        public SendCompletedEventArgs(CSendParam sendParam)
        {
            _SendTime = DateTime.Now;
            _SendParam = sendParam;

            //if (sendParam!=null)
            //{
            //    _SendParam = new CSendParam(sendParam.Format,
            //    sendParam.Mode,
            //    sendParam.DelayTime,
            //    sendParam.DataBytes, 0, sendParam.DataLen);
            //}
            //else
            //{
            //    _SendParam = null;
            //}
        }

        public DateTime SendTime
        {
            get { return _SendTime; }
        }

        public CSendParam SendParam
        {
            get { return _SendParam; }
        }

        public string TimeString
        {
            get
            {
                return string.Format("[{0}.{1:D3}]", _SendTime.ToString("yyyy-MM-dd HH:mm:ss"), _SendTime.Millisecond);
            }
        }
    }
}