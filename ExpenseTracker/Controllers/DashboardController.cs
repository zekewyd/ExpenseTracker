using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Models;
using Transaction = ExpenseTracker.Models.Transaction;
using ExpenseTracker.Data;

namespace ExpenseTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ExpenseDbContext _context;

        public DashboardController(ExpenseDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            // Date range for last 7 days
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;

            // All transactions
            List<Transaction> AllTransactions = await _context.Transactions
                .Include(x => x.Category)
                .ToListAsync();

            // Transactions for the current month
            DateTime FirstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            List<Transaction> SelectedTransactionsMonthly = AllTransactions
                .Where(y => y.Date >= FirstDayOfMonth && y.Date <= EndDate)
                .ToList();

            // Transactions for the last 7 days
            List<Transaction> SelectedTransactionsLast7Days = AllTransactions
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToList();

            // Total income (all transactions)
            int TotalIncome = AllTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            // Total expense (all transactions)
            int TotalExpense = AllTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C0");

            // Balance (all transactions)
            int Balance = TotalIncome - TotalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencySymbol = "₱";
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            // Doughnut chart - expense by category (monthly)
            ViewBag.DoughnutChartData = SelectedTransactionsMonthly
               .Where(i => i.Category.Type == "Expense")
               .GroupBy(j => j.Category.CategoryId)
               .Select(k => new
               {
                   categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                   amount = k.Sum(j => j.Amount),
                   formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
               })
               .OrderByDescending(l => l.amount)
               .ToList();

            // Spline chart - income vs expense (last 7 days)
            // Income summary
            List<SplineChartData> IncomeSummary = SelectedTransactionsLast7Days
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            // Expense summary
            List<SplineChartData> ExpenseSummary = SelectedTransactionsLast7Days
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            // Combine income & expense
            string[] Last7Days = Enumerable.Range(0, 7)
                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last7Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };

            // Recent transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }

}