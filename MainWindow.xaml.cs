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
using System.Windows.Documents;
using MdXaml;

namespace LectorIAPDF
{
    public class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        
        private MarkdownScrollViewer viewerRespuesta; 
        private TextBox txtPregunta;
        private ListBox lstFuentes;
        private Button btnEnviar;
        private Button btnExportar;

        public MainWindow()
        {
            Title = "Asistente de IA para PDFs y XMLs";
            Height = 700; 
            Width = 950; 
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Grid gridPrincipal = new Grid { Margin = new Thickness(20) };
            gridPrincipal.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            gridPrincipal.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            gridPrincipal.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock lblTitulo = new TextBlock
            {
                Text = "💬 Consulta tus PDFs y XMLs con Inteligencia Artificial",
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

            viewerRespuesta = new MarkdownScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(15)
            };
            
            var estiloMarkdown = new Style(typeof(Table));
            estiloMarkdown.Setters.Add(new Setter(Table.CellSpacingProperty, 0.0));
            viewerRespuesta.Resources.Add(typeof(Table), estiloMarkdown);

            viewerRespuesta.Markdown = "⏳ Esperando al servidor de Python... Aguanta un momento.";

            Grid.SetColumn(viewerRespuesta, 0);
            gridCentral.Children.Add(viewerRespuesta);
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
            gridInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            gridInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });

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
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0)
            };
            btnEnviar.Click += btnEnviar_Click;
            Grid.SetColumn(btnEnviar, 1);
            gridInferior.Children.Add(btnEnviar);

            btnExportar = new Button
            {
                Content = "Exportar a Excel",
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")), 
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0)
            };
            btnExportar.Click += btnExportar_Click;
            Grid.SetColumn(btnExportar, 2);
            gridInferior.Children.Add(btnExportar);

            Grid.SetRow(gridInferior, 2);
            gridPrincipal.Children.Add(gridInferior);

            this.Content = gridPrincipal;

            txtPregunta.IsEnabled = false;
            btnEnviar.IsEnabled = false;
            btnExportar.IsEnabled = false; 
            
            _ = VerificarConexionServidor();
        }

        private void EstablecerTextoEnVisor(string texto)
        {
            viewerRespuesta.Markdown = texto;
        }

        private async Task VerificarConexionServidor()
        {
            // Forzamos la conexión por localhost para evadir bloqueos de Firewall
            string urlTest = "http://localhost:8000/";
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
                EstablecerTextoEnVisor("🚀 ¡Listo! Ya podés escribir tu consulta abajo.");
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

        private async void btnExportar_Click(object sender, RoutedEventArgs e)
        {
            btnExportar.IsEnabled = false;
            try
            {
                string urlExportar = "http://localhost:8000/exportar";
                HttpResponseMessage respuesta = await client.PostAsync(urlExportar, null);
                
                if (respuesta.IsSuccessStatusCode)
                {
                    MessageBox.Show("🚀 ¡Tabla exportada con éxito! Revisá tu Escritorio (Tabla_Exportada_IA.xlsx).", "Reporte Generado", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("⚠️ No se encontraron tablas estructuradas en la última respuesta para exportar o el servidor falló.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnExportar.IsEnabled = true;
            }
        }

        private async Task RealizarConsulta()
        {
            string pregunta = txtPregunta.Text.Trim();
            if (string.IsNullOrEmpty(pregunta)) return;

            txtPregunta.Text = "";
            txtPregunta.IsEnabled = false;
            btnEnviar.IsEnabled = false;
            btnExportar.IsEnabled = false;
            lstFuentes.Items.Clear();
            EstablecerTextoEnVisor("🔍 Pensando... Buscando en los documentos y procesando la información.");

            try
            {
                string urlPregunta = "http://localhost:8000/preguntar";
                var payload = new { pregunta = pregunta };
                string jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(urlPregunta, content);
                
                if (response.IsSuccessStatusCode)
                {
                    string jsonRespuesta = await response.Content.ReadAsStringAsync();
                    var resultado = JsonConvert.DeserializeObject<RespuestaIA>(jsonRespuesta);

                    viewerRespuesta.Markdown = resultado.respuesta;

                    if (resultado.fuentes != null)
                    {
                        foreach (var fuente in resultado.fuentes)
                        {
                            lstFuentes.Items.Add(fuente);
                        }
                    }

                    btnExportar.IsEnabled = true;
                }
                else
                {
                    EstablecerTextoEnVisor("⚠️ El servidor de Python devolvió un error al procesar tu consulta.");
                }
            }
            catch (Exception ex)
            {
                EstablecerTextoEnVisor($"❌ Error de conexión con el backend: {ex.Message}");
            }
            finally
            {
                txtPregunta.IsEnabled = true;
                btnEnviar.IsEnabled = true;
                txtPregunta.Focus();
            }
        }
    }

    public class RespuestaIA
    {
        public string respuesta { get; set; }
        public List<string> fuentes { get; set; }
    }
}
