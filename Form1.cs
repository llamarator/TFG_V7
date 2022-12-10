using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using ScottPlot;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace com
{
 
    public partial class btnread : Form
    {
        TextWriter archivo_w1;
        TextWriter archivo_w2;
        TextReader archivo_r;

        public const int orden_filtro = 100;
        double[] filtro = new double[orden_filtro];
        double[] signal = new double[500];
        public int i_arry = 0;
        public int i = 0;

        public int n_documento = 0;
        public UInt64 n_muestra = 0; //muestra química
        public UInt64 n_muestra_prev = 0;
        public int reading = 0;
        public int stop = 0;

        delegate void serialCalback(string val);
        public string descriptor_r;
        public string descriptor_w;
        public string nombre,dir_trabajo;
        private static Mutex obMutex = new Mutex();
        string incomSting;


        List<ulong> listA = new List<ulong>();
        List<double> listB = new List<double>();
        public btnread()
        {
            InitializeComponent();
        }


        private void button4_Click(object sender, EventArgs e)
        {

        }



        private void button5_Click(object sender, EventArgs e)  //leer
        {
            Form2 newForm = new Form2();
            newForm.ShowDialog();
        }

        private void button6_Click(object sender, EventArgs e)  //reiniciar
        {
            listA.Clear();
            listB.Clear();
            serialPort1.Write("1\n");
            //archivo_w1.Close();
            /*
            serialPort1.Write("1");
            double[] dataX = new double[] { 1, 2, 3, 4, 5 };
            double[] dataY = new double[] { 1, 4, 9, 16, 25 };
            formsPlot1.Plot.AddScatter(dataX, dataY);
            formsPlot1.Refresh();
            */
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)   //textbox selector del puerto
        {

        }
        
        private void button6_Click_1(object sender, EventArgs e)    //conectar
        {
            //borrar, solo util para depurar

            //nombre = "C:\\Users\\David\\Desktop\\TFG\\recepcionCOM\\C_sharp\\com_V6\\bin\\Debug" + "\\" + DateTime.Now.ToString("d;M;y");
            //Directory.CreateDirectory(nombre);  //creamos una carpeta para cada dia
            //nombre =   DateTime.Now.ToString("d;M;y") + "\\" + "estandar";// la primera muestra es el estandar
            //archivo_w1 = new StreamWriter(nombre);
            //archivo_w1.Close();
            //borrar lo superior

            listA.Clear();
            listB.Clear();
            timer1.Enabled = true;
            stop = 0;
            serialPort1.DataReceived -= serialPort1_DataReceived_1;//De no ser por esto habría 2 eventos lanzados
            serialPort1.DataReceived += serialPort1_DataReceived_1;    
            if (!serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.PortName = textBox1.Text.ToString();
                    serialPort1.BaudRate = 38400;
                    reading = 1;
                    serialPort1.Open();
                    serialPort1.Write("1\n");               //reiniciamos los parámetros del arduino
                    while (!(serialPort1.BytesToRead == 0 && serialPort1.BytesToWrite == 0))
                    {
                        serialPort1.DiscardInBuffer();
                        serialPort1.DiscardOutBuffer();
                    }
                    serialPort1.DiscardInBuffer();
                    serialPort1.DiscardOutBuffer();

                    btnconnect.Enabled = false;
                    btndisconnect.Enabled = true;
                    //serialPort1.Write("1\n");
                }
                catch (Exception ex)
                {

                    MessageBox.Show(ex.Message);
                }
            }
        }
  

        private void setText(string val)
        {
            if (this.textBox2.InvokeRequired)
            {
                serialCalback scb = new serialCalback(setText);
                this.Invoke(scb, new object[] { val });

            }
            else
            {
                textBox2.Text = val;

            }
      
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void btnclose_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.ExitThread();
            this.Close();
        }

        public delegate void InvokeDelegate();
 
        private void button6_Click_2(object sender, EventArgs e) //desconectar
        {
            timer1.Enabled = false;
            //iniciamos el hilo de esta manera asíncrona porque si no, el handler entra en bucle
            //al cerrar el puerto debido a que al intentar cerrar el puerto espera a que el evento
            // en el que se reciben los datos regrese y la GUI espera a que el handler esté en una 
            //posición de idle por tanto, se queda en un estado de congelación.

            //ThreadPool.QueueUserWorkItem(handleReceivedBytes);
            Console.WriteLine("CERRAR");
            stop = 1;
            serialPort1.DataReceived -= serialPort1_DataReceived_1;
            while (!(serialPort1.BytesToRead == 0 && serialPort1.BytesToWrite == 0))
            {
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
            }

            //if (reading == 0)
            // {
            BeginInvoke(new InvokeDelegate(InvokeMethod));
              //  return;
            //}
        }

        public void InvokeMethod()
        {
            try
            {
                
                lock (serialPort1)
                {
                    if (serialPort1.IsOpen)
                    {

                            if (reading == 0)
                            {
                                serialPort1.Close();
                            }

                    }
                }

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
            btnconnect.Enabled = true;
            btndisconnect.Enabled = false;

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void serialPort1_DataReceived_1(object sender, SerialDataReceivedEventArgs e)
        {

            //if (stop == 0)
            lock (serialPort1) { 
            try{

                reading = 1;
                incomSting = serialPort1.ReadLine();
                    incomSting = incomSting.Replace("\r", "");
                    var values = incomSting.Split(';');
                    if (UInt64.Parse(values[0]) != n_muestra) // no soluciona el problema de que escriba el mismo
                                                              // numero de muestra varias veces
                    ThreadPool.QueueUserWorkItem (R_W_files);
                    Console.WriteLine("leyendo");
                    Console.WriteLine(incomSting);
                    //R_W_files(incomSting);
            }
            catch (Exception ex)
            {
                    MessageBox.Show(ex.Message);
                    //put other, more interesting error handling here.
                }
            }
            reading = 0;
            setText(incomSting);
        }

        private void R_W_files(object state)
        {
            int a = 0;
            double s_filtrada = 0;

            int len = incomSting.Length + 1;
            Console.WriteLine("RECIBIENDO");
            if (len > 15)
            {
                //char c = incomSting[len - 3];
                //int a = Convert.ToInt32(c) - '0';
                //char cr = incomSting[0];
                //int b = Convert.ToInt32(cr) - '0';
                //text = text.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
                incomSting = incomSting.Replace("\r", "");
                var values = incomSting.Split(';');
                obMutex.WaitOne();

                filtro[i] = Convert.ToDouble(values[1])/10000.0;
                i++;
                if (i == orden_filtro) i = 0;
                s_filtrada = 0;
                for (a = 0; a < orden_filtro; a++)
                {
                    s_filtrada += filtro[a];
                }
                s_filtrada = s_filtrada / orden_filtro;
                try
                {
                  
                    listA.Add(UInt64.Parse(values[0]) / 1);
                    listB.Add(s_filtrada/100.0);

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                    obMutex.ReleaseMutex();
                //if (b == 0) return;
                obMutex.WaitOne();//seccion critica para el fichero
                if (n_muestra == 0) //1º muestra ESTANDAR 
                {
                    DateTime d = DateTime.Today;
                    //nombre = n_muestra + "-" + d.Day.ToString() + "_" + d.Month.ToString() + "_" + d.Year.ToString() + "-" + DateTime.Now.ToString("HH;mm;ss tt") + ".csv";
                    dir_trabajo = "C:\\Users\\David\\Desktop\\TFG\\recepcionCOM\\C_sharp\\com_V6\\bin\\Debug" + "\\" + DateTime.Now.ToString("d;M;y");
                    Directory.CreateDirectory(dir_trabajo);  //creamos una carpeta para cada dia
                    nombre =   DateTime.Now.ToString("d;M;y") + "\\" + "estandar";// la primera muestra es el estandar
                    archivo_w1 = new StreamWriter(nombre);
                    //archivo_w1.WriteLine("millis;Val;n_muestra;voltage_derivative;n_documento");
                    archivo_w1.Close();
                }
                try
                {

                    n_muestra = UInt64.Parse(values[4]);

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                
                if (n_muestra != n_muestra_prev)    //nueva muestra
                {
                    listA.Clear();
                    listB.Clear();
                    DateTime d = DateTime.Today;
                    nombre = n_muestra + "-" + d.Day.ToString() + "_" + d.Month.ToString() + "_" + d.Year.ToString() + "-" + DateTime.Now.ToString("HH;mm;ss tt") + ".csv";
                    obMutex.WaitOne();      //seccion critica para el fichero
                    archivo_w1 = new StreamWriter(nombre);
                    //archivo_w1.WriteLine("millis;Val;n_muestra;voltage_derivative;n_documento");
                    archivo_w1.WriteLine(incomSting);//meter SPI
                    n_muestra_prev = n_muestra;
                    archivo_w1.Close();
                    obMutex.ReleaseMutex(); //liberacion sección crítica
                }
                else                 //misma muestra
                {
                    using (StreamWriter archivo = File.AppendText(nombre))
                    {
                        DateTime d = DateTime.Today;
                        archivo.WriteLine(incomSting);
                        archivo.Close();
                    }



                }
                obMutex.ReleaseMutex();         //liberacion sección crítica
            }


        }

        private void chart2_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btnread_Load(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)    //timer
        {
            chart3.Series.Clear();
            var series = new Series("signal");

            // Frist parameter is X-Axis and Second is Collection of Y- Axis
            //series.ToolTip = ;//borrar
            obMutex.WaitOne();
            series.Points.DataBindXY(listA, listB);
            obMutex.ReleaseMutex();
            //chart1.Series.Add("helloworld");
            chart3.Series.Add(series);

            //chart1.Series[0].IsValueShownAsLabel = true;
            chart3.Series[0].IsVisibleInLegend = true;
            chart3.Series[0].LegendToolTip = "";
            chart3.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart3.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chart3.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            chart3.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
            //  this.chartControl1.ZoomType = ZoomType.MouseWheelZooming;

            chart3.Series["signal"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
        }
    }

}
