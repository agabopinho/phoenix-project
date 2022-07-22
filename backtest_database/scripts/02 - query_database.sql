use backtest_data;
go

select convert(date, [time]), count(*)
from trade with (nolock)
group by convert(date, [time])
order by 1
