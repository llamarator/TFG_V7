//using OxyPlot.Series;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace com
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        public double Ts = 50;//poner 50
        public const int orden_filtro = 100;

        public double peak_threesold = 0.002;
        public double peak_pos1_der = 0.0003;
        public double peak_pos2_der = 0.0001;
        public double peak_neg_der = -0.0001;

        public Int32 x0 = 0, x1 = 0, x2 = 0;
        public Int16 contador = 0;
        public Int32[] markers = new Int32[50];
        public Int16 marker_i = 0;
        private List<double> listA = new List<double>();
        private List<double> listB = new List<double>();
        private double[] filtro = new double[orden_filtro];
        public string descriptor_r;
        public string descriptor_w;
        public string nombre, dir_trabajo;
        public string path_estandar;
        public Int16 n_pdf = 0;

        private Series series = new Series("signal");

        public Int32 n_peaks = 0;
        public int init_peak = 0;
        public double peak_init_value = 0.0;
        private List<int> x0_peak = new List<int>();        //inicio de pico
        private List<int> x1_peak = new List<int>();        //pos pico
        private List<int> x2_peak = new List<int>();        //fin pico
        private List<double> pos_peaks = new List<double>();

        // Esta función se utiliza para encontrar y registrar picos
        private void peak_finder()
        {
            int fin = 0;
            int fin_2 = 0;
            for (int i = 5000; i < listA.Count - 100; i++)// el 5000 inicial es porque antes de ese tiempo se ignoran
            {
                //Se comprueba la derivada positiva con el valor PPD1
                if ((listB[i + 20] - listB[i]) >= peak_pos1_der) //derivada
                {
                    peak_init_value = listB[i + 1];
                    // Si supera el umbral se empieza a buscar el pico mas alto
                    for (int j = i; fin != 1 && j < listA.Count - 100 && (j - i) < 50000; j++)
                    {
                        // Si la derivada negativa es menor que PND1, indicará que se ha llegado al pico.
                        if ((listB[j + 20] - listB[j]) <= peak_neg_der)
                        {
                            // Si el valor del pico es mayor que el umbral, se guarda el valor del pico y se marca en el gráfico
                            if ((listB[j] - peak_init_value) >= peak_threesold) //umbral de pico
                            {
                                pos_peaks.Add(j);
                                series.Points[i].MarkerStyle = MarkerStyle.Triangle;
                                series.Points[i].MarkerSize = 5;
                                series.Points[i].MarkerColor = Color.Green;
                                x0_peak.Add(i);
                                x1_peak.Add(j);
                                i = j;
                                // Se busca el final del pico con la derivada positiva PPD2 aprovechándo la característica de la línea base
                                for (int k = j; fin_2 != 1 && k < listA.Count - orden_filtro && (k - j) < 50000; k++)
                                {
                                    if ((listB[k + orden_filtro] - listB[k]) >= peak_pos2_der)  //fin del pico
                                    {
                                        series.Points[k].MarkerStyle = MarkerStyle.Triangle;
                                        series.Points[k].MarkerSize = 5;
                                        series.Points[k].MarkerColor = Color.Purple;
                                        fin_2 = 1;
                                        x2_peak.Add(k);
                                    }
                                }
                                fin_2 = 0;
                            }
                            fin = 1;
                        }
                    }
                    fin = 0;
                }
            }
        }
        // Esta función se utiliza para calcular el área de un pico
        public double area_calc(int n_x0, int n_x2)
        {
            double area = 0.0;
            int aux = 0;
            area = 0;
            // Cálculo del área por la regla del trapecio
            if (n_x0 > n_x2) 
            {
                aux = n_x0;
                n_x0=n_x2;
                n_x2= aux;
            }
            for (int j = n_x0; j < n_x2 && j + 1 < listB.Count; j++)
            {
                area += (Ts * 1e-3) * ((listB[j + 1] - listB[j]) / 2 + listB[j]);
            }
            // Resta del área del offset
            area -= (Ts * 1e-3) * (n_x2 - n_x0) * (listB[n_x0] + (listB[n_x2] - listB[n_x0]) / 2); //le resto el área del offset

            return area;
        }
        // Esta función se utiliza para el cálculo del área de cada pico y su representación
        public double area_plot()
        {
            double area = 0.0;

            // Cálculo del área por la regla de todos los picos
            for (int i = 0; i < x2_peak.Count; i++)
            {
                area = 0;

                // Cálculo del área de cada pico
                for (int j = x0_peak[i]; j < x2_peak[i] && j + 1 < listB.Count; j++)
                {
                    area += (Ts * 1e-3) * ((listB[j + 1] - listB[j]) / 2 + listB[j]);
                }
                area -= (Ts * 1e-3) * (x2_peak[i] - x0_peak[i]) * (listB[x0_peak[i]] + (listB[x2_peak[i]] - listB[x0_peak[i]]) / 2); //le resto el área del offset

                // Se marca el pico más alto y se representa el valor del área
                series.Points[x1_peak[i]].MarkerStyle = MarkerStyle.Circle;
                series.Points[x1_peak[i]].MarkerSize = 5;
                series.Points[x1_peak[i]].MarkerColor = Color.Red;
                string min = Math.Truncate((listA[x1_peak[i]] * Ts * 1e-3 / 60 - listA[0] * Ts * 1e-3 / 60)).ToString();
                string seg = Math.Truncate(((listA[x1_peak[i]] * Ts * 1e-3 / 60 - listA[0] * Ts * 1e-3 / 60) - Math.Truncate(listA[x1_peak[i]] * Ts * 1e-3 / 60 - listA[0] * Ts * 1e-3 / 60)) * 60).ToString();
                series.Points[x1_peak[i]].Label = "RT=" + min + ":" + seg
                    + "\n Area=" + Math.Round(area, 3).ToString(); ;
            }
            return area;
        }

        // Esta función lee un archivo y lo utiliza para generar una gráfica
        private void leer_y_plot(string FileName)
        {
            double s_filtrada = 0; // Variable para almacenar la señal filtrada
            int i = 0, a = 0; // Variables auxiliares para el filtrado
            using (var reader = new StreamReader(FileName)) // Abrir el archivo para leer
            {
                while (!reader.EndOfStream) // Leer el archivo mientras no se alcance el final
                {
                    var line = reader.ReadLine(); // Leer una línea del archivo
                    var values = line.Split(';'); // Separar los valores de la línea
                                                  // Si no es la última línea y el primer valor no está vacío
                    if (!reader.EndOfStream && values[0] != "")
                    {
                        Ts = Convert.ToInt32(values[2]); // Obtener la tasa de muestreo
                        filtro[i] = Convert.ToDouble(values[1]); // Añadir el valor al filtro
                        i++;
                        if (i == orden_filtro) i = 0; // Reinciar el filtro cuando se llegue al final
                        s_filtrada = 0;
                        for (a = 0; a < orden_filtro; a++)
                        {
                            s_filtrada += filtro[a]; // Calcular la suma de los valores del filtro
                        }
                        s_filtrada = s_filtrada / (orden_filtro); // Calcular el valor promedio de los valores del filtro

                        listA.Add(Convert.ToDouble(values[0])); // Añadir el valor de la muestra a la lista
                        listB.Add(s_filtrada); // Añadir el valor de la señal filtrada a la lista
                    }
                }

                series.Points.Clear(); // Limpiar los puntos de la serie
                chart1.Series.Clear(); // Limpiar las series del gráfico
                for (int j = 0; j < listA.Count; j++) // Recorrer las muestras
                {
                    double xValue = (j * 50) / (60.0 * 1000.0);  // Calcular el valor en segundos para el eje X
                    double yValue = listB[j];           // Obtener el valor de la muestra
                    series.Points.AddXY(xValue, yValue); // Añadir los valores al gráfico
                }
                chart1.Series.Add(series); // Añadir la serie al gráfico

                // Obtener el número total de muestras
                int totalSamples = series.Points.Count;

                // Calcular el valor máximo en minutos para el eje X
                double maxXValueInMinutes = totalSamples * 50.0 / 1000.0 / 60.0;

                // Definir el formato del label para mostrar en minutos en vez de nº muestras
                string labelFormat = "0.00' min'";

                // Asignar el nuevo label al eje X
                chart1.ChartAreas[0].AxisX.Title = "Tiempo (min)";
                chart1.ChartAreas[0].AxisX.LabelStyle.Format = labelFormat;

                chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
                chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
                chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
                chart1.ChartAreas[0].AxisY.MinorGrid.Enabled = false;

                chart1.Series["signal"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line; // Establecer el tipo de gráfico de la serie
                peak_finder();
                area_plot();
            }
        }

        // Esta función se ejecuta al cargar el formulario
        private void Form2_Load(object sender, EventArgs e)
        {
            // Asigna los valores de los parámetros a los campos de texto correspondientes
            textBox1.Text = peak_threesold.ToString();
            textBox2.Text = peak_pos1_der.ToString();
            textBox3.Text = peak_pos2_der.ToString();
            textBox4.Text = peak_neg_der.ToString();

            // Establece el directorio actual como directorio inicial del cuadro de diálogo de apertura de archivo
            openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
            // Establece las extensiones de archivo permitidas para el cuadro de diálogo
            openFileDialog1.Filter = "Database files (*.csv, *.txt, *.xlsm)|*.xlsm;*.csv;*.txt";
            openFileDialog1.FilterIndex = 0;

            // Si se selecciona un archivo en el cuadro de diálogo correctamente
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // Asigna la ruta del archivo seleccionado a la variable descriptor_r
                descriptor_r = openFileDialog1.FileName;
                // Asigna el directorio de trabajo a la ruta del archivo seleccionado
                dir_trabajo = openFileDialog1.InitialDirectory;
                // Restaura el directorio actual después de cerrar el cuadro de diálogo de apertura de archivo
                openFileDialog1.RestoreDirectory = true;

                // Lee el archivo seleccionado y grafica los datos
                leer_y_plot(descriptor_r);
            }
            else
            {
                // Si se cancela la selección de archivo, se cierra el formulario
                this.Close();
            }
        }

        private ToolTip tooltip = new ToolTip();
        private Point? clickPosition = null;

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            if (clickPosition.HasValue && e.Location != clickPosition)
            {
                tooltip.RemoveAll();
                clickPosition = null;
            }
        }

        private void chart1_MouseClick(object sender, MouseEventArgs e)
        {
            // Guardamos la posición del clic del usuario
            var pos = e.Location;
            clickPosition = pos;
            double area = 0;
            // Comprobamos si el usuario ha hecho clic en el área de trazado del gráfico
            var results = chart1.HitTest(pos.X, pos.Y, false, ChartElementType.PlottingArea);

            // Obtenemos el valor inicial de x del gráfico (suponemos que está almacenado en listA en la posición 1)
            x0 = Convert.ToInt32(listA[1]);

            // Recorremos los resultados obtenidos del método HitTest
            foreach (var result in results)
            {
                // Si el usuario ha hecho clic en el área de trazado...
                if (result.ChartElementType == ChartElementType.PlottingArea)
                {
                    // Obtenemos los valores de x e y del punto donde se hizo clic
                    Int32 xVal = (int)(60 * result.ChartArea.AxisX.PixelPositionToValue(pos.X) / ((double)Ts / 1000));
                    Int32 yVal = (int)result.ChartArea.AxisY.PixelPositionToValue(pos.Y);

                    // Modificamos el estilo y color del marcador correspondiente al punto donde se hizo clic
                    series.Points[xVal - x0].MarkerStyle = MarkerStyle.Diamond;
                    series.Points[xVal - x0].MarkerSize = 12;
                    series.Points[xVal - x0].MarkerColor = Color.DarkBlue;

                    // Convertimos el valor de x del punto donde se hizo clic a minutos y lo utilizamos como etiqueta para el punto
                    string min = Math.Truncate((Convert.ToDouble(xVal) * Ts * 1e-3 / 60 - Convert.ToDouble(x0) * Ts * 1e-3 / 60)).ToString();
                    string seg = Math.Truncate(((Convert.ToDouble(xVal) * Ts * 1e-3 / 60 - Convert.ToDouble(x0) * Ts * 1e-3 / 60) - Math.Truncate(Convert.ToDouble(xVal) * Ts * 1e-3 / 60 - Convert.ToDouble(x0) * Ts * 1e-3 / 60)) * 60).ToString();
                    series.Points[xVal - x0].Label = min + ":" + seg + yVal.ToString();

                    // Contador para alternar entre el primer y segundo clic del usuario
                    Console.WriteLine(contador);
                    if (contador == 2) contador = 0;

                    // Si es el primer clic, almacenamos el valor de x en la posición actual del array markers
                    if (contador == 0)
                    {
                        markers[marker_i] = xVal;
                        marker_i++;
                    }

                    // Si es el segundo clic, calculamos el área entre los dos valores de x y actualizamos la etiqueta del punto correspondiente
                    if (contador == 1)
                    {
                        markers[marker_i] = xVal;
                        area = area_calc(markers[marker_i - 1], markers[marker_i]);
                        min = Math.Truncate((Convert.ToDouble(xVal) * Ts * 1e-3 / 60 - Convert.ToDouble(x0) * Ts * 1e-3 / 60)).ToString();
                        seg = Math.Truncate(((Convert.ToDouble(xVal) * Ts * 1e-3 / 60 - Convert.ToDouble(x0) * Ts * 1e-3 / 60) - Math.Truncate(Convert.ToDouble(xVal) * Ts * 1e-3 / 60 - Convert.ToDouble(x0) * Ts * 1e-3 / 60)) * 60).ToString();
                        series.Points[markers[marker_i++] - x0].Label = "RT=" + min + ":" + seg + "\n Area=" + Math.Round(area, 3).ToString();
                    }

                    contador++;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)//clear   markers
        {
            // Itera entre todos los marcadores ya que para eliminarlos es necesario poner un marcador vacío en su posición
            for (int i = 0; i < x0_peak.Count; i++)
            {
                series.Points[x0_peak[i]].MarkerSize = 0;
                series.Points[x0_peak[i]].Label = "";
            }
            for (int i = 0; i < x1_peak.Count; i++)
            {
                series.Points[x1_peak[i]].MarkerSize = 0;
                series.Points[x1_peak[i]].Label = "";
            }
            for (int i = 0; i < x2_peak.Count; i++)
            {
                series.Points[x2_peak[i]].MarkerSize = 0;
                series.Points[x2_peak[i]].Label = "";
            }
            x1_peak.Clear();
            x2_peak.Clear();
            x0_peak.Clear();
        }

        private void button2_Click(object sender, EventArgs e)// Botón calculo de picos
        {
            peak_finder();
            area_plot();
        }

        private void button3_Click(object sender, EventArgs e) // Boton retroceder
        {
            // Si hay marcadores, eliminar el último marcador y actualizar el contador
            if (marker_i > 0)
            {
                series.Points[markers[marker_i - 1] - x0].MarkerSize = 0;
                series.Points[markers[marker_i - 1] - x0].Label = "";
                marker_i--;
            }
            if (contador == 2) contador--;
            else if (contador == 1) contador--;
        }

        private string[] fileEntr;
        private Document doc_l = new Document(iTextSharp.text.PageSize.A4, 10, 10, 42, 35);
        private bool ajuste = false;

        private void button4_Click(object sender, EventArgs e)  // Botón PDF siguiente
        {
            // Si aún no se ha realizado el ajuste, leer y graficar el primer archivo CSV y crear un nuevo documento PDF
            if (ajuste == false)
            {
                var files = Directory.EnumerateFiles(Path.GetDirectoryName(descriptor_r), "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".csv"));
                fileEntr = files.ToArray();

                // Limpiar los datos del gráfico y leer los datos del primer archivo CSV
                chart1.Series.Clear();
                series.Points.Clear();
                x0_peak.Clear();
                x1_peak.Clear();
                x2_peak.Clear();
                pos_peaks.Clear();
                listA.Clear();
                listB.Clear();
                leer_y_plot(fileEntr[0]);

                // Crear un nuevo archivo PDF y abrirlo para escritura
                ajuste = true;
                PdfWriter wri = PdfWriter.GetInstance(doc_l, new FileStream(Path.GetDirectoryName(descriptor_r) + "\\test.pdf", FileMode.Create));
                doc_l.Open();
                return;
            }

            // Si se han procesado todos los archivos CSV, guardar el último gráfico en el PDF y cerrar el documento
            if (n_pdf == fileEntr.Length - 1)
            {
                var chartimage = new MemoryStream();
                chart1.SaveImage(chartimage, ChartImageFormat.Png);
                iTextSharp.text.Image Chart_image = iTextSharp.text.Image.GetInstance(chartimage.GetBuffer());
                Chart_image.ScalePercent(55);
                Paragraph paragraph = new Paragraph("muestra" + (n_pdf).ToString());
                doc_l.Add(paragraph);
                doc_l.Add(Chart_image);
                doc_l.Close();

                // Abrir el archivo PDF en la aplicación predeterminada y cerrar la aplicación actual
                Process.Start(Path.GetDirectoryName(descriptor_r) + "/test.pdf");
                this.Close();
            }
            else
            {
                // Si todavía hay archivos CSV por procesar, agregar la siguiente página del PDF con el siguiente gráfico
                añadir_pdf(fileEntr[n_pdf + 1], fileEntr.Length, doc_l);
            }
        }

        private UInt16 presc = 0;

        private bool añadir_pdf(string fileName, int count, Document doc)
        {
            // Crear un MemoryStream para almacenar la imagen del gráfico
            var chartimage = new MemoryStream();

            // Guardar la imagen del gráfico en el MemoryStream
            chart1.SaveImage(chartimage, ChartImageFormat.Png);

            // Crear una instancia de iTextSharp Image a partir del MemoryStream para añadir la imagen al documento
            iTextSharp.text.Image Chart_image = iTextSharp.text.Image.GetInstance(chartimage.GetBuffer());

            // Escalar la imagen al 55%
            Chart_image.ScalePercent(55);

            // Crear un nuevo párrafo con un texto que incluye el número de muestra actual
            Paragraph paragraph = new Paragraph("muestra" + (n_pdf).ToString());

            // Agregar el párrafo y la imagen del gráfico al documento
            doc.Add(paragraph);
            doc.Add(Chart_image);

            // Incrementar el contador de presc para controlar el número de muestras por página
            presc++;

            // Si el número de muestras por página ha llegado a 2, agregar una nueva página al documento
            if (presc == 2)
            {
                doc.NewPage();
                presc = 0;
            }

            // Limpiar los datos del gráfico y leer los datos del archivo CSV actual para el siguiente gráfico
            chart1.Series.Clear();
            series.Points.Clear();
            x0_peak.Clear();
            x1_peak.Clear();
            x2_peak.Clear();
            pos_peaks.Clear();
            listA.Clear();
            listB.Clear();
            leer_y_plot(fileName);

            // Incrementar el contador de muestra química para el siguiente gráfico
            n_pdf++;

            // Si el contador de muestra ha llegado a count, devolver false para detener la creación de más gráficos y PDFs
            if (n_pdf == count)
                return false;
            else
                return true;
        }

        private void button5_Click(object sender, EventArgs e)  //todoPDF
        {
            // Obtener todos los archivos CSV en el directorio del archivo descriptor_r y en todos sus subdirectorios
            var files = Directory.EnumerateFiles(Path.GetDirectoryName(descriptor_r), "*.*", SearchOption.AllDirectories)
                        .Where(s => s.EndsWith(".csv"));

            // Convertir el array de array de archivos en array de cadenas
            string[] fileEntries = files.ToArray();

            // Iterar a través de los archivos y hacer clic en el botón "PDF siguiente" para cada uno
            for (Int16 k = 0; k < fileEntries.Length + 1; k++)
                button4_Click(sender, e);
        }

        private void button6_Click(object sender, EventArgs e)//Boton PDF next
        {
            button4_Click(sender, e);
        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {
            peak_threesold = Convert.ToDouble(textBox1.Text);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            peak_pos1_der = Convert.ToDouble(textBox2.Text);
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            peak_pos2_der = Convert.ToDouble(textBox3.Text);
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            peak_neg_der = Convert.ToDouble(textBox4.Text);
        }

    }
}