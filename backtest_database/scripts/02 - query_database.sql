use backtest_data;
go

select convert(date, [time]), count(*)
from trade with (nolock)
group by convert(date, [time])
order by 1

select top 100000 * 
from trade
where symbol = 'WIN$'
and convert(date, [time]) = convert(date, getdate())
order by [time]

