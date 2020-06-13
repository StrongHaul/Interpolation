using System;
using System.Drawing;
using System.Windows.Forms;
using ZedGraph;
using System.Linq;
using System.Collections;
using System.IO;


namespace lab2
{
	public partial class Form1 : Form
	{
		double[] X = new double[1];
		double[] Y = new double[1];
		static double[] Xinter = new double[0];
		double[] Yinter = new double[0];
		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			GraphPane pane = z1.GraphPane;

			z1.GraphPane.CurveList.Clear();
			z1.IsShowPointValues = true;
			z1.IsEnableHZoom = true;
			z1.IsEnableVZoom = true;

			pane.XAxis.Title.IsVisible = false;
			pane.YAxis.Title.IsVisible = false;
			pane.XAxis.Scale.IsSkipFirstLabel = true;
			pane.XAxis.Scale.IsSkipLastLabel = true;
			pane.XAxis.Scale.IsSkipCrossLabel = true;
			pane.YAxis.Scale.IsSkipLastLabel = true;
			pane.YAxis.Scale.IsSkipCrossLabel = true;
			pane.Title.IsVisible = false;

			pane.XAxis.Cross = 0.0;
			pane.YAxis.Cross = 0.0;

			//pane.XAxis.Scale.Min = X.Min();       // левая граница масштаба
			//pane.XAxis.Scale.Max = X.Max();       // правая граница масштаба
			//pane.YAxis.Scale.Min = Y.Min();      // По оси Y установим автоматический подбор масштаба
			//pane.YAxis.Scale.Max = Y.Max();

			pane.IsBoundedRanges = true;    // при автоматическом подборе масштаба нужно учитывать только видимый интервал графика

			z1.AxisChange();
			z1.Invalidate();
		}
		private void Button2_Click(object sender, EventArgs e)      // выбрать файл
		{
			if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
				return;
			dataGridView1.Rows.Clear();
			Array.Resize(ref X, 0);             // обнулили длину массива   
			Array.Resize(ref Y, 0);

			string filename = openFileDialog1.FileName;
			StreamReader filereader = new StreamReader(filename);
			int i = 0;
			while (filereader.EndOfStream != true)
			{
				string[] line = filereader.ReadLine().Replace(" ", "").Split(new string[] { "," }, StringSplitOptions.None);     // строка файла "x, y"
				Array.Resize(ref X, X.Length + 1);             // увеличили длину массива на 1 
				Array.Resize(ref Y, Y.Length + 1);
				X[i] = Convert.ToDouble(line[0]);
				Y[i] = Convert.ToDouble(line[1]);
				dataGridView1.Rows.Add(X[i], Y[i]);

				//dataGridView1.Rows[i].Cells[0].Value = X[i];    // добавить x в таблицу
				//dataGridView1.Rows[i].Cells[1].Value = Y[i];    // добавить y в таблицу
				i++;
			}
			filereader.Close();
		}

		private void button1_Click(object sender, EventArgs e)      // построить
		{
			z1.GraphPane.CurveList.Clear();
			double x = Convert.ToDouble(textBox1.Text);

			Array.Resize(ref Xinter, 0);
			Array.Resize(ref Yinter, 0);

			var res = (0.0, new double[Xinter.Length]);
			switch (comboBox1.SelectedIndex)
			{
				case 0:
					res = canon(x);
					break;
				case 1:
					res = liney(x);
					break;
				case 2:
					res = lagrange(x);
					break;
				case 3:
					res = newton(x);
					break;
			}

			textBox2.Text = Math.Round(res.Item1, 2).ToString();        // вывести высчитанную Y точку
			res.Item2.CopyTo(Yinter, 0);            // скопировать данные в массив Yinter

			LineItem points, line;
			points = z1.GraphPane.AddCurve("Исх. данные", X, Y, Color.Green, SymbolType.Circle);       // точки
			points.Symbol.Fill.Color = Color.Green;         // цвет заливки
			points.Symbol.Fill.Type = FillType.Solid;       // тип заливки
			points.Line.IsVisible = false;

			line = z1.GraphPane.AddCurve("Интерполяция", Xinter, Yinter, Color.Blue, SymbolType.None);       // линия
			line.Line.Width = 2;

			z1.AxisChange();
			z1.Invalidate();
		}

