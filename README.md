# Phoenix Project - Sistema de Trading Automatizado

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Python](https://img.shields.io/badge/python-3.8+-blue.svg)](https://www.python.org/downloads/)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple.svg)](https://dotnet.microsoft.com/download)
[![gRPC](https://img.shields.io/badge/gRPC-latest-green.svg)](https://grpc.io/)
[![MetaTrader](https://img.shields.io/badge/MetaTrader-5-orange.svg)](https://www.metatrader5.com/)

## 📑 Índice

- [📋 Visão Geral](#-visão-geral)
- [🏗️ Arquitetura do Sistema](#️-arquitetura-do-sistema)
- [🚀 Funcionalidades Principais](#-funcionalidades-principais)
- [📁 Estrutura do Projeto](#-estrutura-do-projeto)
- [🛠️ Tecnologias Utilizadas](#️-tecnologias-utilizadas)
- [⚙️ Configuração e Instalação](#️-configuração-e-instalação)
- [📊 Configurações Principais](#-configurações-principais)
- [🔌 API e Scripts](#-api-e-scripts)
- [🏭 Arquitetura e Estratégias](#-arquitetura-e-estratégias)
- [🔍 Monitoramento e Performance](#-monitoramento-e-performance)
- [❗ Troubleshooting](#-troubleshooting)
- [🚀 Próximos Passos](#-próximos-passos)
- [🔒 Segurança e Compliance](#-segurança-e-compliance)
- [📝 Licença de Uso](#-licença-de-uso)
- [👥 Contribuição](#-contribuição)
- [📞 Suporte e Comunidade](#-suporte-e-comunidade)

## 📋 Visão Geral

O Phoenix Project é um sistema avançado de trading automatizado que integra múltiplas tecnologias para análise de mercado financeiro e execução de estratégias de trading. O projeto combina um servidor gRPC em Python conectado ao MetaTrader 5 com aplicações .NET Core para análise de dados e backtesting.

## 🏗️ Arquitetura do Sistema

O projeto está organizado em duas partes principais:

### 1. **gRPC Server** (Python)
- **Localização**: `grpc_server/`
- **Função**: Interface com MetaTrader 5 via gRPC
- **Tecnologias**: Python, gRPC, MetaTrader5, NumPy, Pandas
- **Serviços**:
  - **MarketData**: Streaming de dados de mercado, ticks, rates
  - **OrderManagementSystem**: Gestão de posições, ordens e histórico
- **Funcionalidades**:
  - Streaming de dados em tempo real
  - Compressão de dados com NumPy
  - Gestão de múltiplas conexões simultâneas
  - Integração direta com MT5 API

### 2. **Market Analyzer** (C#/.NET)
- **Localização**: `market_analyzer/`
- **Função**: Análise de dados de mercado e backtesting
- **Tecnologias**: .NET 8, gRPC Client, Docker, Redis
- **Módulos**:
  - **ConsoleApp**: Aplicação principal de trading em tempo real
  - **BacktestRange**: Backtesting especializado em Range Charts
  - **BacktestTimeframe**: Backtesting tradicional por tempo
  - **Application**: Lógica de negócio e estratégias
  - **Infrastructure**: Comunicação gRPC e infraestrutura

## 🚀 Funcionalidades Principais

### **Coleta de Dados de Mercado**
- Conexão direta com MetaTrader 5
- Streaming de dados de ticks em tempo real
- Histórico de preços e volumes
- Suporte a múltiplos símbolos financeiros

### **Análise Técnica**
- Indicadores técnicos avançados (ATR, SMA, etc.)
- Gráficos Range Charts
- Análise de padrões de preço
- Sinais de compra e venda automatizados

### **Backtesting**
- Teste de estratégias em dados históricos
- Análise de performance e lucratividade
- Relatórios detalhados em Excel
- Simulação de slippage e custos de transação

### **Trading Automatizado**
- Gestão automática de ordens
- Controle de posições
- Gestão de risco
- Monitoramento em tempo real

## 📁 Estrutura do Projeto

```
phoenix-project/
├── grpc_server/                          # Servidor Python/gRPC
│   ├── main.py                           # Servidor principal
│   ├── multiserver.py                    # Gerenciador de múltiplos servidores
│   ├── backtest.py                       # Script de backtesting
│   ├── requirements.txt                  # Dependências Python
│   ├── protos/                           # Definições Protocol Buffers
│   │   ├── MarketData.proto              # Serviços de dados de mercado
│   │   ├── OrderManagementSystem.proto   # Gestão de ordens
│   │   └── Contracts.proto               # Contratos base
│   ├── terminal/                         # Módulos de integração MT5
│   │   ├── MarketData.py                 # Implementação serviços de dados
│   │   ├── OrderManagementSystem.py      # Implementação gestão ordens
│   │   └── Extensions/                   # Extensões e utilitários
│   └── notebooks/                        # Jupyter notebooks para análise
│
└── market_analyzer/                      # Aplicações .NET
    ├── ConsoleApp/                       # Aplicação principal de trading
    ├── BacktestRange/                    # Backtesting com Range Charts
    ├── BacktestTimeframe/                # Backtesting por timeframe
    ├── Application/                      # Lógica de negócio
    │   ├── Models/                       # Modelos de dados
    │   ├── Services/                     # Serviços de aplicação
    │   └── Helpers/                      # Utilitários e extensões
    ├── Infrastructure/                   # Infraestrutura e integrações
    └── docker-compose.yml                # Configuração Docker
```

## 🛠️ Tecnologias Utilizadas

### **Backend (Python)**
- **MetaTrader5**: Integração com terminal de trading
- **gRPC**: Comunicação de alta performance
- **NumPy/Pandas**: Processamento de dados numéricos
- **Backtrader**: Framework de backtesting
- **Plotly**: Visualização de dados
- **PyTZ**: Gerenciamento de fuso horário
- **Protocol Buffers**: Serialização eficiente

### **Frontend/Análise (C#/.NET)**
- **.NET 8**: Framework principal
- **gRPC Client**: Comunicação com servidor Python
- **Serilog**: Sistema de logging estruturado
- **Dapper**: ORM para banco de dados
- **Skender.Stock.Indicators**: Indicadores técnicos avançados
- **OoplesFinance.StockIndicators**: Análise financeira adicional
- **MiniExcel**: Geração de relatórios Excel
- **NumSharp**: Processamento numérico em .NET
- **Spectre.Console**: Interface de linha de comando avançada

### **Infraestrutura**
- **Docker**: Containerização e orquestração
- **Redis**: Cache, sessões e dados temporários
- **Protocol Buffers**: Serialização eficiente
- **Object Pool**: Gerenciamento eficiente de conexões gRPC

## ⚙️ Configuração e Instalação

### **Pré-requisitos**
- Python 3.8+
- .NET 8 SDK
- MetaTrader 5 instalado
- Docker (opcional)
- Redis (para cache)

### **Instalação Rápida**

**gRPC Server (Python):**
```bash
cd grpc_server
python -m venv venv && source venv/Scripts/activate
pip install -r requirements.txt
./codegen.bat
python main.py 5051
```

**Market Analyzer (.NET):**
```bash
cd market_analyzer
dotnet restore && dotnet build
dotnet run --project ConsoleApp                    # Trading em tempo real
dotnet run --project BacktestRange                 # Backtesting Range Charts
dotnet run --project BacktestTimeframe             # Backtesting tradicional
```

### **Docker (Opcional)**
```bash
cd market_analyzer
docker-compose up -d        # Iniciar serviços
docker-compose logs -f      # Ver logs
docker-compose down         # Parar serviços
```

### **Dependências Essenciais**
- **MetaTrader 5**: [Download oficial](https://www.metatrader5.com/) + configurar conta
- **Redis**: `choco install redis-64` (Windows) ou usar Docker

## 📊 Configurações Principais

### **Configuração do Trading (appsettings.json)**

```json
{
  "GrpcServer": {
    "Hosts": ["http://localhost:5051+19"]
  },
  "Operation": {
    "Symbol": "WINQ24",           // Símbolo a ser negociado
    "BrickSize": 30,              // Tamanho do brick para Range Chart
    "TimeZoneId": "America/Sao_Paulo",
    "Order": {
      "Magic": 467276,            // Número mágico para identificação
      "Lot": 1,                   // Tamanho da posição
      "Deviation": 0,             // Desvio máximo
      "ProductionMode": "Off"     // Modo de produção
    }
  }
}
```

### **Parâmetros de Backtesting**

- **Período de análise**: Configurável por datas (UTC)
- **Slippage**: Simulação de custos de transação e escorregamento
- **Indicadores**: ATR, SMA, Range Charts, Volume Analysis
- **Símbolos suportados**: WIN (Mini Índice), WDO (Mini Dólar), stocks, forex
- **Timeframes**: 1s, 5s, 10s, 1m, 5m, 15m, 1h, 1D
- **Métricas**: Sharpe Ratio, Sortino Ratio, Maximum Drawdown, Win Rate

## 🔌 API e Scripts

### **gRPC Services**
- **MarketData**: Streaming de ticks, rates, dados históricos
- **OrderManagement**: Gestão de posições, ordens e histórico de negociações

### **Scripts Principais**
```bash
python multiserver.py 5051+4 5060+2    # Múltiplos servidores para carga
python backtest.py                      # Backtesting standalone
```

### **Relatórios Automáticos**
- Arquivos Excel com métricas de performance (Sharpe, Sortino, Max Drawdown)
- Histórico detalhado de trades e equity curves

## 🏭 Arquitetura e Estratégias

### **Padrão de Serviços**
O sistema utiliza loops especializados para:
- **Monitoramento**: Posições, ordens, integridade do sistema
- **Processamento**: Dados de mercado em tempo real
- **Execução**: Estratégias de compra/venda automatizadas

### **Estratégias Implementadas**

**Range Chart Strategy**
- Baseada em movimentação de preços por pontos fixos (brick size configurável)
- Ideal para mercados voláteis como WIN e WDO

**Moving Average Strategy**  
- Cruzamento de médias móveis com confirmação ATR
- Período configurável (padrão: 50 períodos)

**ATR Dynamic Strategy**
- Stop loss e take profit dinâmicos baseados na volatilidade
- Relação risco/retorno 1:2 ajustável

## 🔍 Monitoramento e Performance

### **Logging**
- **Serilog** com níveis configuráveis (Debug, Info, Warning, Error)
- Saídas: Console, arquivos rotacionais, Elasticsearch (opcional)
- Métricas: Performance, latência, taxa de erro

### **Otimizações**
- **gRPC**: Object pooling, streaming, compressão NumPy
- **Memory**: Garbage collection otimizada, buffer pooling
- **Benchmarks**: < 5ms latência, > 10k ticks/segundo, < 500MB RAM

## ❗ Troubleshooting

### **Problemas Comuns**

**MetaTrader 5 não conecta:**
```bash
# Verificar se MT5 está rodando e testar Python API
python -c "import MetaTrader5 as mt5; print(mt5.initialize())"
```

**gRPC Connection Refused:**
```bash
# Verificar se servidor está ativo na porta
netstat -an | grep :5051
```

**Protocol Buffers Error:**
```bash
# Regenerar arquivos proto e recompilar
cd grpc_server && ./codegen.bat
cd ../market_analyzer && dotnet clean && dotnet build
```

## 🚀 Próximos Passos

### **Roadmap Principal**
- **Interface Web**: Dashboard em tempo real com SignalR
- **Machine Learning**: Otimização automática de parâmetros
- **Multi-Broker**: Interactive Brokers, Binance
- **Mobile App**: Monitoramento via smartphone
- **Microserviços**: Arquitetura cloud-native com Kubernetes

## 🔒 Segurança e Compliance

### **Medidas de Segurança**
- **Comunicação**: TLS 1.3 criptografado para todas as conexões
- **Autenticação**: JWT tokens e controle de acesso baseado em função
- **Auditoria**: Log completo de operações e audit trail

### **Gestão de Risco**
- **Stop Loss obrigatório** e position sizing baseado em Kelly Criterion
- **Controle de drawdown** com parada automática em perdas excessivas
- **Backup automático** de configurações e estado do sistema

## 📝 Licença de Uso

### **MIT License**
**Copyright © 2024-2025 Phoenix Project**

Este projeto está licenciado sob a **MIT License** - permissiva para uso comercial, distribuição e modificação.

### **⚠️ AVISO IMPORTANTE - RISCOS FINANCEIROS**

**Trading automatizado envolve riscos substanciais:**
- **Alto Risco**: Pode resultar em perda total do capital investido
- **Sem Garantias**: Performance passada não garante resultados futuros
- **Teste Obrigatório**: Sempre teste em ambiente de demonstração primeiro
- **Não é Consultoria**: Este é um software, não consultoria financeira

### **Uso Responsável**
**USE POR SUA PRÓPRIA CONTA E RISCO E APENAS COM CAPITAL QUE PODE PERDER.**

**📄 Licença completa: [LICENSE.md](LICENSE.md)**

## 👥 Contribuição

**Contribuições são bem-vindas!** 

### **Como Contribuir**
```bash
git clone https://github.com/agabopinho/phoenix-project.git
git checkout -b feature/nova-funcionalidade
# Faça suas alterações
git commit -m "Adiciona nova funcionalidade"
git push origin feature/nova-funcionalidade
# Abra um Pull Request
```

### **Tipos de Contribuição**
- 🐛 **Correção de bugs** e melhorias de código
- ✨ **Novas estratégias** e indicadores técnicos  
- 📚 **Documentação** e exemplos práticos
- 🧪 **Testes** unitários e de integração
- ⚡ **Otimizações** de performance

### **Diretrizes**
- Siga as convenções de código do projeto
- Adicione testes para novas funcionalidades
- Documente mudanças significativas
- Use mensagens de commit descritivas

## 📞 Suporte e Comunidade

### **Obtendo Ajuda**
- **🐛 Bugs e Features**: [GitHub Issues](https://github.com/agabopinho/phoenix-project/issues)
- **💬 Discussões**: [GitHub Discussions](https://github.com/agabopinho/phoenix-project/discussions)
- **📖 Documentação**: README.md e comentários no código

### **Comunidade**
- ⭐ **Star** o projeto para apoiar o desenvolvimento
- 👀 **Watch** para receber notificações de atualizações
- 🍴 **Fork** para suas próprias modificações
- 🤝 **Contribua** ajudando outros usuários e reportando bugs

---

**⚠️ Aviso**: Este sistema é destinado para fins educacionais e de pesquisa. Trading automatizado envolve riscos significativos. Use com responsabilidade e sempre teste em ambiente de demonstração antes de operar com dinheiro real.
