# Phoenix Project - Automated Trading System

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Python](https://img.shields.io/badge/python-3.8+-blue.svg)](https://www.python.org/downloads/)
[![.NET](https://img.shields.io/badge/.NET-8.0+-purple.svg)](https://dotnet.microsoft.com/download)
[![gRPC](https://img.shields.io/badge/gRPC-latest-green.svg)](https://grpc.io/)
[![MetaTrader](https://img.shields.io/badge/MetaTrader-5-orange.svg)](https://www.metatrader5.com/)

> 🌐 **Language**: [English](README.md) | [Português](README_pt-BR.md)

## 📑 Table of Contents

- [📋 Overview](#-overview)
- [🏗️ System Architecture](#️-system-architecture)
- [🚀 Key Features](#-key-features)
- [📁 Project Structure](#-project-structure)
- [🛠️ Technologies Used](#️-technologies-used)
- [⚙️ Setup and Installation](#️-setup-and-installation)
- [📊 Main Configurations](#-main-configurations)
- [🔌 API and Scripts](#-api-and-scripts)
- [🏭 Architecture and Strategies](#-architecture-and-strategies)
- [🔍 Monitoring and Performance](#-monitoring-and-performance)
- [❗ Troubleshooting](#-troubleshooting)
- [🚀 Roadmap](#-roadmap)
- [🔒 Security and Compliance](#-security-and-compliance)
- [📝 License](#-license)
- [👥 Contributing](#-contributing)
- [📞 Support and Community](#-support-and-community)

## 📋 Overview

The Phoenix Project is an advanced automated trading system that integrates multiple technologies for financial market analysis and trading strategy execution. The project combines a Python gRPC server connected to MetaTrader 5 with .NET Core applications for data analysis and backtesting.

## 🏗️ System Architecture

The project is organized into two main parts:

### 1. **gRPC Server** (Python)
- **Location**: `grpc_server/`
- **Function**: Interface with MetaTrader 5 via gRPC
- **Technologies**: Python, gRPC, MetaTrader5, NumPy, Pandas
- **Services**:
  - **MarketData**: Market data streaming, ticks, rates
  - **OrderManagementSystem**: Position, order and history management
- **Features**:
  - Real-time data streaming
  - NumPy data compression
  - Multiple simultaneous connection management
  - Direct MT5 API integration

### 2. **Market Analyzer** (C#/.NET)
- **Location**: `market_analyzer/`
- **Function**: Market data analysis and backtesting
- **Technologies**: .NET 8, gRPC Client, Docker, Redis
- **Modules**:
  - **ConsoleApp**: Main real-time trading application
  - **BacktestRange**: Range Charts specialized backtesting
  - **BacktestTimeframe**: Traditional time-based backtesting
  - **Application**: Business logic and strategies
  - **Infrastructure**: gRPC communication and infrastructure

## 🚀 Key Features

### **Market Data Collection**
- Direct connection to MetaTrader 5
- Real-time tick data streaming
- Price and volume history
- Multiple financial symbols support

### **Technical Analysis**
- Advanced technical indicators (ATR, SMA, etc.)
- Range Charts
- Price pattern analysis
- Automated buy/sell signals

### **Backtesting**
- Strategy testing on historical data
- Performance and profitability analysis
- Detailed Excel reports
- Slippage and transaction cost simulation

### **Automated Trading**
- Automatic order management
- Position control
- Risk management
- Real-time monitoring

## 📁 Project Structure

```
phoenix-project/
├── grpc_server/                          # Python/gRPC Server
│   ├── main.py                           # Main server
│   ├── multiserver.py                    # Multiple server manager
│   ├── backtest.py                       # Backtesting script
│   ├── requirements.txt                  # Python dependencies
│   ├── protos/                           # Protocol Buffers definitions
│   │   ├── MarketData.proto              # Market data services
│   │   ├── OrderManagementSystem.proto   # Order management
│   │   └── Contracts.proto               # Base contracts
│   ├── terminal/                         # MT5 integration modules
│   │   ├── MarketData.py                 # Data services implementation
│   │   ├── OrderManagementSystem.py      # Order management implementation
│   │   └── Extensions/                   # Extensions and utilities
│   └── notebooks/                        # Jupyter notebooks for analysis
│
└── market_analyzer/                      # .NET Applications
    ├── ConsoleApp/                       # Main trading application
    ├── BacktestRange/                    # Range Charts backtesting
    ├── BacktestTimeframe/                # Traditional backtesting
    ├── Application/                      # Business logic
    │   ├── Models/                       # Data models
    │   ├── Services/                     # Application services
    │   └── Helpers/                      # Utilities and extensions
    ├── Infrastructure/                   # Infrastructure and integrations
    └── docker-compose.yml                # Docker configuration
```

## 🛠️ Technologies Used

### **Backend (Python)**
- **MetaTrader5**: Trading terminal integration
- **gRPC**: High-performance communication
- **NumPy/Pandas**: Numerical data processing
- **Backtrader**: Backtesting framework
- **Plotly**: Data visualization
- **PyTZ**: Timezone management
- **Protocol Buffers**: Efficient serialization

### **Frontend/Analysis (C#/.NET)**
- **.NET 8**: Main framework
- **gRPC Client**: Communication with Python server
- **Serilog**: Structured logging system
- **Dapper**: Database ORM
- **Skender.Stock.Indicators**: Advanced technical indicators
- **OoplesFinance.StockIndicators**: Additional financial analysis
- **MiniExcel**: Excel report generation
- **NumSharp**: Numerical processing in .NET
- **Spectre.Console**: Advanced command line interface

### **Infrastructure**
- **Docker**: Containerization and orchestration
- **Redis**: Cache, sessions and temporary data
- **Protocol Buffers**: Efficient serialization
- **Object Pool**: Efficient gRPC connection management

## ⚙️ Setup and Installation

### **Prerequisites**
- Python 3.8+
- .NET 8 SDK
- MetaTrader 5 installed
- Docker (optional)
- Redis (for caching)

### **Quick Installation**

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
dotnet run --project ConsoleApp                    # Real-time trading
dotnet run --project BacktestRange                 # Range Charts backtesting
dotnet run --project BacktestTimeframe             # Traditional backtesting
```

### **Docker (Optional)**
```bash
cd market_analyzer
docker-compose up -d        # Start services
docker-compose logs -f      # View logs
docker-compose down         # Stop services
```

### **Essential Dependencies**
- **MetaTrader 5**: [Official download](https://www.metatrader5.com/) + configure account
- **Redis**: `choco install redis-64` (Windows) or use Docker

## 📊 Main Configurations

### **Trading Configuration (appsettings.json)**

```json
{
  "GrpcServer": {
    "Hosts": ["http://localhost:5051+19"]
  },
  "Operation": {
    "Symbol": "WINQ24",           // Symbol to be traded
    "BrickSize": 30,              // Brick size for Range Chart
    "TimeZoneId": "America/Sao_Paulo",
    "Order": {
      "Magic": 467276,            // Magic number for identification
      "Lot": 1,                   // Position size
      "Deviation": 0,             // Maximum deviation
      "ProductionMode": "Off"     // Production mode
    }
  }
}
```

### **Backtesting Parameters**

- **Analysis period**: Configurable by dates (UTC)
- **Slippage**: Transaction cost and slippage simulation
- **Indicators**: ATR, SMA, Range Charts, Volume Analysis
- **Supported symbols**: WIN (Mini Index), WDO (Mini Dollar), stocks, forex
- **Timeframes**: 1s, 5s, 10s, 1m, 5m, 15m, 1h, 1D
- **Metrics**: Sharpe Ratio, Sortino Ratio, Maximum Drawdown, Win Rate

## 🔌 API and Scripts

### **gRPC Services**
- **MarketData**: Streaming of ticks, rates, historical data
- **OrderManagement**: Position, order and trading history management

### **Main Scripts**
```bash
python multiserver.py 5051+4 5060+2    # Multiple servers for load balancing
python backtest.py                      # Standalone backtesting
```

### **Automatic Reports**
- Excel files with performance metrics (Sharpe, Sortino, Max Drawdown)
- Detailed trade history and equity curves

## 🏭 Architecture and Strategies

### **Service Pattern**
The system uses specialized loops for:
- **Monitoring**: Positions, orders, system integrity
- **Processing**: Real-time market data
- **Execution**: Automated buy/sell strategies

### **Implemented Strategies**

**Range Chart Strategy**
- Based on fixed point price movements (configurable brick size)
- Ideal for volatile markets like WIN and WDO

**Moving Average Strategy**  
- Moving average crossover with ATR confirmation
- Configurable period (default: 50 periods)

**ATR Dynamic Strategy**
- Dynamic stop loss and take profit based on volatility
- Adjustable 1:2 risk/reward ratio

## 🔍 Monitoring and Performance

### **Logging**
- **Serilog** with configurable levels (Debug, Info, Warning, Error)
- Outputs: Console, rotating files, Elasticsearch (optional)
- Metrics: Performance, latency, error rate

### **Optimizations**
- **gRPC**: Object pooling, streaming, NumPy compression
- **Memory**: Optimized garbage collection, buffer pooling
- **Benchmarks**: < 5ms latency, > 10k ticks/second, < 500MB RAM

## ❗ Troubleshooting

### **Common Issues**

**MetaTrader 5 won't connect:**
```bash
# Check if MT5 is running and test Python API
python -c "import MetaTrader5 as mt5; print(mt5.initialize())"
```

**gRPC Connection Refused:**
```bash
# Check if server is active on port
netstat -an | grep :5051
```

**Protocol Buffers Error:**
```bash
# Regenerate proto files and recompile
cd grpc_server && ./codegen.bat
cd ../market_analyzer && dotnet clean && dotnet build
```

## 🚀 Roadmap

### **Main Roadmap**
- **Web Interface**: Real-time dashboard with SignalR
- **Machine Learning**: Automatic parameter optimization
- **Multi-Broker**: Interactive Brokers, Binance
- **Mobile App**: Smartphone monitoring
- **Microservices**: Cloud-native architecture with Kubernetes

## 🔒 Security and Compliance

### **Security Measures**
- **Communication**: TLS 1.3 encrypted for all connections
- **Authentication**: JWT tokens and role-based access control
- **Auditing**: Complete operation logs and audit trail

### **Risk Management**
- **Mandatory stop loss** and Kelly Criterion-based position sizing
- **Drawdown control** with automatic stop on excessive losses
- **Automatic backup** of configurations and system state

## 📝 License

### **MIT License**
**Copyright © 2024-2025 Phoenix Project**

This project is licensed under the **MIT License** - permissive for commercial use, distribution and modification.

### **⚠️ IMPORTANT WARNING - FINANCIAL RISKS**

**Automated trading involves substantial risks:**
- **High Risk**: May result in total loss of invested capital
- **No Guarantees**: Past performance does not guarantee future results
- **Mandatory Testing**: Always test in demo environment first
- **Not Financial Advice**: This is software, not financial advice

### **Responsible Use**
**USE AT YOUR OWN RISK AND ONLY WITH CAPITAL YOU CAN AFFORD TO LOSE.**

**📄 Full license: [LICENSE.md](LICENSE.md)**

## 👥 Contributing

**Contributions are welcome!** 

### **How to Contribute**
```bash
git clone https://github.com/agabopinho/phoenix-project.git
git checkout -b feature/new-feature
# Make your changes
git commit -m "Add new feature"
git push origin feature/new-feature
# Open a Pull Request
```

### **Types of Contributions**
- 🐛 **Bug fixes** and code improvements
- ✨ **New strategies** and technical indicators  
- 📚 **Documentation** and practical examples
- 🧪 **Unit and integration tests**
- ⚡ **Performance optimizations**

### **Guidelines**
- Follow project code conventions
- Add tests for new features
- Document significant changes
- Use descriptive commit messages

## 📞 Support and Community

### **Getting Help**
- **🐛 Bugs and Features**: [GitHub Issues](https://github.com/agabopinho/phoenix-project/issues)
- **💬 Discussions**: [GitHub Discussions](https://github.com/agabopinho/phoenix-project/discussions)
- **📖 Documentation**: README.md and code comments

### **Community**
- ⭐ **Star** the project to support development
- 👀 **Watch** to receive update notifications
- 🍴 **Fork** for your own modifications
- 🤝 **Contribute** by helping other users and reporting bugs

---

**⚠️ Disclaimer**: This system is intended for educational and research purposes. Automated trading involves significant risks. Use responsibly and always test in a demo environment before operating with real money.
