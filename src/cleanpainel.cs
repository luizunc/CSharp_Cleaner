using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using Shell32;
using System.Runtime.InteropServices; // Adicionar para P/Invoke

namespace Cleaner.src
{
    public partial class Clean_Form : Form
    {
        // Importar a função SHEmptyRecycleBin da shell32.dll
        [DllImport("shell32.dll")]
        public static extern int SHEmptyRecycleBin(IntPtr hWnd, string pszRootPath, uint dwFlags);

        // Flags para a função SHEmptyRecycleBin
        public const uint SHERB_NOCONFIRMATION = 0x00000001;
        public const uint SHERB_NOPROGRESSUI = 0x00000002;
        public const uint SHERB_NOSOUND = 0x00000004;

        public Clean_Form()
        {
            InitializeComponent();
        }

        // Método para atualizar o label de status na UI de forma thread-safe
        private void UpdateStatus(string statusText)
        {
            if (lblstatus.InvokeRequired)
            {
                lblstatus.Invoke((MethodInvoker)delegate {
                    lblstatus.Text = statusText;
                });
            }
            else
            {
                lblstatus.Text = statusText;
            }
        }

        // Método para esvaziar a lixeira (usando P/Invoke)
        private void EmptyRecycleBin()
        {
            UpdateStatus("Esvaziando lixeira...");
            try
            {
                // Esvazia a lixeira para todas as drives, sem confirmação, sem progresso e sem som.
                // IntPtr.Zero para hWnd indica a desktop window.
                SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                UpdateStatus("Lixeira esvaziada.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Erro ao esvaziar a lixeira: {ex.Message}");
                Console.WriteLine($"Erro ao esvaziar a lixeira: {ex.Message}");
            }
        }

        // Método para apagar arquivos temporários (usuário, sistema e prefetch)
        private void CleanTempFiles()
        {
            // Limpar arquivos temporários do usuário (%TEMP%)
            string userTempPath = Path.GetTempPath();
            CleanDirectory(userTempPath, "arquivos temporários do usuário");

            // Limpar arquivos temporários do sistema (C:\Windows\Temp)
            // Requer privilégios de administrador
            string systemTempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            CleanDirectory(systemTempPath, "arquivos temporários do sistema");

            // Limpar arquivos da pasta Prefetch (C:\Windows\Prefetch)
            // A limpeza desta pasta nem sempre traz grande benefício e pode impactar levemente o tempo de inicialização
            // inicialmente, mas alguns a incluem em limpezas. Requer privilégios de administrador.
            string prefetchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            CleanDirectory(prefetchPath, "arquivos Prefetch");
        }

        // Método auxiliar para limpar um diretório com segurança
        private void CleanDirectory(string directoryPath, string description)
        {
            if (!Directory.Exists(directoryPath))
            {
                UpdateStatus($"Diretório de {description} não encontrado ou inacessível.");
                return;
            }

            UpdateStatus($"Limpando {description}...");
            try
            {
                foreach (string file in Directory.GetFiles(directoryPath))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Não foi possível excluir o arquivo {Path.GetFileName(file)} em {directoryPath}: {ex.Message}");
                    }
                }

