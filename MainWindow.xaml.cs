using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LectorIAPDF
{
    public class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        private TextBox txtRespuesta;
        private TextBox txtPregunta;
        private ListBox lstFuentes;
        private Button btnEnviar;

        public MainWindow()
        {
            Title = "Asistente de IA para PDFs";
            Height = 600;
            Width = 800;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Grid gridPrincipal = new Grid { Margin = new Thickness(20) };
            gridPrincipal.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            gridPrincipal.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            gridPrincipal.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock lblTitulo = new TextBlock
            {
                Text = "💬 Consulta tus PDFs con Inteligencia Artificial",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937"))
            };
            Grid.SetRow(lblTitulo, 0);
            gridPrincipal.Children.Add(lblTitulo);

            Grid gridCentral = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            gridCentral.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            gridCentral.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            txtRespuesta = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontSize = 14,
                Padding = new Thickness(10),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB")),
                BorderThickness = new Thickness(1)
            };
            Grid.SetColumn(txtRespuesta, 0);
            gridCentral.Children.Add(txtRespuesta);

            GroupBox grpFuentes = new GroupBox
            {
                Header = "📄 Fuentes utilizadas",
                Margin = new Thickness(15, 0, 0, 0),
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563"))
            };
            lstFuentes = new ListBox { Background = Brushes.White, BorderThickness = new Thickness(0) };
            grpFuentes.Content = lstFuentes;
            Grid.SetColumn(grpFuentes, 1);
            gridCentral.Children.Add(grpFuentes);

            Grid.SetRow(gridCentral, 1);
            gridPrincipal.Children.Add(gridCentral);

            Grid gridInferior = new Grid();
            gridInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            txtPregunta = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB")),
                BorderThickness = new Thickness(1)
            };
            txtPregunta.KeyDown += txtPregunta_KeyDown;
            Grid.SetColumn(txtPregunta, 0);
            gridInferior.Children.Add(txtPregunta);

            btnEnviar = new Button
            {
                Content = "Preguntar a la IA",
                Width = 130,
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0)
            };
            btnEnviar.Click += btnEnviar_Click;
            Grid.SetColumn(btnEnviar, 1);
            gridInferior.Children.Add(btnEnviar);

            Grid.SetRow(gridInferior, 2);
            gridPrincipal.Children.Add(gridInferior);

            this.Content = gridPrincipal;

            txtPregunta.IsEnabled = false;
            btnEnviar.IsEnabled = false;
            txtRespuesta.Text = "⏳ Esperando al servidor de Python... Aguanta un momento.";
            
            _ = VerificarConexionServidor();
            
           }
        private async Task VerificarConexionServidor()
        {
            string hostBase = "127.0.0.1";
            string puertoBase = "8000";
            string urlTest = "http://" + hostBase + ":" + puertoBase + "/";
            bool conectado = false;
            
            while (!conectado)
            {
                try
                {
                    HttpResponseMessage testResponse = await client.GetAsync(urlTest);
                    conectado = true; 
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }

            Dispatcher.Invoke(() => {
                txtPregunta.IsEnabled = true;
                btnEnviar.IsEnabled = true;
                txtRespuesta.Text = "🚀 ¡Listo! Ya podés escribir tu consulta abajo.";
            });
        }

        private async void btnEnviar_Click(object sender, RoutedEventArgs e)
        {
            await RealizarConsulta();
        }

        private async void txtPregunta_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnEnviar.IsEnabled)
            {
                await RealizarConsulta();
            }
        }

        private async Task RealizarConsulta()
        {
            string preguntaUsuario = txtPregunta.Text.Trim();
            if (string.IsNullOrEmpty(preguntaUsuario)) return;

            btnEnviar.IsEnabled = false;
            txtPregunta.IsEnabled = false;
            txtRespuesta.Text = "⚡ Consultando a Groq...";
            lstFuentes.Items.Clear();

            try
            {
                var datosConsulta = new { pregunta = preguntaUsuario };
                string jsonEnvio = JsonConvert.SerializeObject(datosConsulta);
                var contenido = new StringContent(jsonEnvio, Encoding.UTF8, "application/json");

                string hostBase = "127.0.0.1";
                string puertoBase = "8000";
                string endpointBase = "preguntar";
                string urlCompleta = "http://" + hostBase + ":" + puertoBase + "/" + endpointBase;

                HttpResponseMessage respuestaServidor = await client.PostAsync(urlCompleta, contenido);

                if (respuestaServidor.IsSuccessStatusCode)
                {
                    string jsonRespuesta = await respuestaServidor.Content.ReadAsStringAsync();
                    var resultadoApi = JsonConvert.DeserializeAnonymousType(jsonRespuesta, new { respuesta = "", fuentes = new List<string>() });
                    txtRespuesta.Text = resultadoApi.respuesta;
                    
                    foreach (var fuente in resultadoApi.fuentes)
                    {
                        lstFuentes.Items.Add(fuente);
                    }
                }
            }
            catch (Exception ex)
            {
                txtRespuesta.Text = "❌ Error de conexión con Python.\n\nDetalle: " + ex.Message;
            }
            finally
            {
                btnEnviar.IsEnabled = true;
                txtPregunta.IsEnabled = true;
                txtPregunta.Clear();
            }
        }
    }
}
