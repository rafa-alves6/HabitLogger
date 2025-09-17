using System.Data.SQLite;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HabitLogger;

public static class Validation
{
    public static bool IsValidMenuOption(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        return Regex.IsMatch(input, "^[0-4]$");
    }
    
    public static bool IsStringValid(string? name)
    {
        return !string.IsNullOrWhiteSpace(name);
    }
    
    public static bool IsQuantityValid(string? quantity)
    {
        return double.TryParse(quantity, CultureInfo.InvariantCulture, out _);
    }
    
    public static bool IsDateValid(string? date, out DateOnly parsedDate)
    {
        parsedDate = default;
        if (string.IsNullOrWhiteSpace(date)) return false;

        string[] formats = ["yyyy-MM-dd", "dd-MM-yyyy"];
        if (!DateOnly.TryParseExact(date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
        {
            return false;
        }

        if (parsedDate > DateOnly.FromDateTime(DateTime.Now))
        {
            return false;
        }

        return true;
    }
}

class Program
{
    static readonly SQLiteConnection db = CreateDbConnection();
    static void Main(string[] args)
    {
        db.Open();

        string sql = "CREATE TABLE IF NOT EXISTS Habit(id_habit INTEGER PRIMARY KEY, name TEXT NOT NULL, quantity REAL NOT NULL, habit_date TEXT NOT NULL)";
        ExecuteSQLNonQuery(sql, db);

        string option;

        do
        {
            Console.Clear();
            ShowOptions();
            option = GetMenuInput();
            EvaluateUserOption(option);

            if (option != "0")
            {
                Console.WriteLine("\nPress any key to return to the menu...");
                Console.ReadKey();
            }

        } while (option != "0");

        db.Close();
    }
    
    static void ViewAllRecords()
    {
        string sql = "SELECT * FROM Habit";
        using SQLiteDataReader reader = GetReader(sql, db);
        ViewSelectedColumns(reader);
    }

    static string GetValidName()
    {
        Console.Write("Enter the habit name: ");
        string? input = Console.ReadLine();
        while (!Validation.IsStringValid(input))
        {
            Console.Write("Name cannot be empty. Please enter a valid name: ");
            input = Console.ReadLine();
        }
        return input;
    }

    static double GetValidQuantity()
    {
        Console.Write("Enter the quantity: ");
        string? input = Console.ReadLine();
        while (!Validation.IsQuantityValid(input))
        {
            Console.Write("Invalid number. Please enter a valid quantity: ");
            input = Console.ReadLine();
        }
        return double.Parse(input, CultureInfo.InvariantCulture);
    }

    static string GetValidDate()
    {
        Console.Write("Enter the date (yyyy-MM-dd or dd-MM-yyyy): ");
        string? input = Console.ReadLine();
        DateOnly parsedDate;
        
        while (!Validation.IsDateValid(input, out parsedDate))
        {
            Console.Write("Invalid date. Please use an accepted format and do not enter future dates: ");
            input = Console.ReadLine();
        }
        
        return parsedDate.ToString("yyyy-MM-dd");
    }

    static void InsertHabit()
    {
        string habitName = GetValidName();
        double habitQuantity = GetValidQuantity();
        string habitDate = GetValidDate();

        string sql = "INSERT INTO Habit(name, quantity, habit_date) VALUES (@name, @quantity, @date)";
        using SQLiteCommand command = new(sql, db);
        command.Parameters.AddWithValue("@name", habitName);
        command.Parameters.AddWithValue("@quantity", habitQuantity);
        command.Parameters.AddWithValue("@date", habitDate);

        command.ExecuteNonQuery();
        Console.WriteLine("\nRecord inserted successfully!");
    }

    static void ViewSelectedColumns(SQLiteDataReader reader)
    {
        if (!reader.HasRows)
        {
            Console.WriteLine("No records found.");
            return;
        }

        Console.WriteLine("---------------------------------");
        while (reader.Read())
        {
            int id = reader.GetInt32(reader.GetOrdinal("id_habit"));
            string name = reader.GetString(reader.GetOrdinal("name"));
            double quantity = reader.GetDouble(reader.GetOrdinal("quantity"));
            string date = reader.GetString(reader.GetOrdinal("habit_date"));
            DateTime dt = DateTime.Parse(date);

            Console.WriteLine($"Id: {id} | Habit: {name} | Quantity: {quantity} | Date: {dt:d}");
        }
        Console.WriteLine("---------------------------------");
    }

    static SQLiteDataReader GetReader(string selectSql, SQLiteConnection db)
    {
        SQLiteCommand command = new(selectSql, db);
        return command.ExecuteReader();
    }

    static void ExecuteSQLNonQuery(string sql, SQLiteConnection db)
    {
        using SQLiteCommand command = new(sql, db);
        command.ExecuteNonQuery();
    }

    static void EvaluateUserOption(string option)
    {
        switch (option)
        {
            case "0":
                Console.WriteLine("\nClosing application. Goodbye!");
                break;
            case "1":
                ViewAllRecords();
                break;
            case "2":
                InsertHabit();
                break;
            case "3":
                Console.WriteLine("\nDelete Record - Not implemented yet.");
                break;
            case "4":
                Console.WriteLine("\nUpdate Record - Not implemented yet.");
                break;
        }
    }

    static void ShowOptions()
    {
        Console.WriteLine("MAIN MENU\n");
        Console.WriteLine("What would you like to do?\n");
        Console.WriteLine("Type 0 to Close Application.");
        Console.WriteLine("Type 1 to View All Records.");
        Console.WriteLine("Type 2 to Insert Record.");
        Console.WriteLine("Type 3 to Delete Record.");
        Console.WriteLine("Type 4 to Update Record.");
        Console.WriteLine("----------------------------\n");
        Console.Write("Your option? ");
    }
    
    static string GetMenuInput()
    {
        string? input = Console.ReadLine();
        while (!Validation.IsValidMenuOption(input))
        {
            Console.Write("Invalid option. Please choose a valid option (0-4): ");
            input = Console.ReadLine();
        }
        return input!;
    }

    static SQLiteConnection CreateDbConnection()
    {
        string dbPath = "HabitLogger.sqlite";
        string connectionString = $"Data Source={dbPath}";
        return new SQLiteConnection(connectionString);
    }
}