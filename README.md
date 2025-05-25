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
- [🔌 API e Protocolos](#-api-e-protocolos)
- [🔧 Scripts Utilitários](#-scripts-utilitários)
- [🏭 Arquitetura de Serviços](#-arquitetura-de-serviços)
- [📈 Estratégias Implementadas](#-estratégias-implementadas)
- [🔍 Monitoramento e Logging](#-monitoramento-e-logging)
- [🚀 Performance e Otimizações](#-performance-e-otimizações)
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
├── grpc_server/                    # Servidor Python/gRPC
│   ├── main.py                    # Servidor principal
│   ├── multiserver.py            # Gerenciador de múltiplos servidores
│   ├── backtest.py              # Script de backtesting
│   ├── requirements.txt         # Dependências Python
│   ├── protos/                  # Definições Protocol Buffers
│   │   ├── MarketData.proto     # Serviços de dados de mercado
│   │   ├── OrderManagementSystem.proto # Gestão de ordens
│   │   └── Contracts.proto      # Contratos base
│   ├── terminal/                # Módulos de integração MT5
│   │   ├── MarketData.py       # Implementação serviços de dados
│   │   ├── OrderManagementSystem.py # Implementação gestão ordens
│   │   └── Extensions/          # Extensões e utilitários
│   └── notebooks/              # Jupyter notebooks para análise
│
└── market_analyzer/               # Aplicações .NET
    ├── ConsoleApp/               # Aplicação principal de trading
    ├── BacktestRange/           # Backtesting com Range Charts
    ├── BacktestTimeframe/       # Backtesting por timeframe
    ├── Application/             # Lógica de negócio
    │   ├── Models/             # Modelos de dados
    │   ├── Services/           # Serviços de aplicação
    │   └── Helpers/            # Utilitários e extensões
    ├── Infrastructure/          # Infraestrutura e integrações
    └── docker-compose.yml      # Configuração Docker
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

### **Instalação do gRPC Server**

1. Navegue até o diretório do servidor:
```bash
cd grpc_server
```

2. Crie um ambiente virtual Python (recomendado):
```bash
python -m venv venv
source venv/Scripts/activate  # No Windows Git Bash
```

3. Instale as dependências:
```bash
pip install -r requirements.txt
```

4. Gere os arquivos gRPC (execute o batch file):
```bash
./codegen.bat
```

5. Execute o servidor principal:
```bash
python main.py 5051
```

6. Para múltiplos servidores (opcional):
```bash
python multiserver.py 5051+4 5060+2
```

### **Instalação do Market Analyzer**

1. Navegue até o diretório:
```bash
cd market_analyzer
```

2. Restaure os pacotes NuGet:
```bash
dotnet restore
```

3. Compile o projeto:
```bash
dotnet build
```

4. Execute a aplicação principal de trading:
```bash
dotnet run --project ConsoleApp
```

5. Para backtesting com Range Charts:
```bash
dotnet run --project BacktestRange
```

6. Para backtesting por timeframe:
```bash
dotnet run --project BacktestTimeframe
```

### **Configuração via Docker**

1. Configurar ambiente Docker:
```bash
cd market_analyzer
```

2. Construir e executar os serviços:
```bash
docker-compose up -d
```

3. Ver logs dos containers:
```bash
docker-compose logs -f
```

4. Parar os serviços:
```bash
docker-compose down
```

### **Dependências do Sistema**

#### **MetaTrader 5**
- Baixar e instalar o MetaTrader 5
- Configurar conta demo ou real
- Habilitar algoritmic trading
- Verificar se Python API está funcionando:
```python
import MetaTrader5 as mt5
print(mt5.version())
```

#### **Redis (Opcional)**
Para desenvolvimento local sem Docker:
```bash
# Windows (usando Chocolatey)
choco install redis-64

# Ou baixar do site oficial
# https://redis.io/download
```

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

## 🔌 API e Protocolos

### **gRPC Services**

#### **MarketData Service**
```protobuf
service MarketData {
  rpc StreamTicksRange(StreamTicksRangeRequest) returns (stream TicksRangeReply);
  rpc StreamRatesRange(StreamRatesRangeRequest) returns (stream RatesRangeReply);
  rpc GetTicksRangeBytes(GetTicksRangeBytesRequest) returns (TicksRangeBytesReply);
  rpc StreamRatesRangeFromTicks(StreamRatesRangeFromTicksRequest) returns (stream RatesRangeReply);
}
```

#### **OrderManagementSystem Service**
```protobuf
service OrderManagementSystem {
  rpc GetPositions(GetPositionsRequest) returns (GetPositionsReply);
  rpc GetOrders(GetOrdersRequest) returns (GetOrdersReply);
  rpc GetHistoryOrders(GetHistoryOrdersRequest) returns (GetHistoryOrdersReply);
  rpc GetHistoryDeals(GetHistoryDealsRequest) returns (GetHistoryDealsReply);
  rpc CheckOrder(OrderRequest) returns (CheckOrderReply);
  rpc SendOrder(OrderRequest) returns (SendOrderReply);
}
```

### **Tipos de Dados Suportados**
- **Ticks**: Bid, Ask, Last, Volume, Time (microsegundos)
- **Rates**: OHLCV com timestamp
- **Orders**: Todas as propriedades MT5 (magic, deviation, etc.)
- **Positions**: Volume, profit, swap, commission
- **Deals**: Histórico completo de negociações

## 🔧 Scripts Utilitários

### **Servidor Múltiplo**
```bash
python multiserver.py 5051+4 5060+2
```
Inicia múltiplos servidores gRPC em portas diferentes para distribuição de carga.

### **Backtesting Standalone**
```bash
python backtest.py
```
Executa backtesting com estratégia básica usando Backtrader.

### **Notebooks Jupyter**
- `import_data.ipynb`: Importação e preprocessamento de dados
- `load_data.ipynb`: Carregamento e análise exploratória

### **Geração de Relatórios**
Os aplicativos de backtesting geram automaticamente arquivos Excel com:
- Histórico detalhado de trades
- Métricas de performance (Sharpe, Sortino, Max Drawdown)
- Análise de lucros/perdas por período
- Gráficos de equity curve
- Estatísticas de win rate e profit factor

## 🏭 Arquitetura de Serviços

### **Padrão de Loops de Serviço**
O sistema utiliza múltiplos loops especializados:

- **PositionLoopService**: Monitora posições abertas
- **OrdersLoopService**: Gerencia ordens pendentes
- **LastTickLoopService**: Processa últimos ticks em tempo real
- **MarketDataLoopService**: Coleta e processa dados de mercado
- **SanityTestLoopService**: Testes de integridade do sistema

### **Estratégia de Negociação**
- **OpenBuyLimitLoopService**: Gerencia ordens de compra limitadas
- **OpenSellLimitLoopService**: Gerencia ordens de venda limitadas
- **PositionBuyLoopService**: Controla posições compradas
- **PositionSellLoopService**: Controla posições vendidas

### **Gestão de Estado**
- **State Management**: Controle centralizado do estado da aplicação
- **OrderWrapper**: Wrapper para operações de ordem
- **Error Handling**: Sistema robusto de tratamento de erros

## 📈 Estratégias Implementadas

### **Range Chart Strategy**
- **Princípio**: Baseada em movimentação de preços por pontos fixos
- **Vantagem**: Ignora tempo, foca apenas na volatilidade
- **Aplicação**: Ideal para mercados com alta volatilidade (WIN, WDO)
- **Configuração**: Brick size configurável (padrão: 30-50 pontos)
- **Sinais**: Breakout de bricks para entrada/saída

### **Moving Average Strategy**
- **Princípio**: Cruzamento de médias móveis simples
- **Configuração**: Período configurável (padrão: 50 períodos)
- **Sinais**: 
  - Compra: Preço cruza média para cima
  - Venda: Preço cruza média para baixo
- **Filtros**: ATR para confirmação de tendência

### **ATR Dynamic Strategy**
- **Princípio**: Utiliza Average True Range para gestão dinâmica
- **Stop Loss**: Baseado em múltiplos do ATR
- **Take Profit**: Relação risco/retorno 1:2
- **Adaptação**: Ajusta-se automaticamente à volatilidade
- **Timeframes**: Suporte a múltiplos períodos

### **Multi-Timeframe Analysis**
- **Análise**: Confirmação em múltiplos timeframes
- **Hierarquia**: Tendência primária vs. secundária
- **Entrada**: Alinhamento de sinais entre timeframes
- **Gestão**: Posições baseadas no timeframe maior

## 🔍 Monitoramento e Logging

O sistema inclui logging abrangente com:
- **Serilog**: Logging estruturado com múltiplos sinks
- **Níveis de log**: Debug, Info, Warning, Error, Fatal
- **Saídas**: Console, arquivos rotacionais, Elasticsearch (opcional)
- **Métricas**: Performance, latência, taxa de erro, throughput
- **Contexto**: Enrichment com informações de trading

### **Configuração de Log**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Grpc": "Warning"
      }
    }
  }
}
```

## 🚀 Performance e Otimizações

### **gRPC Optimizations**
- **Object Pooling**: Reutilização de conexões gRPC
- **Streaming**: Processamento de dados em chunks
- **Compressão**: NumPy para serialização eficiente
- **Keep-Alive**: Manutenção de conexões persistentes

### **Memory Management**
- **Garbage Collection**: Otimização para low-latency
- **Buffer Pooling**: Reutilização de buffers
- **Data Compression**: Redução de overhead de rede

### **Benchmarks Típicos**
- **Latência**: < 5ms para operações locais
- **Throughput**: > 10k ticks/segundo
- **Memory**: < 500MB para operação normal
- **CPU**: < 30% em operação contínua

## ❗ Troubleshooting

### **Problemas Comuns**

#### **MetaTrader 5 não conecta**
```bash
# Verificar se MT5 está rodando
tasklist | grep terminal64.exe

# Testar conexão Python
python -c "import MetaTrader5 as mt5; print(mt5.initialize())"
```

#### **gRPC Connection Refused**
```bash
# Verificar se servidor está rodando
netstat -an | grep :5051

# Testar conectividade
telnet localhost 5051
```

#### **Erro de Protocol Buffers**
```bash
# Regenerar arquivos proto
cd grpc_server
./codegen.bat

# Recompilar projeto .NET
cd ../market_analyzer
dotnet clean && dotnet build
```

### **Logs de Debug**
Para habilitar debug completo:
```json
{
  "Serilog": {
    "MinimumLevel": "Debug"
  }
}
```

## 🚀 Próximos Passos

### **Melhorias Planejadas**
- **Interface Web**: Dashboard em tempo real com SignalR
- **Machine Learning**: 
  - Otimização automática de parâmetros
  - Detecção de padrões com TensorFlow.NET
  - Sentiment analysis de notícias
- **Multi-Broker**: Suporte a Interactive Brokers, Binance
- **API REST**: Complementar ao gRPC para integrações web
- **Alertas**: Sistema de notificações via email/SMS/Telegram
- **Mobile App**: Aplicativo para monitoramento

### **Escalabilidade**
- **Microserviços**: Decomposição em serviços especializados
- **Kubernetes**: Orquestração cloud-native
- **Event Sourcing**: Rastreabilidade completa de operações
- **CQRS**: Separação de comando e consulta
- **Multi-Symbol**: Trading simultâneo em múltiplos ativos
- **Distribuição de Carga**: Load balancing entre servidores
- **Processamento Paralelo**: GPU computing para análises
- **Cache Distribuído**: Redis Cluster para alta disponibilidade

### **Integrações Futuras**
- **Dados Fundamentais**: Reuters, Bloomberg APIs
- **Social Trading**: Copy trading e signal providers
- **Risk Management**: Sistemas de controle de risco avançados
- **Compliance**: Audit trail e regulamentações financeiras

## 🔒 Segurança e Compliance

### **Medidas de Segurança**
- **Autenticação**: JWT tokens para APIs
- **Autorização**: Role-based access control
- **Criptografia**: TLS 1.3 para todas as comunicações
- **Audit Trail**: Log completo de todas as operações
- **Rate Limiting**: Proteção contra abuso de APIs

### **Considerações de Trading**
- **Risk Management**: Stop loss obrigatório
- **Position Sizing**: Gestão de capital baseada em Kelly Criterion
- **Drawdown Control**: Parada automática em perdas excessivas
- **Market Hours**: Respeito aos horários de negociação
- **Slippage Control**: Monitoramento de execução

### **Backup e Recovery**
- **Database Backup**: Backup automático diário
- **Configuration**: Versionamento de configurações
- **State Recovery**: Recuperação automática de estado
- **Disaster Recovery**: Plano de continuidade

## 📝 Licença de Uso

### **MIT License**

**Copyright © 2024-2025 Phoenix Project**

Este projeto está licenciado sob a **MIT License** - uma das licenças de código aberto mais permissivas e amplamente utilizadas.

### **Principais Características da MIT License:**

#### **✅ Permissões**
- **Uso Comercial**: Permitido usar o software comercialmente
- **Distribuição**: Permitido distribuir cópias do software
- **Modificação**: Permitido modificar o código fonte
- **Uso Privado**: Permitido uso pessoal e privado
- **Sublicenciamento**: Permitido sublicenciar o software

#### **📋 Condições**
- **Inclusão de Copyright**: Deve incluir o aviso de copyright original
- **Inclusão da Licença**: Deve incluir o texto da licença MIT

#### **🚫 Limitações**
- **Sem Garantias**: O software é fornecido "como está"
- **Sem Responsabilidade**: Os autores não são responsáveis por danos

### **⚠️ AVISO IMPORTANTE - RISCOS FINANCEIROS**

**Embora esta seja uma licença permissiva, é crucial entender os riscos específicos do trading automatizado:**

#### **Riscos de Trading**
- **Alto Risco**: Trading automatizado envolve risco substancial de perda financeira
- **Sem Garantias**: Performance passada não garante resultados futuros  
- **Volatilidade**: Mercados financeiros são imprevisíveis e voláteis
- **Perda Total**: Você pode perder todo seu investimento ou mais

#### **Responsabilidade do Usuário**
- **Teste Primeiro**: Sempre teste estratégias em ambiente de demonstração
- **Entenda os Riscos**: Certifique-se de compreender completamente os riscos
- **Gestão de Risco**: Implemente gestão adequada de risco e sizing
- **Conformidade**: Cumpra todas as regulamentações financeiras aplicáveis

#### **Não é Consultoria Financeira**
- Este software é uma ferramenta, não consultoria financeira
- Os autores não são consultores financeiros licenciados
- Tome suas próprias decisões de investimento
- Considere consultar profissionais qualificados

### **Uso Responsável**
**USE POR SUA PRÓPRIA CONTA E RISCO E APENAS COM CAPITAL QUE VOCÊ PODE SE PERMITIR PERDER.**

### **Acordo de Uso**
Ao usar este software, você concorda com os termos da MIT License e reconhece ter lido e compreendido os avisos de risco financeiro.

**📄 Para o texto completo da licença, consulte o arquivo [LICENSE.md](LICENSE.md)**

## 👥 Contribuição

**Este é um projeto open source!** Contribuições são muito bem-vindas e incentivadas.

### **Como Contribuir**

1. **Fork o repositório**
   ```bash
   git clone https://github.com/agabopinho/phoenix-project.git
   ```

2. **Crie uma branch para sua feature**
   ```bash
   git checkout -b feature/nova-estrategia
   ```

3. **Commit suas mudanças**
   ```bash
   git commit -m "Adiciona nova estratégia de RSI"
   ```

4. **Push para a branch**
   ```bash
   git push origin feature/nova-estrategia
   ```

5. **Abra um Pull Request**
   - Descreva detalhadamente as mudanças
   - Inclua testes se aplicável
   - Atualize a documentação conforme necessário

### **Tipos de Contribuição Bem-vindas**

- **🐛 Correção de Bugs**: Relatórios e correções de problemas
- **✨ Novas Features**: Estratégias, indicadores, funcionalidades
- **📚 Documentação**: Melhorias na documentação e exemplos
- **🧪 Testes**: Adição de testes unitários e de integração
- **🎨 Interface**: Melhorias na usabilidade e interface
- **⚡ Performance**: Otimizações de performance
- **🔧 Configuração**: Melhorias na configuração e deployment

### **Diretrizes de Contribuição**

- **Estilo de Código**: Siga as convenções do projeto
- **Testes**: Adicione testes para novas funcionalidades
- **Documentação**: Documente código e funcionalidades
- **Commits**: Use mensagens de commit claras e descritivas
- **Issues**: Use templates de issue quando disponíveis

### **Código de Conduta**

Este projeto adere a um código de conduta. Ao participar, você concorda em manter um ambiente respeitoso e inclusivo para todos.

## 📞 Suporte e Comunidade

### **Obtendo Ajuda**

#### **📋 Issues no GitHub**
Para bugs, problemas técnicos ou solicitações de features:
- Abra uma [issue no GitHub](https://github.com/agabopinho/phoenix-project/issues)
- Use os templates de issue quando disponíveis
- Forneça informações detalhadas sobre o problema

#### **💬 Discussões**
Para perguntas, discussões e ideias:
- Use as [GitHub Discussions](https://github.com/agabopinho/phoenix-project/discussions)
- Compartilhe estratégias e experiências
- Faça perguntas para a comunidade

#### **📖 Documentação**
- Leia este README.md completamente
- Consulte os comentários no código fonte
- Verifique exemplos na pasta `notebooks/`

### **Comunidade**

#### **🤝 Participar da Comunidade**
- Ajude outros usuários respondendo questões
- Compartilhe suas estratégias e melhorias
- Reporte bugs e sugira melhorias
- Contribua com código e documentação

#### **📢 Mantenha-se Atualizado**
- ⭐ Deixe uma estrela no projeto
- 👀 "Watch" o repositório para receber notificações
- 🍴 Fork o projeto para suas próprias modificações

### **Suporte Comercial**

Para suporte empresarial, consultoria ou desenvolvimento customizado:
- **Consultoria**: Desenvolvimento de estratégias personalizadas
- **Integração**: Ajuda com integrações complexas
- **Treinamento**: Workshops e treinamentos especializados

---

**Nota**: Este sistema é destinado para fins educacionais e de pesquisa. Trading automatizado envolve riscos significativos. Use com responsabilidade e sempre teste em ambiente de demonstração antes de operar com dinheiro real.
