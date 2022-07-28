use master;
go

drop database backtest_data;
go

create database backtest_data;
go

use backtest_data;
go

create table trade
(
    id int not null identity,
    symbol varchar (100) not null,
    [time] datetime2 not null,
    bid float not null,
    ask float not null,
    [last] float not null,
    volume float not null,
    flags int not null,
    volumeReal float not null,
    createAt datetime2 not null constraint df_trade_createAt default (getdate())

    constraint pk_trade primary key (id)
);

create index ix_trade_01 on trade (symbol, [time])