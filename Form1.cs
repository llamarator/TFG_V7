using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace com
{
    public partial class btnread : Form
    {
        private TextWriter archivo_w1;
        private TextWriter archivo_w2;
        private TextReader archivo_r;

        public int Ts = 50;
        public const int orden_filtro = 2;
        private double[] filtro = new double[orden_filtro];
        public int i_arry = 0;
        public int i = 0;

        public int n_documento = 0;
        public UInt64 n_muestra = 0; //muestra química
        public UInt64 n_muestra_prev = 0;
        public int reading = 0;
        public int stop = 0;

        private delegate void serialCalback(string val);

        public string descriptor_r;
        public string descriptor_w;
        public string nombre, dir_trabajo;
        private static Mutex lstMutex = new Mutex();
        private static Mutex wrMutex = new Mutex();
        private static Mutex obMutex = new Mutex();
        private static Mutex flMutex = new Mutex();
        private static ReaderWriterLock fl_lock = new ReaderWriterLock();
        private string incomSting;

        private List<ulong> listA = new List<ulong>();
        private List<double> listB = new List<double>();

        public btnread()
        {
            InitializeComponent();
        }

        public void guardar()
        {
            using (StreamWriter archivo_w1 = new StreamWriter(nombre))
            {
                for (int i = 0; i < listA.Count; i++)
                {
                    archivo_w1.Write(listA[i]);
                    archivo_w1.Write(";");
                    archivo_w1.Write(listB[i]);
                    archivo_w1.Write(";");
                    archivo_w1.Write(Ts);
                    archivo_w1.Write("\n");
                }
            }
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
        }

        private void button6_Click_1(object sender, EventArgs e)    //conectar
        {
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
                    btninit.Enabled = true;
                    button6.Enabled = true; //Botón next
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

        private void btnclose_Click(object sender, EventArgs e)
        {
            if(serialPort1.IsOpen) guardar();
            System.Windows.Forms.Application.ExitThread();
            this.Close();
        }

        public delegate void InvokeDelegate();

        private void button6_Click_2(object sender, EventArgs e) //desconectar
        {
            timer1.Enabled = false;

            guardar();

            Console.WriteLine("CERRAR");
            stop = 1;
            serialPort1.DataReceived -= serialPort1_DataReceived_1;
            while (!(serialPort1.BytesToRead == 0 && serialPort1.BytesToWrite == 0))
            {
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
            }
            BeginInvoke(new InvokeDelegate(InvokeMethod));
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
            btninit.Enabled = false;
            button6.Enabled = false; //Botón next
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
        }

        private void serialPort1_DataReceived_1(object sender, SerialDataReceivedEventArgs e)
        {
            int a = 0;
            double s_filtrada = 0;
            DateTime d = DateTime.Today;

            // Se utiliza un bloqueo para garantizar la exclusión mutua 
            // en el acceso al puerto serial.
            lock (serialPort1)
            {
                try
                {
                    // Se indica que se está leyendo del puerto serial.
                    reading = 1;

                    // Se lee el mensaje recibido del puerto serial.
                    incomSting = serialPort1.ReadLine();

                    // Se elimina el carácter de retorno de carro del mensaje.
                    incomSting = incomSting.Replace("\r", "");

                    // Se separan los valores del mensaje por el caracter ';'.
                    var values = incomSting.Split(';');
                    int len = incomSting.Length + 1;

                    // Se realiza el filtrado del valor recibido.
                    if (len > 15)
                    {
                        filtro[i] = Convert.ToDouble(values[1]) / 10000.0;
                        i++;

                        // Si se ha llegado al final del filtro, se vuelve a comenzar desde el principio.
                        if (i == orden_filtro) i = 0;

                        // Se calcula el promedio del filtro.
                        s_filtrada = 0;
                        for (a = 0; a < orden_filtro; a++)
                        {
                            s_filtrada += filtro[a];
                        }
                        s_filtrada = s_filtrada / orden_filtro;

                        // Se espera a obtener el mutex para acceder a las listas de valores.
                        obMutex.WaitOne();
                        try
                        {
                            // Se convierten y añaden los valores a las listas de valores.
                            Ts = Int32.Parse(values[2]);
                            listA.Add(UInt64.Parse(values[0]) / 1);
                            listB.Add(s_filtrada / 100.0);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }

                        // Se libera el mutex para permitir a otros threads acceder a las listas.
                        obMutex.ReleaseMutex();

                        // Se espera a obtener el mutex para acceder al archivo de registro.
                        wrMutex.WaitOne();

                        try { n_muestra = UInt64.Parse(values[4]); }
                        catch (Exception ex) { MessageBox.Show(ex.Message); }

                        // Si el directorio de trabajo es nulo, se crea una carpeta para el día actual.
                        if (dir_trabajo == null) //1º documento
                        {
                            d = DateTime.Today;
                            dir_trabajo = Directory.GetCurrentDirectory() + "\\" + DateTime.Now.ToString("yyyy;MM;dd");
                            Directory.CreateDirectory(dir_trabajo);  //creamos una carpeta para cada dia
                            nombre = dir_trabajo + "\\" + n_muestra + "-" + d.Day.ToString() + "_" + d.Month.ToString() + "_" + d.Year.ToString() + "-" + DateTime.Now.ToString("HH;mm;ss tt") + ".csv";
                            archivo_w1 = TextWriter.Synchronized(new StreamWriter(nombre));
                            archivo_w1.Close();
                        }
                        // Si la muestra actual es diferente a la muestra previa, se crea un nuevo archivo de registro.
                        else if (n_muestra != n_muestra_prev)    //nuevo documento
                        {
                            guardar();
                            nombre = dir_trabajo + "\\" + n_muestra + "-" + d.Day.ToString() + "_" + d.Month.ToString() + "_" + d.Year.ToString() + "-" + DateTime.Now.ToString("HH;mm;ss tt") + ".csv";
                            archivo_w1 = TextWriter.Synchronized(new StreamWriter(nombre));
                            archivo_w1.Close();

                            listA.Clear();
                            listB.Clear();

                            // Envía el comando 5 indicando al Arduino que se ha aumentado la muestra química
                            serialPort1.Write("5\n");
                            listA.Clear();
                            listB.Clear();
                            d = DateTime.Today;
                            nombre = dir_trabajo + "\\" + n_muestra + "-" + d.Day.ToString() + "_" + d.Month.ToString() + "_" + d.Year.ToString() + "-" + DateTime.Now.ToString("HH;mm;ss tt") + ".csv";
                            obMutex.WaitOne();      //seccion critica para el fichero
                            archivo_w1 = TextWriter.Synchronized(new StreamWriter(nombre));
                            archivo_w1.Close();
                            n_muestra_prev = n_muestra;

                            // se libera el mutex para permitir que otros hilos accedan a la sección crítica
                            obMutex.ReleaseMutex(); 
                        }
                        //liberacion sección crítica
                        wrMutex.ReleaseMutex();         
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            reading = 0;
            setText(incomSting);
        }

        // La siguiente función se utiliza para escribir en el archivo de guardado.
        private void R_W_files(object sender)
        {
            Console.WriteLine("RECIBIENDO");

            // Seccion critica para el fichero
            flMutex.WaitOne();
            try
            {
                fl_lock.AcquireWriterLock(1); 

                archivo_w1.WriteLine(incomSting);
            }
            finally
            {
                fl_lock.ReleaseWriterLock();
            }

            // Libera la sección crítica
            flMutex.ReleaseMutex();
        }


        private Series mySeries = new Series("signal");

        private void timer1_Tick(object sender, EventArgs e)    //timer para representar la gráfica
        {
            mySeries.Points.Clear();
            chart3.Series.Clear();
            // Como los valores de la lista listA están en nº de muestras es necesario hacer la conversión 
            // Para que se muestren en unidades de tiempo
            for (int j = 0; j < listA.Count; j++)
            {
                double xValue = (j * 50) / (60.0 * 1000.0);  // Valor en segundos
                double yValue = listB[j];                   // Valor en Voltios
                mySeries.Points.AddXY(xValue, yValue);
            }
            chart3.Series.Add(mySeries);

            chart3.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chart3.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chart3.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            chart3.ChartAreas[0].AxisY.MinorGrid.Enabled = false;

            // Obtener el número total de muestras
            int totalSamples = mySeries.Points.Count;

            // Calcular el valor máximo en minutos para el eje X
            double maxXValueInMinutes = totalSamples * 50.0 / 1000.0 / 60.0;

            // Definir el formato del label para mostrar minutos
            string labelFormat = "0.00' min'";

            // Asignar el nuevo label al eje X
            chart3.ChartAreas[0].AxisX.Title = "Tiempo (min)";
            chart3.ChartAreas[0].AxisX.LabelStyle.Format = labelFormat;
            chart3.Series["signal"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
        }

        public bool OptimizeOfLocalFormsOnly(System.Windows.Forms.Control chartControlForm)     //usado para agilizar los charts
        {
            if (!System.Windows.Forms.SystemInformation.TerminalServerSession)
            {
                SetUpDoubleBuffer(chartControlForm);
                return true;
            }
            return false;
        }

        private void button6_Click_4(object sender, EventArgs e)
        {
            // Envía un comando al Arduino para que aumente la muestra química
            serialPort1.Write("2\n");
        }

        public static void SetUpDoubleBuffer(System.Windows.Forms.Control chartControlForm)     //usado para agilizar los charts
        {
            System.Reflection.PropertyInfo formProp =
            typeof(System.Windows.Forms.Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            formProp.SetValue(chartControlForm, true, null);
        }
    }
}