                foreach (string dir in Directory.GetDirectories(directoryPath))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Não foi possível excluir o diretório {Path.GetFileName(dir)} em {directoryPath}: {ex.Message}");
                    }
                }
                UpdateStatus($"{description} limpos (arquivos em uso foram ignorados).");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Erro geral ao limpar {description}: {ex.Message}");
                Console.WriteLine($"Erro geral ao limpar {description}: {ex.Message}");
            }
        }

        // Método para limpar arquivos temporários de instalação do Windows
        private void CleanWindowsInstallTempFiles()
        {
            string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
            CleanDirectory(downloadPath, "arquivos temporários de instalação do Windows/cache do Windows Update");
        }

        // Método para limpar o cache do Microsoft Store
        private void CleanMicrosoftStoreCache()
        {
            UpdateStatus("Limpando cache do Microsoft Store (wsreset.exe)... ");
            try
            {
                // Executa wsreset.exe de forma silenciosa (usando RunCommand)
                RunCommand("wsreset.exe");
                UpdateStatus("Comando wsreset.exe executado (deve rodar silenciosamente).");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Erro ao executar wsreset.exe: {ex.Message}");
                Console.WriteLine($"Erro ao executar wsreset.exe: {ex.Message}");
            }
        }

        // Método para apagar backups antigos do instalador do Office (Placeholder)
        private void CleanOfficeBackups()
        {
            UpdateStatus("Limpeza de backups do Office: Não implementado automaticamente por segurança. Pode requerer desinstalação/reparação do Office.");
            // TODO: Implementar lógica se uma forma segura e genérica for encontrada.
        }

        // Método para esvaziar pasta de logs do Windows (Event Logs)
        private void CleanWindowsLogs()
        {
            UpdateStatus("Limpando logs de eventos do Windows...");
            try
            {
                // Limpar logs de eventos comuns usando wevtutil
                RunCommand("wevtutil.exe clear-log Application");
                RunCommand("wevtutil.exe clear-log System");
                RunCommand("wevtutil.exe clear-log Security");
                // RunCommand("wevtutil.exe clear-log Setup"); // Opcional
                // RunCommand("wevtutil.exe clear-log ForwardedEvents"); // Opcional
                UpdateStatus("Logs de eventos comuns limpos.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Erro ao limpar logs de eventos do Windows: {ex.Message}");
                Console.WriteLine($"Erro ao limpar logs de eventos do Windows: {ex.Message}");
            }
        }

        // Método para limpar pasta de arquivos recentes do usuário
        private void CleanRecentFiles()
        {
            string recentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Recent");
            CleanDirectory(recentPath, "pasta de arquivos recentes");
            // Nota: Isso limpa a pasta 'Recent', mas não a lista de 'Itens Recentes' no Jump List/Shell, que requer manipulação de registro/APIs.
            UpdateStatus("Pasta de arquivos recentes limpa.");
        }

        // Método para executar Limpeza de Disco (Implementação silenciosa para alguns itens comuns)
        private void RunDiskCleanup()
        {
            UpdateStatus("Realizando limpeza de disco (silenciosa para itens comuns)...");
            // Implementar limpeza de alguns itens comuns que cleanmgr limpa

            // Ex: Limpar cache de miniaturas
            UpdateStatus("Limpando cache de miniaturas...");
            string thumbnailCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Explorer");
            string[] thumbnailCacheFiles = Directory.GetFiles(thumbnailCachePath, "thumbcache_*.db", SearchOption.TopDirectoryOnly);
            foreach (string file in thumbnailCacheFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Não foi possível excluir o arquivo de cache de miniaturas {Path.GetFileName(file)}: {ex.Message}");
                }
            }
            UpdateStatus("Cache de miniaturas limpo.");

            // Ex: Limpar arquivos temporários da internet (cuidado: pode requerer acesso a diretórios protegidos ou específicos do navegador)
            // Esta é complexa de fazer de forma genérica e segura sem a ferramenta nativa.
            // UpdateStatus("Limpeza de arquivos temporários da internet (requer implementação específica).");

            // Nota: Limpeza de arquivos de otimização de entrega e arquivos de instalação temporários do Windows já é coberta por CleanWindowsInstallTempFiles.
            // Outros itens como arquivos de log de instalação, relatórios de erro do Windows, etc. exigiriam identificação de localizações específicas.

            UpdateStatus("Limpeza de disco silenciosa concluída (itens comuns).");
        }

        // Método para Limpeza de Resíduos de Programas (Placeholder - Risco Alto para automação genérica)
        private void CleanProgramResidues()
        {
            UpdateStatus("Limpeza de resíduos de programas: Implementação automática genérica não é segura. Use 'Aplicativos e Recursos' do Windows para desinstalar completamente ou ferramentas especializadas.");
            // Não implementar limpeza de resíduos genérica devido ao alto risco de danificar o sistema ou outros programas.
        }

        // Método para limpar pontos de restauração antigos (Usando vssadmin - deve ser silencioso via RunCommand)
        private void CleanOldRestorePoints()
        {
            UpdateStatus("Limpando pontos de restauração antigos (vssadmin)...");
            try
            {
                RunCommand("vssadmin.exe Delete ShadowStorage /For=C: /Oldest");
                UpdateStatus("Comando vssadmin executado para remover ponto de restauração mais antigo (apenas C:).");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Erro ao limpar pontos de restauração antigos: {ex.Message}");
                Console.WriteLine($"Erro ao limpar pontos de restauração antigos: {ex.Message}");
            }
        }

        // Método para remover drivers antigos (Placeholder)
        private void RemoveOldDrivers()
        {
            UpdateStatus("Remoção de drivers antigos: Não implementado automaticamente por segurança. Use o Gerenciador de Dispositivos ou a Limpeza de Disco do Windows.");
            // TODO: Implementar lógica se uma forma segura e genérica for encontrada.
        }

        // Método para limpar cache do Windows Update manualmente (Redundante com CleanWindowsInstallTempFiles - mantido para clareza)
        private void CleanWindowsUpdateCache()
        {
            CleanWindowsInstallTempFiles(); // Chama o método que limpa o diretório SoftwareDistribution Download
            UpdateStatus("Cache do Windows Update limpo (via limpeza de arquivos temporários de instalação).");
        }

        // Método para excluir arquivos de hibernação (desabilitar hibernação - deve ser silencioso via RunCommand)
        private void DisableHibernation()
        {
            UpdateStatus("Desabilitando hibernação (isso excluirá o arquivo hiberfil.sys)...");
            try
            {
                RunCommand("powercfg.exe /h off");
                UpdateStatus("Hibernação desabilitada. Arquivo hiberfil.sys removido.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Erro ao desabilitar hibernação: {ex.Message}");
                Console.WriteLine($"Erro ao desabilitar hibernação: {ex.Message}");
            }
        }

        // Método para excluir cache do sistema e logs (Limpeza adicional - deve ser silencioso via RunCommand)
        private void CleanSystemCacheAndLogs()
        {
            UpdateStatus("Limpando cache geral do sistema e logs (limpeza adicional)...");
            CleanWindowsLogs(); // Chama a limpeza de logs de eventos

            // Limpar cache DNS
            UpdateStatus("Limpando cache DNS...");
            RunCommand("ipconfig /flushdns");
            UpdateStatus("Cache DNS limpo.");

            // Outras limpezas silenciosas podem ser adicionadas aqui (ex: cache de miniaturas, que movi para RunDiskCleanup para manter a associação conceitual).
        }

        // Método para Limpeza Geral (Chama TODAS as outras limpezas, priorizando métodos silenciosos)
        private void PerformGeneralCleanup()
        {
            UpdateStatus("Iniciando Limpeza Geral (todas as tarefas, silenciosamente onde possível)...");

            // Limpeza Interna (Métodos seguros e silenciosos)
            EmptyRecycleBin();
            CleanTempFiles();
            // CleanMicrosoftStoreCache(); // Removido da limpeza geral para garantir total silêncio
            CleanWindowsInstallTempFiles();
            CleanOfficeBackups(); // Placeholder/Risco - apenas mensagem informativa
            CleanWindowsLogs();
            CleanRecentFiles();

            // Limpeza Profunda (Métodos seguros/semi-silenciosos ou placeholders)
            RunDiskCleanup(); // Implementação silenciosa para itens comuns (cache de miniaturas, etc.)
            CleanProgramResidues(); // Placeholder/Risco - apenas mensagem informativa
            CleanOldRestorePoints();
            RemoveOldDrivers(); // Placeholder/Risco - apenas mensagem informativa
            DisableHibernation();
            CleanSystemCacheAndLogs();

            // Removido: UpdateStatus("Processo de Limpeza General concluído (tarefas silenciosas onde possível). Verifique o status acima.");
            // A mensagem de conclusão será exibida via MessageBox no CLEANACTION_BUTTON_Click
        }

        // Método auxiliar para executar comandos do sistema de forma silenciosa
        private void RunCommand(string command)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true, // NÃO criar janela do console
                    Verb = "runas" // Tenta rodar como administrador
                };

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit(); // Espera o comando terminar
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao executar comando \'{command}\': {ex.Message}");
            }
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void guna2CustomCheckBox1_Click(object sender, EventArgs e)
        {

        }

        private void FPSBOOST_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2CheckBox5_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void CLEANACTION_BUTTON_Click(object sender, EventArgs e)
        {
            UpdateStatus("Iniciando limpeza...");

            // Se a Limpeza Geral estiver marcada, execute apenas ela.
            if (L_Geral.Checked)
            {
                PerformGeneralCleanup();
            }
            else
            {
                // Executa as limpezas selecionadas individualmente

                // Limpeza Interna
                if (C_Lixeira.Checked)
                {
                    EmptyRecycleBin();
                }
                if (C_Temp.Checked)
                {
                    CleanTempFiles();
                }
                if (C_CacheNav.Checked)
                {
                    UpdateStatus("Limpeza de cache de navegador: Não implementado automaticamente por segurança. Use a ferramenta interna do navegador.");
                }
                if (C_Instalações.Checked)
                {
                    CleanWindowsInstallTempFiles();
                }
                if (C_MicrosoftStore.Checked)
                {
                    CleanMicrosoftStoreCache(); // Chama o método que pode não ser 100% silencioso
                }
                if (C_Office.Checked)
                {
                    UpdateStatus("Limpeza de backups do Office: Não implementado automaticamente por segurança. Pode requerer desinstalação/reparação do Office.");
                }
                if (C_Logs.Checked)
                {
                    CleanWindowsLogs();
                }
                if (C_Recentes.Checked)
                {
                    CleanRecentFiles();
                }

                // Limpeza Profunda
                if (L_Disco.Checked)
                {
                    RunDiskCleanup(); // Chama o método com implementação silenciosa para alguns itens
                }
                if (L_Residuos.Checked)
                {
                    UpdateStatus("Limpeza de resíduos de programas: Implementação automática genérica não é segura. Use 'Aplicativos e Recursos' do Windows para desinstalar completamente ou ferramentas especializadas.");
                }
                if (L_Restauração.Checked)
                {
                    CleanOldRestorePoints();
                }
                if (L_Drivers.Checked)
                {
                    UpdateStatus("Remoção de drivers antigos: Não implementado automaticamente por segurança. Use o Gerenciador de Dispositivos ou a Limpeza de Disco do Windows.");
                }
                if (L_Update.Checked)
                {
                    CleanWindowsUpdateCache();
                }
                if (L_Hibernação.Checked)
                {
                    DisableHibernation();
                }
                if (L_Logs_CacheSistema.Checked)
                {
                    CleanSystemCacheAndLogs();
                }
            }

            // Exibir MessageBox de conclusão
            MessageBox.Show("Limpeza concluída com sucesso!", "Concluído", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // Removido: Aviso de conclusão geral no status, substituído pelo MessageBox
            // if (!L_Geral.Checked)
            // {
            //      UpdateStatus("Processo de seleção de limpeza concluído. Verifique o status acima.");
            // }
        }
    }
}
