using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlphaDownloadManager
{
    public partial class Form1 : Form
    {
        private HttpClient _httpClient;

        public Form1()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
        }

        private async Task DownloadFileAsync(string fileUrl, string filePath)
        {
            try
            {
                using (var response = await _httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;
                        long totalBytes = response.Content.Headers.ContentLength ?? -1;
                        long downloadedBytes = 0;

                        var stopwatch = Stopwatch.StartNew();

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            double downloadSpeed = downloadedBytes / stopwatch.Elapsed.TotalSeconds / 1024;
                            Invoke(new Action(() =>
                            {
                                lblSpeed.Text = $"Download Speed: {downloadSpeed:F2} KB/s";
                            }));

                            // Atualizar progressBar de forma segura
                        }

                        stopwatch.Stop();

                        // Verificação de integridade do arquivo
                        if (totalBytes != -1 && downloadedBytes != totalBytes)
                        {
                            throw new Exception("O tamanho do arquivo baixado não corresponde ao tamanho esperado.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Re-throw the exception to be caught in the btnDownload_Click method
                throw new Exception($"Erro ao baixar o arquivo de {fileUrl}: {ex.Message}", ex);
            }
        }




        private async void btnDownload_Click(object sender, EventArgs e)
        {   
            btnDownload.Enabled = false;
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string downloadFolderPath = folderDialog.SelectedPath;
                    string txtFileUrl = "https://cdn.cfalphagaming.com/files.txt"; // URL do arquivo .txt com os links

                    try
                    {
                        List<string> fileUrls = await ReadUrlsFromTextFile(txtFileUrl);

                        if (fileUrls.Count == 0)
                        {
                            MessageBox.Show("Nenhum link encontrado no arquivo .txt.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        progressBar.Value = 0;
                        progressBar.Maximum = fileUrls.Count;

                        foreach (var fileUrl in fileUrls)
                        {
                            string fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
                            lblDownloadingFile.Text = $"Downloading File: {fileName}";
                            string filePath = Path.Combine(downloadFolderPath, fileName);

                            try
                            {
                                await DownloadFileAsync(fileUrl, filePath);

                                // Atualizar a barra de progresso após cada arquivo baixado
                                progressBar.Value++;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Erro ao baixar o arquivo: {fileName}\n\n{ex.Message}", "Erro de Download", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                // Continuar o loop para tentar baixar os próximos arquivos
                            }
                        }

                        MessageBox.Show("Download concluído!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnDownload.Enabled = true;
                        lblDownloadingFile.Text = "Downloading File: None";
                        lblSpeed.Text = "Download Speed: 0 KB/s";

                        // Execute the setup file
                        string setupFilePath = Path.Combine(downloadFolderPath, "AlphaGaming_Setup.exe");
                        if (File.Exists(setupFilePath))
                        {
                            try
                            {
                                Process.Start(setupFilePath);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Erro ao executar o arquivo de setup: {ex.Message}", "Erro de Execução", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Arquivo de setup não encontrado.", "Erro de Execução", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao ler o arquivo .txt: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    btnDownload.Enabled = true;
                }
            }
        }

        private async Task<List<string>> ReadUrlsFromTextFile(string txtFileUrl)
        {
            List<string> urls = new List<string>();

            try
            {
                using (var response = await _httpClient.GetAsync(txtFileUrl))
                {
                    response.EnsureSuccessStatusCode();
                    string content = await response.Content.ReadAsStringAsync();
                    string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        urls.Add(line.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao ler o arquivo .txt de {txtFileUrl}: {ex.Message}", ex);
            }

            return urls;
        }





        // Enable window dragging from background
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("User32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("User32.dll")]
        public static extern bool SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
