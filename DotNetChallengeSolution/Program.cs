using System.Globalization;

namespace Solution
{
    public class Program
    {
        public int NumberOfSecurities { get; set; }
        public int NumberOfDays { get; set; }
        public int LookAheadDays = 2;
        public decimal StartCapital { get; set; }
        public Dictionary<string, Security> SecurityMap { get; set; } = new Dictionary<string, Security>();

        public decimal CurrentCapital { get; set; }
        public Dictionary<string, int> SecurityInStock { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SecurityInPortfolio { get; set; } = new Dictionary<string, int>();
        public int MaximumAllowedTrades { get; } = 10;

        public List<List<string>> Trades = new List<List<string>>();

        public static void Main(string[] args)
        {
            var inputFile = args[0];
            var outputFile = args[1];
            Console.WriteLine($"Taking input file from path {inputFile}");
            Console.WriteLine($"Taking output file from path {outputFile}");
            var program = new Program();
            program.ParseInputFile(inputFile);
            program.InitializeSolverData();

            program.StartTrading();

            program.WriteOutputFile(outputFile);
        }

        private void OutputFinalTotal(decimal remainingCash, decimal portfolioTotal)
        {
            var finalTotal = remainingCash + portfolioTotal;
            Console.WriteLine($"Final total: {finalTotal} (cash: {remainingCash} + securities: {portfolioTotal})");
        }

        private void WriteOutputFile(string outputFile)
        {
            using var file = new FileStream(outputFile, FileMode.Create);
            using var writer = new StreamWriter(file);
            var daysTraded = Trades.Where(x=>x.Count > 0).Count();
            writer.WriteLine(daysTraded);
            for (int tradingDay = 0; tradingDay < Trades.Count; tradingDay++)
            {
                writer.WriteLine($"{tradingDay} {Trades[tradingDay].Count}");
                foreach (var trade in Trades[tradingDay])
                {
                    writer.WriteLine(trade);
                }
            }
            writer.Flush();
        }

        private void StartTrading()
        {
            var valueRemaining = StartCapital;
            for (int day = 0; day < NumberOfDays; day++)
            {
                Trades.Add(new List<string>());
                var allOutlooks = CalculateAllOutlooks(day);
                var top = allOutlooks.OrderByDescending(o => o.Value).Where(o => o.Value > 0).Take(10).ToArray();
                var topNames = new HashSet<string>(top.Select(x => x.Key));

                var toSell = SecurityInPortfolio
                    .Where(x => x.Value > 0 && !topNames.Contains(x.Key))
                    .OrderBy(x => allOutlooks[x.Key])
                    .ToArray();

                int tradesRemaining = 10;
                int buy = 0;
                int sell = 0;
                while (valueRemaining > 0 && tradesRemaining > 1 && buy < top.Length)
                {
                    var valueToBuy = SecurityMap[top[buy].Key].StockPrices[day] * SecurityInStock[top[buy].Key];
                    while (valueToBuy > valueRemaining && tradesRemaining > 1 && sell < toSell.Length)
                    {
                        valueRemaining += Sell(day, toSell[sell].Key, toSell[sell].Value);
                        sell++;
                        tradesRemaining--;
                    }

                    var valueBought = Buy(day, top[buy].Key, valueRemaining);
                    if (valueBought > 0)
                    {
                        tradesRemaining--;
                        valueRemaining -= valueBought;
                    }

                    buy++;
                }
            }

            OutputFinalTotal(valueRemaining, SecurityInPortfolio.Select(x=>x.Value * SecurityMap[x.Key].StockPrices.Last()).Sum());
        }

        private decimal Sell(int tradingDay, string name, int qtty)
        {
            var map = SecurityMap[name];
            var price = map.StockPrices[tradingDay];
            var value = price * qtty;

            SecurityInPortfolio[name] = 0;
            SecurityInStock[name] = SecurityInStock[name] + qtty;
            Trades[tradingDay].Add($"{name} SELL {qtty}");

            return value;
        }

        private decimal Buy(int tradingDay, string name, decimal maxValue)
        {
            var map = SecurityMap[name];
            var price = map.StockPrices[tradingDay];
            var qttyToBuy = Math.Min((int)(maxValue / price), SecurityInStock[name]);
            var value = qttyToBuy * price;

            if (qttyToBuy > 0)
            {
                SecurityInStock[name] = SecurityInStock[name] - qttyToBuy;
                SecurityInPortfolio[name] = SecurityInPortfolio[name] + qttyToBuy;
                Trades[tradingDay].Add($"{name} BUY {qttyToBuy}");
            }

            return value;
        }

        private Dictionary<string, decimal> CalculateAllOutlooks(int day)
        {
            return SecurityMap
                .Select(s =>
                {
                    var values = s.Value.StockPrices
                        .Select(x => (x - s.Value.StockPrices[day]) * s.Value.StockAvailable)
                        .Skip(day).Take(LookAheadDays).ToArray();

                    var outlook = values.Max();
                    return new { s.Key, outlook };
                }).ToDictionary(x => x.Key, x => x.outlook);
        }

        public void ParseInputFile(string inputFile)
        {
            using var streamReader = new StreamReader(inputFile);
            var firstLine = streamReader.ReadLine().Trim().Split();
            NumberOfSecurities = Int32.Parse(firstLine[0]);
            NumberOfDays = Int32.Parse(firstLine[1]);
            StartCapital = Convert.ToDecimal(firstLine[2], CultureInfo.GetCultureInfo("en-US"));

            for (var i = 0; i < NumberOfSecurities; i++)
            {
                var security = new Security();
                var splitLine = streamReader.ReadLine().Trim().Split();
                security.Name = splitLine[0];
                security.StockAvailable = Convert.ToInt32(splitLine[1]);
                security.StockPrices = streamReader.ReadLine().Trim().Split().Select(s => Convert.ToDecimal(s, CultureInfo.GetCultureInfo("en-US"))).ToArray();
                SecurityMap[security.Name] = security;
            }

            streamReader.Close();
        }

        public void InitializeSolverData()
        {
            CurrentCapital = StartCapital;
            foreach (var securityName in SecurityMap.Keys)
            {
                SecurityInStock[securityName] = SecurityMap[securityName].StockAvailable;
                SecurityInPortfolio[securityName] = 0;
            }
        }

    }
    public class Security
    {
        public string Name { get; set; }
        public int StockAvailable { get; set; }
        public decimal[] StockPrices { get; set; }
    }
}
