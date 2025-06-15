using KeyAuth;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Principal;
using System.Diagnostics;
using Guna.UI2.WinForms;
using Cleaner.src;

namespace Cleaner
{
    public partial class Login_Form : Form
    {
        private readonly api KeyAuthApp;
        private bool isInitialized = false;

        public Login_Form()
        {
            InitializeComponent();
            KeyAuthApp = new api(
                name: "Cleaner C#",
                ownerid: "x65xDpT4Jf",
                secret: "9891be11ce896d8b0219cefab6d5cd9d178f4060a9aa248445e736ae18d452f5",
                version: "1.0"
            );

            this.Load += Login_Load;
        }

        private async void Login_Load(object sender, EventArgs e)
        {
            try
            {
                await InitializeKeyAuth();
            }
            catch (Exception ex)
            {
                ShowError("Erro ao inicializar KeyAuth", ex);
            }
        }

        private async Task InitializeKeyAuth()
        {
            Login_Buton.Enabled = false;
            Error01_Label.Text = "Inicializando KeyAuth...";

            try 
            {
                await Task.Run(() => KeyAuthApp.init());

                if (!KeyAuthApp.response.success)
                {
                    Error01_Label.Text = $"Erro ao inicializar KeyAuth: {TranslateKeyAuthError(KeyAuthApp.response.message)}";
                    Login_Buton.Enabled = true;
                    return;
                }

                if (await KeyAuthApp.checkblack())
                {
                    Error01_Label.Text = "Acesso bloqueado.";
                    Login_Buton.Enabled = true;
                    return;
                }

                if (!string.IsNullOrEmpty(KeyAuthApp.app_data.downloadLink))
                {
                    Error01_Label.Text = "Nova versão disponível!";
                    MessageBox.Show(
                        "Uma nova versão está disponível. Por favor, atualize o aplicativo.",
                        "Atualização Disponível",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    Error01_Label.Text = "";
                }

                isInitialized = true;
                Login_Buton.Enabled = true;
            }
            catch (Exception ex)
            {
                Error01_Label.Text = $"Erro durante inicialização: {ex.Message}";
                Login_Buton.Enabled = true;
            }
        }

        private async void Guna2Button1_Click(object sender, EventArgs e)
        {
            if (!isInitialized)
            {
                ShowError("Sistema não inicializado", new Exception("O sistema ainda não foi inicializado corretamente."));
                return;
            }

            try
            {
                await PerformLogin();
            }
            catch (Exception ex)
            {
                ShowError("Erro ao realizar login", ex);
            }
        }

        private async Task PerformLogin()
        {
            Login_Buton.Enabled = false;
            Error01_Label.Text = "Autenticando com licença...";

            var licenseKey = UserLogin_Text.Text?.Trim();
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                Error01_Label.Text = "Por favor, insira sua chave de licença.";
                Login_Buton.Enabled = true;
                return;
            }

            try
            {
                await Task.Run(() => KeyAuthApp.license(licenseKey, ""));

                if (KeyAuthApp.response.success)
                {
                    HandleSuccessfulLogin();
                }
                else
                {
                    HandleFailedLogin();
                }
            }
            catch (Exception ex)
            {
                Error01_Label.Text = $"Erro durante autenticação: {ex.Message}";
                Login_Buton.Enabled = true;
            }
        }

        private void HandleSuccessfulLogin()
        {
            Error01_Label.Text = "Login realizado com sucesso!";

            Clean_Form cleanForm = new Clean_Form();
            cleanForm.Show();

            this.Hide();
        }

        private void HandleFailedLogin()
        {
            var errorMessage = KeyAuthApp.response.message ?? "Erro desconhecido";
            Error01_Label.Text = TranslateKeyAuthError(errorMessage);
            Login_Buton.Enabled = true;
        }

        private void ShowError(string message, Exception ex)
        {
            Error01_Label.Text = $"{message}: {TranslateKeyAuthError(ex.Message)}";
            Login_Buton.Enabled = true;
        }

        private string TranslateKeyAuthError(string errorMessage)
        {
            switch (errorMessage?.ToLower())
            {
                case "key not found":
                case "invalid license key":
                    return "Chave de licença inválida ou não encontrada.";
                case "invalid key":
                    return "Chave inválida.";
                case "hwid mismatch":
                    return "HWID incompatível. Contate o suporte para resetar.";
                case "key expired":
                    return "Chave expirada.";
                case "subscription expired":
                    return "Assinatura expirada.";
                case "client is banned":
                    return "Cliente banido.";
                case "client is not whitelisted":
                    return "Cliente não está na lista de permissões.";
                case "client is blacklisted":
                    return "Cliente está na lista negra.";
                case "invalidver":
                    return "Versão do aplicativo inválida. Por favor, atualize.";
                default:
                    return errorMessage ?? "Ocorreu um erro desconhecido.";
            }
        }
    }
}