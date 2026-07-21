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
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        
        private MarkdownScrollViewer viewerRespuesta; 
        private TextBox txtPregunta;
        private ListBox lstFuentes;
        private ListBox lstArchivosDisponibles; // Panel para los PDFs reales en el disco
        private Button btnEnviar;
        private Button btnExportar;
        private Button btnLimpiar; // Botón para resetear la sesión técnica

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

            // ==========================================================
            // 📁 DISEÑO ESTRUCTURAL DEL PANEL LATERAL DOBLE
            // ==========================================================
            Grid gridLateralDerecho = new Grid();
            gridLateralDerecho.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            gridLateralDerecho.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            gridLateralDerecho.Margin = new Thickness(15, 0, 0, 0);

            GroupBox grpArchivos = new GroupBox
            {
                Header = "📁 Documentos en repositorio",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            lstArchivosDisponibles = new ListBox { Background = Brushes.White, BorderThickness = new Thickness(0) };
            grpArchivos.Content = lstArchivosDisponibles;
            Grid.SetRow(grpArchivos, 0);
            gridLateralDerecho.Children.Add(grpArchivos);
            GroupBox grpFuentes = new GroupBox
            {
                Header = "📄 Fuentes utilizadas",
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563"))
            };
            lstFuentes = new ListBox { Background = Brushes.White, BorderThickness = new Thickness(0) };
            grpFuentes.Content = lstFuentes;
            Grid.SetRow(grpFuentes, 1);
            gridLateralDerecho.Children.Add(grpFuentes);

            Grid.SetColumn(gridLateralDerecho, 1);
            gridCentral.Children.Add(gridLateralDerecho);

            Grid.SetRow(gridCentral, 1);
            gridPrincipal.Children.Add(gridCentral);

            // ==========================================================
            // 🕹️ BARRA INFERIOR DE COMANDOS AJUSTADA (4 COLUMNAS)
            // ==========================================================
            Grid gridInferior = new Grid();
            gridInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
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

            btnLimpiar = new Button
            {
                Content = "Limpiar Chat",
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")), 
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0)
            };
            btnLimpiar.Click += btnLimpiar_Click;
            Grid.SetColumn(btnLimpiar, 3);
            gridInferior.Children.Add(btnLimpiar);

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
            string urlTest = "http://localhost:8000/";
            bool conectado = false;
            string jsonRespuesta = "";
            
            while (!conectado)
            {
                try
                {
                    HttpResponseMessage testResponse = await client.GetAsync(urlTest);
                    if (testResponse.IsSuccessStatusCode)
                    {
                        jsonRespuesta = await testResponse.Content.ReadAsStringAsync();
                        conectado = true;
                    }
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }

            dynamic datosServidor = JsonConvert.DeserializeObject(jsonRespuesta);

            Dispatcher.Invoke(() => {
                txtPregunta.IsEnabled = true;
                btnEnviar.IsEnabled = true;
                
                lstArchivosDisponibles.Items.Clear();
                foreach (var archivo in datosServidor.lista_archivos)
                {
                    lstArchivosDisponibles.Items.Add($"• {archivo}");
                }

                EstablecerTextoEnVisor("🚀 ¡Listo! El sistema RAG está activo. Escribí tu consulta sobre los PDFs o XMLs abajo.");
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

            txtPregunta.IsEnabled = false;
            btnEnviar.IsEnabled = false;
            btnExportar.IsEnabled = false;
            lstFuentes.Items.Clear();
            EstablecerTextoEnVisor("*⚡ Pensando y analizando documentos... Aguanta un momento.*");

            try
            {
                string urlPreguntar = "http://localhost:8000/preguntar";
                
                var payload = new Dictionary<string, string> { { "pregunta", preguntaUsuario } };
                string jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(urlPreguntar, content);
                
                if (response.IsSuccessStatusCode)
                {
                    string jsonRespuesta = await response.Content.ReadAsStringAsync();
                    var datos = JsonConvert.DeserializeObject<RespuestaIaDto>(jsonRespuesta);

                    EstablecerTextoEnVisor(datos.Respuesta);

                    foreach (var fuente in datos.Fuentes)
                    {
                        lstFuentes.Items.Add(fuente);
                    }

                    btnExportar.IsEnabled = datos.Respuesta != null && datos.Respuesta.Contains("|");
                }
                else
                {
                    EstablecerTextoEnVisor("❌ Ocurrió un error interno (500) en el motor de Python. Verificá la consola del servidor.");
                }
            }
            catch (Exception ex)
            {
                EstablecerTextoEnVisor($"❌ Falló la comunicación HTTP con el backend: {ex.Message}");
            }
            finally
            {
                txtPregunta.IsEnabled = true;
                btnEnviar.IsEnabled = true;
                txtPregunta.Text = "";
                txtPregunta.Focus();
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
                    MessageBox.Show("⚠️ No se encontraron tablas estructuradas válidas en la última respuesta para procesar.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al solicitar la exportación: {ex.Message}", "Fallo de Sistema", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnExportar.IsEnabled = true;
            }
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            txtPregunta.Clear();
            lstFuentes.Items.Clear();
            btnExportar.IsEnabled = false;
            EstablecerTextoEnVisor("🚀 ¡Listo! El sistema RAG está activo. Escribí tu consulta sobre los PDFs o XMLs abajo.");
            txtPregunta.Focus();
        }
    }

       // ==========================================================
    // 📦 ESTRUCTURAS DE TRANSFERENCIA DE DATOS (DTOs) SIN ADVERTENCIAS
    // ==========================================================
    public class RespuestaIaDto
    {
        [JsonProperty("respuesta")]
        public string? Respuesta { get; set; } // El ? elimina las advertencias de compilación

        [JsonProperty("fuentes")]
        public List<string> Fuentes { get; set; } = new List<string>();
    }
}