		public (double, double[]) canon(double x)       // каноническая
		{
			for (double i = 0, k = X.Min(); k <= X.Max(); k += 0.1, i++)
			{
				Array.Resize(ref Xinter, Xinter.Length + 1);
				Xinter[(int)i] = k;
			}
			Array.Resize(ref Yinter, Xinter.Length);

			int N = X.Length;

			(double, double[]) res = (0, new double[Xinter.Length]);     // кортеж (Yточка, Yинтерп)
			double[] A = new double[N];                          // массив для хранения коэфф А

			double[,] Xmatrix = new double[N, N + 1];       // расширенная матрица
			for (int i = 0; i < N; i++)                     // заполняем расширенную матрицу
				for (int j = 0; j < N + 1; j++)
				{
					if (j < N)
						Xmatrix[i, j] = Math.Pow(X[i], j);
					else
						Xmatrix[i, j] = Y[i];
				}

			// прямой ход
			for (int i = 0; i < N - 1; ++i)
			{
				double element_of_glavn = Xmatrix[i, i];       // элемент главной диагонали

				for (int s = i + 1; s < N; s++)
				{
					double obnul = Xmatrix[s, i];       // элемент строки который нужно обнулить
					for (int j = i; j < N + 1; j++)
						Xmatrix[s, j] += Xmatrix[i, j] * (-obnul / element_of_glavn);
				}
			}

			//обратный ход            
			for (int i = N - 1; i > 0; i--)
			{
				double element_of_glavn = Xmatrix[i, i];       // элемент главной диагонали

				for (int s = i - 1; s >= 0; s--)
				{
					double obnul = Xmatrix[s, i];       // элемент строки который нужно обнулить
					for (int j = N; j >= 0; j--)
						Xmatrix[s, j] += Xmatrix[i, j] * (-obnul / element_of_glavn);
				}
			}

			for (int i = 0; i < N; i++)
				A[i] = Xmatrix[i, N] / Xmatrix[i, i];

			for (int i = 0; i < Xinter.Length; i++)
				for (int j = 0; j < N; j++)
					res.Item2[i] += A[j] * Math.Pow(Xinter[i], j);

			for (int j = 0; j < N; j++)
				res.Item1 += A[j] * Math.Pow(x, j);

			return res;
		}

		public (double, double[]) liney(double x)       // линейная
		{
			Array.Resize(ref Xinter, X.Length);
			Array.Resize(ref Yinter, Y.Length);

			Array.Copy(X, Xinter, X.Length);
			Array.Copy(Y, Yinter, Y.Length);

			(double, double[]) res = (0, new double[Xinter.Length]);     // кортеж (Yточка, Yинтерп)
			Array.Copy(Y, res.Item2, Y.Length);

			double[] A = new double[X.Length];
			double[] B = new double[X.Length];

			for (int i = 0; i < X.Length - 1; i++)
			{
				A[i] = (Y[i + 1] - Y[i]) / (X[i + 1] - X[i]);
				B[i] = Y[i] - A[i] * X[i];
				if (X[i] <= x && x <= X[i + 1])
					res.Item1 = A[i] * x + B[i];
			}

			return res;
		}

		public (double, double[]) lagrange(double x)    // лагранж
		{
			for (double i = 0, k = X.Min(); k <= X.Max(); k += 0.1, i++)
			{
				Array.Resize(ref Xinter, Xinter.Length + 1);
				Xinter[(int)i] = k;
			}
			Array.Resize(ref Yinter, Xinter.Length);

			(double, double[]) res = (0, new double[Xinter.Length]);     // кортеж (Yточка, Yинтерп)

			for (int i = 0; i < Xinter.Length; i++)
				res.Item2[i] = lagrangeforpoint(Xinter[i], X, Y, X.Length);

			double lagrangeforpoint(double xinter, double[] xValues, double[] yValues, int size)
			{
				double lagrangePol = 0;
				for (int i = 0; i < size; i++)
				{
					double basicsPol = 1;

					for (int j = 0; j < size; j++)
						if (j != i)
							basicsPol *= (xinter - xValues[j]) / (xValues[i] - xValues[j]);

					lagrangePol += basicsPol * yValues[i];
				}
				return lagrangePol;
			}
			res.Item1 = lagrangeforpoint(x, X, Y, X.Length);
			return res;
		}

		public (double, double[]) newton(double x)      // ньютон
		{
			for (double i = 0, k = X.Min(); k <= X.Max(); k += 0.1, i++)
			{
				Array.Resize(ref Xinter, Xinter.Length + 1);
				Xinter[(int)i] = k;
			}
			Array.Resize(ref Yinter, Xinter.Length);

			(double, double[]) res = (0, new double[Xinter.Length]);     // кортеж (Yточка, Yинтерп)

			for (int i = 0; i < Xinter.Length; i++)
				res.Item2[i] = newton_for_point(Xinter[i], X.Length - 1, X, Y, 1);

			double newton_for_point(double xinter, int n, double[] MasX, double[] MasY, double step)
			{
				double[,] mas = new double[n + 2, n + 1];
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < n + 1; j++)
					{
						if (i == 0)
							mas[i, j] = MasX[j];
						else if (i == 1)
							mas[i, j] = MasY[j];
					}
				}
				int m = n;
				for (int i = 2; i < n + 2; i++)
				{
					for (int j = 0; j < m; j++)
					{
						mas[i, j] = mas[i - 1, j + 1] - mas[i - 1, j];
					}
					m--;
				}

				double[] dy0 = new double[n + 1];

				for (int i = 0; i < n + 1; i++)
				{
					dy0[i] = mas[i + 1, 0];
				}

				double result = dy0[0];
				double[] xn = new double[n];
				xn[0] = xinter - mas[0, 0];

				for (int i = 1; i < n; i++)
				{
					double ans = xn[i - 1] * (xinter - mas[0, i]);
					xn[i] = ans;
					ans = 0;
				}

				int m1 = n + 1;
				int fact = 1;
				for (int i = 1; i < m1; i++)
				{
					fact = fact * i;
					result = result + (dy0[i] * xn[i - 1]) / (fact * Math.Pow(step, i));
				}

				return result;
			}
			res.Item1 = newton_for_point(x, X.Length - 1, X, Y, 1);
			return res;
		}

	}
}
