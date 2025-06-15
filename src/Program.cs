using System;
using System.Windows.Forms;
using System.Security.Principal;
using System.Diagnostics;

namespace Cleaner
{
    static class Program
    {
        /// <summary>
        /// Ponto de entrada principal para o aplicativo.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!IsAdministrator())
            {
                // Reinicia o aplicativo com privilégios de administrador
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.FileName = Application.ExecutablePath;
                proc.Verb = "runas"; // Executa como administrador

                try
                {
                    Process.Start(proc);
                }
                catch
                {
                    // O usuário cancelou a solicitação de elevação
                    MessageBox.Show("Este aplicativo requer permissões de administrador para ser executado.", "Permissão Necessária", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                Application.Exit(); // Sai da instância atual sem permissão
            }
            else
            {
                // Se já for administrador, executa o aplicativo normalmente
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Login_Form());
            }
        }

        // Método para verificar se o usuário atual é administrador
        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
