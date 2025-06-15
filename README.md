# C# Cleaner

Uma ferramenta de limpeza e otimização de sistema desenvolvida em C# com interface gráfica moderna usando Guna.UI2.

## Características

- Interface gráfica moderna e intuitiva
- Limpeza do sistema
- Otimização do registro do Windows
- Sistema de autenticação seguro (KeyAuth)

## Tecnologias Utilizadas

- C# WinForms
- Guna.UI2.WinForms (v2.0.4.7)
- Newtonsoft.Json (v13.0.3)
- Portable.BouncyCastle (v1.9.0)
- System.Configuration.ConfigurationManager (v7.0.0)
- Costura.Fody (v6.0.0)

## Estrutura do Projeto

```
├── src/
│   ├── KeyAuth.cs          # Sistema de autenticação
│   ├── Program.cs          # Ponto de entrada do aplicativo
│   ├── cleanpainel.cs      # Painel principal de limpeza
│   └── login.cs            # Interface de login
├── resources/              # Recursos do projeto
│   ├── painel_clean.png
│   ├── painel_regedit.png
│   └── unclogo.ico
└── config/                 # Arquivos de configuração
```

## Requisitos do Sistema

- Windows 7 ou superior
- .NET Framework 4.7.2 ou superior

## Instalação

1. Clone o repositório
2. Abra a solução no Visual Studio
3. Restaure os pacotes NuGet
4. Compile e execute o projeto

## Licença

Todos os direitos reservados.
