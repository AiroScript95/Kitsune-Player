using Project_Kitsune.Models;
using System.IO;
using System.Text.Json;

namespace Project_Kitsune.Services
{
    public class ConfiguracaoService
    {
        public Configuracao CarregarConfiguracao()
        {
            string caminhoData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string caminhoPasta = Path.Combine(caminhoData, "Kitsune");
            string caminhoPastaConfig = Path.Combine(caminhoPasta, "Settings");
            string caminhoArquivo = Path.Combine(caminhoPastaConfig, "settings.json");

            if (!File.Exists(caminhoArquivo))
            {
                Configuracao novaConfig = new Configuracao();
                novaConfig.RotaMusica.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
                return novaConfig;
            }

            try
            {
                string TextJson = File.ReadAllText(caminhoArquivo);
                return JsonSerializer.Deserialize<Configuracao>(TextJson) ?? new Configuracao();
            }
            catch (JsonException)
            {
                // Ficheiro corrompido de uma tentativa anterior — cai para config nova em vez de crashar
                Configuracao novaConfig = new Configuracao();
                novaConfig.RotaMusica.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
                return novaConfig;
            }
        }

        public void GuardarConfiguracao(Configuracao config)
        {
            try
            {
                string TextJson = JsonSerializer.Serialize(config);
                string caminhoData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string caminhoPasta = Path.Combine(caminhoData, "Kitsune");
                string caminhoPastaConfig = Path.Combine(caminhoPasta, "Settings");
                string caminhoArquivo = Path.Combine(caminhoPastaConfig, "settings.json");
                Directory.CreateDirectory(caminhoPastaConfig);

                string caminhoTemp = caminhoArquivo + ".tmp";
                File.WriteAllText(caminhoTemp, TextJson);

                if (File.Exists(caminhoArquivo))
                {
                    File.Replace(caminhoTemp, caminhoArquivo, null);
                }
                else
                {
                    File.Move(caminhoTemp, caminhoArquivo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public void GuardarBackground(string caminho)
        {
            try
            {
                HashSet<string> extensoesValidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".png",".jpg",".jpeg",
                };

                string extensao = Path.GetExtension(caminho);

                if (!extensoesValidas.Contains(extensao)) return;

                string caminhoData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string caminhoPasta = Path.Combine(caminhoData, "Kitsune");
                string caminhoPastaAssets = Path.Combine(caminhoPasta, "Assets");
                Directory.CreateDirectory(caminhoPastaAssets);

                // Apaga qualquer "background.*" antigo, independente da extensão
                foreach (string arquivoAntigo in Directory.EnumerateFiles(caminhoPastaAssets, "background.*"))
                {
                    File.Delete(arquivoAntigo);
                }

                string caminhoDestino = Path.Combine(caminhoPastaAssets, "background" + extensao);
                File.Copy(caminho, caminhoDestino, overwrite: true);

                // Atualiza e guarda a configuração
                Configuracao config = CarregarConfiguracao();
                config.Background = caminhoDestino;
                GuardarConfiguracao(config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERRO: {ex.Message}");
            }
        }

        public void RemoverBackground()
        {
            try
            {
                Configuracao config = CarregarConfiguracao();

                if (!string.IsNullOrWhiteSpace(config.Background) && File.Exists(config.Background))
                {
                    File.Delete(config.Background);
                }

                config.Background = string.Empty;
                GuardarConfiguracao(config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERRO: {ex.Message}");
            }
        }

        public bool AdicionarPastaMusica(string caminho)
        {
            Configuracao config = CarregarConfiguracao();

            if (config.RotaMusica.Count >= 5) return false;

            if (config.RotaMusica.Contains(caminho)) return false;

            if (!Directory.Exists(caminho)) return false;

            config.RotaMusica.Add(caminho);
            GuardarConfiguracao(config);
            return true;
        }

        public void RemoverPastaMusica(string caminho)
        {
            Configuracao config = CarregarConfiguracao();
            config.RotaMusica.Remove(caminho);
            GuardarConfiguracao(config);
        }

        public void AtualizarTema(string nomeTema)
        {
            Configuracao config = CarregarConfiguracao();
            config.Theme = nomeTema;
            GuardarConfiguracao(config);
        }

        public void AtualizarIdiona(string nomeIdiona)
        {
            Configuracao config = CarregarConfiguracao();
            config.Idioma = nomeIdiona;
            GuardarConfiguracao(config);
        }
    }
}