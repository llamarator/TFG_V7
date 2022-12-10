//using OxyPlot.Series;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public const int orden_filtro = 500;
        public const double peak_threesold = 0.015;
        public Int32 x0 = 0, x1 = 0, x2 = 0;
        public Int16 contador = 0;
        public Int32[] markers = new Int32[2];
        List<double> listA = new List<double>();
        List<double> listB = new List<double>();
        double[] filtro = new double[orden_filtro];
        public string descriptor_r;
        public string descriptor_w;
        public string nombre, dir_trabajo;
        public string path_estandar;

        Series series =  new Series("signal");

        public Int32 n_peaks = 0;
        public int init_peak = 0;
        public double peak_init_value = 0.0;
        List<int> x0_peak = new List<int>();        //inicio de pico
        List<int> x1_peak = new List<int>();        //pos pico
        List<int> x2_peak = new List<int>();        //fin pico
        List<double> pos_peaks = new List<double>();

        void peak_finder()
        {
            int fin = 0;
            int fin_2 = 0;
            for (int i = 20000; i < listA.Count-500; i++)
            {
                if ((listB[i + 500] - listB[i]) >= 0.0001) //derivada
                {
                    peak_init_value = listB[i + 1];
                    for (int j = i; fin != 1 && j< listA.Count - 100 && (j-i) <50000; j++)
                    {
                        if ((listB[j + 100] - listB[j]) <= -0.001) 
                        {
                            
                            if ((listB[j] - peak_init_value) >= peak_threesold)
                            {
                                
                                pos_peaks.Add(j);
                                //series.Points[j].MarkerStyle = MarkerStyle.Circle;
                                //series.Points[j].MarkerSize = 5;
                                //series.Points[j].MarkerColor = Color.Red;
                                //series.Points[j].Label = Math.Round((listA[j] / 6000 - listA[0] / 6000), 2).ToString();

                                series.Points[i].MarkerStyle = MarkerStyle.Triangle;
                                series.Points[i].MarkerSize = 5;
                                series.Points[i].MarkerColor = Color.Green;
                                x0_peak.Add(i);
                                x1_peak.Add(j);

                                //fin = 1;
                                i = j;

                                for (int k = j; fin_2 != 1 && k < listA.Count - 500 && (k - j) < 50000; k++)
                                {
                                    if ((listB[k + orden_filtro] - listB[k]) >= 0.001)
                                    {
                                       // if (area_calc(i, k) > 0.5)
                                        //{



                                            series.Points[k].MarkerStyle = MarkerStyle.Triangle;
                                            series.Points[k].MarkerSize = 5;
                                            series.Points[k].MarkerColor = Color.Purple;
                                            fin_2 = 1;
                                            x2_peak.Add(k);
                                        //}
                                        //else fin_2 = 1;

                                    }

                                }
                                //if (area_calc(i, x2_peak[x2_peak.Count-1]) > 0.5)
                                //{
                                //    x0_peak.Add(i);
                                //    x1_peak.Add(j);

                                //    //fin = 1;
                                //    i = j;
                                //}
                                fin_2 = 0;
                            }
                            fin = 1;
                        }
                    }
                    fin = 0;
                }

            }

        }
        public double area_calc(int n_x0,int n_x2)
        {

            double area = 0.0;
                area = 0;
                for (int j = n_x0; j < n_x2 && j + 1 < listB.Count; j++)
                {
                    area += (10e-3) * ((listB[j + 1] - listB[j]) / 2 + listB[j]);
                }
                area -= (10e-3) * (n_x2 - n_x0) * listB[n_x0]; //le resto el área del offset
            
            return area;

        }
        public double area_plot()
        {
            
            double area = 0.0;
            for (int i = 0; i < x2_peak.Count; i++)
            {
                area = 0;
                for (int j = x0_peak[i]; j < x2_peak[i] && j+1 < listB.Count; j++)
                {
                    area += (10e-3) * ((listB[j + 1] - listB[j]) / 2 + listB[j]);
                }
                area -= (10e-3) * (x2_peak[i] - x0_peak[i]) * listB[x0_peak[i]]; //le resto el área del offset
                series.Points[x1_peak[i]].MarkerStyle = MarkerStyle.Circle;
                series.Points[x1_peak[i]].MarkerSize = 5;
                series.Points[x1_peak[i]].MarkerColor = Color.Red;
                series.Points[x1_peak[i]].Label = Math.Round(((listA[x1_peak[i]]- listA[0]) / 6000), 2).ToString()+ "\n Area=" + Math.Round(area,3).ToString(); ;
                //chart1.ClientRectangle.
            }
            return area;
        }


        private void Form2_Load(object sender, EventArgs e)
        {
            int i = 0,a=0;
            double s_filtrada = 0;
            //openFileDialog1.InitialDirectory = "c:\\";      C:\Users\David\Desktop\TFG\recepcionCOM
            openFileDialog1.InitialDirectory = "C:\\Users\\David\\Desktop\\TFG\\recepcionCOM\\C_sharp\\com_V5\\bin\\Debug";
            openFileDialog1.Filter = "Database files (*.csv, *.txt, *.xlsm)|*.xlsm;*.csv;*.txt";
            openFileDialog1.FilterIndex = 0;
            //openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                descriptor_r = openFileDialog1.FileName;
                dir_trabajo = openFileDialog1.InitialDirectory;
                openFileDialog1.RestoreDirectory = true;
                using (var reader = new StreamReader(descriptor_r))
                {

                    // var plt = new ScottPlot.Plot(600, 400);
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(';');

                        if (!reader.EndOfStream && values[0] != "")
                        {

                            filtro[i] = Convert.ToDouble(values[1]);
                            i++;
                            if (i == orden_filtro) i = 0;
                            s_filtrada = 0;
                            for (a = 0; a < orden_filtro; a++)
                            {
                                s_filtrada += filtro[a];
                            }
                            s_filtrada = s_filtrada / orden_filtro;

                            listA.Add(Convert.ToDouble(values[0]));
                            listB.Add(s_filtrada / 1000000.0);
                        }
                    }

                    chart1.Series.Clear();


                    // Frist parameter is X-Axis and Second is Collection of Y- Axis
                    //series.ToolTip = ;//borrar
                    series.Points.DataBindXY(listA, listB);

                    //chart1.Series.Add("helloworld");
                    chart1.Series.Add(series);

                    //chart1.Series[0].IsValueShownAsLabel = true;
                    chart1.Series[0].IsVisibleInLegend = true;
                    chart1.Series[0].LegendToolTip = "aaaaaaaaaaaaaaaaa";
                    chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
                    chart1.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
                    chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
                    chart1.ChartAreas[0].AxisY.MinorGrid.Enabled = false;
                    //  this.chartControl1.ZoomType = ZoomType.MouseWheelZooming;

                    series.Points[1000].MarkerStyle = MarkerStyle.Circle;
                    series.Points[1000].MarkerSize = 5;
                    series.Points[1000].MarkerColor = Color.Red;
                    //chart1.Series[0].ToolTip = " #VALX ; #VAL";

                    chart1.Series["signal"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                    peak_finder();
                    area_plot();

                }
            }
            else this.Close();

        }
        ToolTip tooltip = new ToolTip();
        Point? clickPosition = null;

        void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            if (clickPosition.HasValue && e.Location != clickPosition)
            {
                tooltip.RemoveAll();
                clickPosition = null;
            }
        }

        void chart1_MouseClick(object sender, MouseEventArgs e)
        {
            var pos = e.Location;
            clickPosition = pos;
            double area = 0;
            var results = chart1.HitTest(pos.X, pos.Y, false,ChartElementType.PlottingArea);
            x0 = Convert.ToInt32(listA[1]);
            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.PlottingArea)
                {
                    Int32 xVal = (int)result.ChartArea.AxisX.PixelPositionToValue(pos.X);
                    Int32 yVal = (int)result.ChartArea.AxisY.PixelPositionToValue(pos.Y);
                    
                    series.Points[xVal - x0 ].MarkerStyle = MarkerStyle.Diamond;
                    series.Points[xVal - x0].MarkerSize = 12;
                    series.Points[xVal - x0].MarkerColor = Color.DarkBlue;
                    series.Points[xVal - x0].Label = Math.Round((Convert.ToDouble(xVal) / 6000 - Convert.ToDouble(x0)/6000),2).ToString(); //conversion de muestras a minutos
                    tooltip.Show("X=" + Convert.ToDouble(xVal) /6000 + ", Y=" + Convert.ToDouble(yVal), this.chart1, e.Location.X, e.Location.Y - 15);
                    Console.WriteLine(contador);
                    if (contador == 2)
                    {
                        contador = 0;
                        series.Points[markers[0] - x0].MarkerSize = 0;
                        series.Points[markers[0] - x0].MarkerColor = Color.White;
                        series.Points[markers[0] - x0].Label = "";
                        series.Points[markers[1] - x0].MarkerSize = 0;
                        series.Points[markers[1] - x0].MarkerColor = Color.White;
                        series.Points[markers[1] - x0].Label = "";
                        //chart1.Series["signal"].EmptyPointStyle.MarkerStyle = MarkerStyle.None;
                    }
                    if (contador == 0) markers[0] = xVal;
                    if (contador == 1)
                    {
                        markers[1] = xVal;
                        for (int j = (markers[0]-x0); j < (markers[1]-x0) && j + 1 < listB.Count; j++)
                        {
                            area += (10e-3) * ((listB[j + 1] - listB[j]) / 2 + listB[j]);
                        }
                        area -= (10e-3) * (markers[1] - markers[0]) * listB[markers[0]-x0]; //le resto el área del offset
                        series.Points[markers[1] - x0].Label = Math.Round((Convert.ToDouble(xVal) / 6000 - Convert.ToDouble(x0) / 6000), 2).ToString()+
                            "\n Area=" + Math.Round(area, 3).ToString(); 

                    }
                   
                    contador++;

                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void chart1_Click(object sender, EventArgs e)
        {
            //chart1.ChartAreas[0].AxisX.PixelPositionToValue(e.X);
            //chart1.ChartAreas[0].AxisY.PixelPositionToValue(e.Y);
        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }
    }
}
