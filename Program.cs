using NBitcoin;
using PhantasmaPhoenix.Cryptography;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading; // Added for Thread.Sleep visibility

public class VanityAddressGenerator
{
    private enum MatchResult { FullMatch, PartialMatch, NoMatch }
    private enum MatchCategory { FullMatch, CaseMatch, PositionMatch, GenericPartialMatch }

    public static void Main(string[] args)
    {

        string? continueChoice;
        do
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.WriteLine("Phantasma Vanity Address Generator");
            Console.ResetColor();
            Console.WriteLine("-----------------------------------");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Press enter for default values");
            Console.ResetColor();

            string desiredPattern;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.Write("Enter the desired pattern: \n");
                Console.ResetColor();

                desiredPattern = Console.ReadLine() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(desiredPattern))
                {
                    Console.WriteLine("\n❌ Error: Pattern cannot be empty. Please enter a pattern.");
                }
                else if (desiredPattern.Any(c => char.IsWhiteSpace(c) || !char.IsLetterOrDigit(c)))
                {
                    Console.WriteLine("\n❌ Error: Pattern cannot contain spaces or special characters. Please use only letters and numbers.");
                }
                else
                {
                    break;
                }
            }

            string caseChoice;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.WriteLine("\nChoose case sensitivity (Default: 1):");
                Console.ResetColor();
                Console.WriteLine("1. Case-sensitive (Slow)");
                Console.WriteLine("2. Case-insensitive (Fast)");
                
                Console.Write("Enter your choice (1 or 2): \n");
                caseChoice = Console.ReadLine() ?? "1";
                if (string.IsNullOrWhiteSpace(caseChoice)) caseChoice = "1";
                if (caseChoice == "1" || caseChoice == "2")
                {
                    break;
                }
                Console.WriteLine("\nInvalid choice. Please enter 1 or 2.");
            }
            bool isCaseSensitive = caseChoice == "1";

            string positionChoice;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.WriteLine("\nChoose pattern position (Default: 1):");
                Console.ResetColor();
                Console.WriteLine("1. At the end (Slow)");
                Console.WriteLine("2. Anywhere (Fast)");               
                Console.Write("Enter your choice (1 or 2): \n");
                positionChoice = Console.ReadLine() ?? "1";
                if (string.IsNullOrWhiteSpace(positionChoice)) positionChoice = "1";
                if (positionChoice == "1" || positionChoice == "2")
                {
                    break;
                }
                Console.WriteLine("\nInvalid choice. Please enter 1 or 2.");
            }
            bool isAtEnd = positionChoice == "1";

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.Write("\nEnter a give-up time in minutes (0 for no time limit) (Default: 0): \n");
            Console.ResetColor();
            string giveUpInput = Console.ReadLine() ?? "0";
            if (string.IsNullOrWhiteSpace(giveUpInput)) giveUpInput = "0";
            int giveUpMinutes;
            while (!int.TryParse(giveUpInput, out giveUpMinutes) || giveUpMinutes < 0)
            {
                Console.Write("Invalid input. Please enter a positive integer: ");
                giveUpInput = Console.ReadLine() ?? "0";
                if (string.IsNullOrWhiteSpace(giveUpInput)) giveUpInput = "0";
            }
            TimeSpan giveUpTime = TimeSpan.FromMinutes(giveUpMinutes);

            string matchesInput;
            int desiredMatches;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.Write("\nEnter the number of full matches you want to find (Default: 1): \n");
                Console.ResetColor();

                matchesInput = Console.ReadLine() ?? "1";
                if (string.IsNullOrWhiteSpace(matchesInput)) matchesInput = "1";

                if (int.TryParse(matchesInput, out desiredMatches) && desiredMatches > 0)
                {
                    break;
                }
                Console.WriteLine("\nInvalid input. Please enter a positive integer.");
            }

            string genChoice;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.WriteLine("\nChoose a generation method (Default: 1):");
                Console.ResetColor();
                Console.WriteLine("1. Fast (Generate from random private key) (around 400x faster)");
                Console.WriteLine("2. Slow (Generate from a random seed phrase)");
                Console.WriteLine("3. Smart (Generate from a single new seed phrase, changing index)");
                Console.WriteLine("4. User (Find an address on a specific seed phrase, changing index)");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nFor 3rd and 4th options be aware 99% of wallets not supports manually entering index number and if you lost index number it can be impossible to recover your assets please save your wif too.");
                Console.ResetColor();
                Console.Write("\nEnter your choice (1, 2, 3, or 4): \n");
                genChoice = Console.ReadLine() ?? "1";
                if (string.IsNullOrWhiteSpace(genChoice)) genChoice = "1";
                if (genChoice == "1" || genChoice == "2" || genChoice == "3" || genChoice == "4")
                {
                    break;
                }
                Console.WriteLine("\nInvalid choice. Please enter 1, 2, 3, or 4.");
            }

            string savePartialMatchesChoice;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.WriteLine("\nSave partial matches to file? (y/n) (Default: n): \n");
                Console.ResetColor();
                savePartialMatchesChoice = Console.ReadLine() ?? "n";
                if (string.IsNullOrWhiteSpace(savePartialMatchesChoice)) savePartialMatchesChoice = "n";
                if (savePartialMatchesChoice.ToLower() == "y" || savePartialMatchesChoice.ToLower() == "n")
                {
                    break;
                }
                Console.WriteLine("\nInvalid choice. Please enter 'y' or 'n'.");
            }
            bool savePartialMatches = savePartialMatchesChoice.ToLower() == "y";

            bool wasCanceled = false;

            switch (genChoice)
            {
                case "1":
                    wasCanceled = !GenerateFromKeyPair(desiredPattern, isCaseSensitive, isAtEnd, giveUpTime, savePartialMatches, desiredMatches);
                    break;
                case "2":
                    wasCanceled = !GenerateFromSeedPhrase(desiredPattern, isCaseSensitive, isAtEnd, giveUpTime, savePartialMatches, desiredMatches);
                    break;
                case "3":
                    wasCanceled = !GenerateFromSeedWithIndex(desiredPattern, isCaseSensitive, isAtEnd, giveUpTime, savePartialMatches, desiredMatches);
                    break;
                case "4":
                    wasCanceled = !GenerateFromUserSeedWithIndex(desiredPattern, isCaseSensitive, isAtEnd, giveUpTime, savePartialMatches, desiredMatches);
                    break;
            }

            if (wasCanceled)
            {
                Console.WriteLine("\nSearch was canceled.");
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\nDo you want to run another search? (y/n) (Default:n): \n");
            Console.ResetColor();

            continueChoice = Console.ReadLine() ?? "n";

            switch (continueChoice)
            {
                case "y":
                    break;
                case "n":
                    break;
                default:
                    continueChoice = "n";
                    break;
            }

        } while (continueChoice?.ToLower() == "y");

        Console.WriteLine("Program exiting. Press any key to close...");
        Console.ReadKey();
    }

    //-----------------------------------------------------------------------------------------
    // Method 1: Generate from random private key
    //-----------------------------------------------------------------------------------------
    public static bool GenerateFromKeyPair(string desiredPattern, bool isCaseSensitive, bool isAtEnd, TimeSpan giveUpTime, bool savePartialMatches, int desiredMatches)
    {
        Console.WriteLine("\nStarting fast generation...");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Press 'P' for pause, 'Esc' to exit or retry...");
        Console.ResetColor();
        long attempts = 0;
        int foundFullMatches = 0;
        int foundCaseMatches = 0;
        int foundPositionMatches = 0;
        int foundGenericMatches = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            var keyPair = PhantasmaKeys.Generate();
            var address = keyPair.Address.Text;
            attempts++;
            if (!CheckTimerAndInput(attempts, stopwatch, giveUpTime, foundFullMatches, desiredMatches, foundCaseMatches, foundPositionMatches, foundGenericMatches)) return false;

            MatchCategory matchCategory = CategorizeMatch(address, desiredPattern, isCaseSensitive, isAtEnd);

            switch (matchCategory)
            {
                case MatchCategory.FullMatch:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nSuccess! Found a full-match vanity address!");
                    Console.ResetColor();
                    Console.WriteLine($"Matching Address: {address}");
                    Console.WriteLine($"Private Key: {keyPair.ToWIF()}");
                    SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), null, true, matchCategory);
                    foundFullMatches++;
                    if (foundFullMatches >= desiredMatches)
                    {
                        stopwatch.Stop();
                        return true;
                    }
                    break;
                case MatchCategory.CaseMatch:
                    foundCaseMatches++;
                    if (savePartialMatches)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        Console.ResetColor();
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), null, false, matchCategory);
                    }
                    break;
                case MatchCategory.PositionMatch:
                    foundPositionMatches++;
                    if (savePartialMatches)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        Console.ResetColor();
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), null, false, matchCategory);
                    }
                    break;
                case MatchCategory.GenericPartialMatch:
                    foundGenericMatches++;
                    break;
            }
        }
    }

    //-----------------------------------------------------------------------------------------
    // Method 2: Generate from random seed phrase
    //-----------------------------------------------------------------------------------------
    public static bool GenerateFromSeedPhrase(string desiredPattern, bool isCaseSensitive, bool isAtEnd, TimeSpan giveUpTime, bool savePartialMatches, int desiredMatches)
    {
        Console.WriteLine("\nStarting slow generation...");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Press 'P' for pause, 'Esc' to exit or retry...");
        Console.ResetColor();
        long attempts = 0;
        int foundFullMatches = 0;
        int foundCaseMatches = 0;
        int foundPositionMatches = 0;
        int foundGenericMatches = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            string seedPhrase = Mnemonics.GenerateMnemonic(MnemonicPhraseLength.Twelve_Words);

            var (pk, errorMessage) = Mnemonics.MnemonicToPK(seedPhrase);

            if (pk == null || errorMessage != null)
            {
                Console.WriteLine($"Error occured: {errorMessage}");
                continue;
            }

            var keyPair = new PhantasmaKeys(pk);
            var address = keyPair.Address.Text;
            attempts++;
            if (!CheckTimerAndInput(attempts, stopwatch, giveUpTime, foundFullMatches, desiredMatches, foundCaseMatches, foundPositionMatches, foundGenericMatches)) return false;
            MatchCategory matchCategory = CategorizeMatch(address, desiredPattern, isCaseSensitive, isAtEnd);

            switch (matchCategory)
            {
                case MatchCategory.FullMatch:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nSuccess! Found a full-match vanity address!");
                    Console.ResetColor();
                    Console.WriteLine($"Matching Address: {address}");
                    Console.WriteLine($"Seed Phrase: {seedPhrase}");
                    SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, true, matchCategory);
                    foundFullMatches++;
                    if (foundFullMatches >= desiredMatches)
                    {
                        stopwatch.Stop();
                        return true;
                    }
                    break;
                case MatchCategory.CaseMatch:
                    foundCaseMatches++;
                    if (savePartialMatches)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        Console.ResetColor();
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, false, matchCategory);
                    }
                    break;
                case MatchCategory.PositionMatch:
                    foundPositionMatches++;
                    if (savePartialMatches)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        Console.ResetColor();
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, false, matchCategory);
                    }
                    break;
                case MatchCategory.GenericPartialMatch:
                    foundGenericMatches++;
                    break;
            }
        }
    }

    //-----------------------------------------------------------------------------------------
    // Method 3: Generate from a single new seed phrase, changing index
    //-----------------------------------------------------------------------------------------
    public static bool GenerateFromSeedWithIndex(string desiredPattern, bool isCaseSensitive, bool isAtEnd, TimeSpan giveUpTime, bool savePartialMatches, int desiredMatches)
    {
        Console.WriteLine("\nStarting smart generation\nPress 'P' for pause, 'Esc' to exit or retry...");
        string seedPhrase = Mnemonics.GenerateMnemonic(MnemonicPhraseLength.Twelve_Words);
        Console.WriteLine($"\nBase Seed Phrase: {seedPhrase}");
        Console.WriteLine("Searching for matching address by incrementing index...");
        uint attempts = 0;
        int foundFullMatches = 0;
        int foundCaseMatches = 0;
        int foundPositionMatches = 0;
        int foundGenericMatches = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            var (pk, errorMessage) = Mnemonics.MnemonicToPK(seedPhrase, attempts);

            if (pk == null || errorMessage != null)
            {
                Console.WriteLine($"Error occured: {errorMessage}");
                continue;
            }

            var keyPair = new PhantasmaKeys(pk);
            var address = keyPair.Address.Text;
            attempts++;
            if (!CheckTimerAndInput(attempts, stopwatch, giveUpTime, foundFullMatches, desiredMatches, foundCaseMatches, foundPositionMatches, foundGenericMatches)) return false;
            MatchCategory matchCategory = CategorizeMatch(address, desiredPattern, isCaseSensitive, isAtEnd);

            switch (matchCategory)
            {
                case MatchCategory.FullMatch:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nSuccess! Found a full-match vanity address!");
                    Console.ResetColor();
                    Console.WriteLine($"Matching Address: {address}");
                    Console.WriteLine($"Derived from Index: {attempts - 1}");
                    Console.WriteLine($"Original Seed Phrase: {seedPhrase}");
                    SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, true, matchCategory, attempts - 1);
                    foundFullMatches++;
                    if (foundFullMatches >= desiredMatches)
                    {
                        stopwatch.Stop();
                        return true;
                    }
                    break;
                case MatchCategory.CaseMatch:
                    foundCaseMatches++;
                    if (savePartialMatches)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        Console.ResetColor();
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, false, matchCategory, attempts - 1);
                    }
                    break;
                case MatchCategory.PositionMatch:
                    foundPositionMatches++;
                    if (savePartialMatches)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        Console.ResetColor();
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), seedPhrase, false, matchCategory, attempts - 1);
                    }
                    break;
                case MatchCategory.GenericPartialMatch:
                    foundGenericMatches++;
                    break;
            }

            if (attempts == 4294967295)
            {
                Console.WriteLine("Reached maximum attempts try again with shorter pattern or select fast options.");
                return true;
            }
        }
    }

    //-----------------------------------------------------------------------------------------
    // Method 4: Find an address on a user-provided seed phrase, changing index
    //-----------------------------------------------------------------------------------------
    public static bool GenerateFromUserSeedWithIndex(string desiredPattern, bool isCaseSensitive, bool isAtEnd, TimeSpan giveUpTime, bool savePartialMatches, int desiredMatches)
    {
        Console.WriteLine("\n⚠️ WARNING: Enter your seed phrase ONLY on an offline computer.");
        Console.Write("Enter your 12 or 24-word seed phrase (separated by spaces): ");
        string userSeedPhrase = Console.ReadLine() ?? string.Empty;
        Console.WriteLine($"\nSearching for matching address from your seed phrase...");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Press 'P' for pause, 'Esc' to exit or retry...");
        Console.ResetColor();
        uint attempts = 0;
        int foundFullMatches = 0;
        int foundCaseMatches = 0;
        int foundPositionMatches = 0;
        int foundGenericMatches = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            var (pk, errorMessage) = Mnemonics.MnemonicToPK(userSeedPhrase, attempts);

            if (pk == null || errorMessage != null)
            {
                Console.WriteLine($"Error occured: {errorMessage}");
                return true;
            }
            var keyPair = new PhantasmaKeys(pk);
            var address = keyPair.Address.Text;
            attempts++;
            if (!CheckTimerAndInput(attempts, stopwatch, giveUpTime, foundFullMatches, desiredMatches, foundCaseMatches, foundPositionMatches, foundGenericMatches)) return false;
            MatchCategory matchCategory = CategorizeMatch(address, desiredPattern, isCaseSensitive, isAtEnd);

            switch (matchCategory)
            {
                case MatchCategory.FullMatch:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nSuccess! Found a full-match vanity address!");
                    Console.ResetColor();
                    Console.WriteLine($"Matching Address: {address}");
                    Console.WriteLine($"Derived from Index: {attempts - 1}");
                    Console.WriteLine($"Original Seed Phrase: {userSeedPhrase}");
                    SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), userSeedPhrase, true, matchCategory, attempts - 1);
                    foundFullMatches++;
                    if (foundFullMatches >= desiredMatches)
                    {
                        stopwatch.Stop();
                        return true;
                    }
                    break;
                case MatchCategory.CaseMatch:
                    foundCaseMatches++;
                    if (savePartialMatches)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        Console.ResetColor();
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), userSeedPhrase, false, matchCategory, attempts - 1);
                    }
                    break;
                case MatchCategory.PositionMatch:
                    foundPositionMatches++;
                    if (savePartialMatches)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n\n✅ {matchCategory} found: {address}");
                        Console.ResetColor();
                        SaveMatchToFile(desiredPattern, address, keyPair.ToWIF(), userSeedPhrase, false, matchCategory, attempts - 1);
                    }
                    break;
                case MatchCategory.GenericPartialMatch:
                    foundGenericMatches++;
                    break;
            }

            if (attempts == 4294967295)
            {
                Console.WriteLine("Reached maximum attempts try again with shorter pattern or select fast options.");
                return true;
            }
        }
    }

    //-----------------------------------------------------------------------------------------
    // Helper Functions
    //-----------------------------------------------------------------------------------------
    private static bool CheckTimerAndInput(long attempts, Stopwatch stopwatch, TimeSpan giveUpTime, int foundFullMatches, int desiredFullMatches, int foundCaseMatches, int foundPositionMatches, int foundGenericMatches)
    {
        if (giveUpTime > TimeSpan.Zero && stopwatch.Elapsed > giveUpTime)
        {
            Console.WriteLine("\n\n❌ Timed out. No matching address found.");
            return false;
        }

        // Check for pause or exit input
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.Escape)
            {
                stopwatch.Stop();
                return false;
            }
            if (key == ConsoleKey.P)
            {
                stopwatch.Stop();
                Console.WriteLine("\n\n⏸️ Paused. Press any key to continue...");
                // Ensure the final status line before the pause is fully visible
                Console.CursorVisible = true;
                Console.ReadKey(true);
                // After key press, resume and reset the cursor visible state if necessary
                Console.CursorVisible = false;
                stopwatch.Start();
            }
        }

        // Update status line every 1000 attempts
        if (attempts % 1000 == 0)
        {
            TimeSpan elapsed = stopwatch.Elapsed;
            double attemptsPerSecond = attempts / elapsed.TotalSeconds;

            string remainingTime = giveUpTime > TimeSpan.Zero
                ? $" | Remaining: {(giveUpTime - elapsed).ToString(@"hh\:mm\:ss")}"
                : "";

            // Construct the full status line, starting with '\r'
            string statusLine =
                $"\rChecked {attempts:N0} | Elapsed: {elapsed.ToString(@"hh\:mm\:ss")} | Speed: {attemptsPerSecond:N2} adds/sec{remainingTime} | Matches: F:{foundFullMatches}/{desiredFullMatches} C:{foundCaseMatches} P:{foundPositionMatches} G:{foundGenericMatches}";

            // --- Truncation Logic Updated Here ---
            try
            {
                int consoleWidth = Console.WindowWidth;
                const string ellipsis = " ..."; // Ellipsis plus a leading space for better readability
                int ellipsisLength = ellipsis.Length;

                if (statusLine.Length > consoleWidth)
                {
                    // Calculate the maximum length the status text can be before adding the ellipsis.
                    // We need to reserve space for the ellipsis and one extra character for safety 
                    // (to prevent wrapping due to hidden terminal characters or cursor position).
                    int truncateLength = consoleWidth - ellipsisLength - 1;

                    if (truncateLength > 0)
                    {
                        statusLine = statusLine.Substring(0, truncateLength) + ellipsis;
                    }
                    else
                    {
                        // Handle extremely small console widths by showing at least the ellipsis
                        statusLine = ellipsis;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore console errors if running in a restricted environment
            }
            // --- End Truncation Logic ---

            Console.Write(statusLine);
        }
        return true;
    }

    private static MatchCategory CategorizeMatch(string address, string pattern, bool isCaseSensitive, bool isAtEnd)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return MatchCategory.GenericPartialMatch;
        }

        StringComparison strictComparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        StringComparison broadComparison = StringComparison.OrdinalIgnoreCase;

        bool isCaseCorrect = address.IndexOf(pattern, strictComparison) >= 0;
        bool isPositionCorrect = isAtEnd ? address.EndsWith(pattern, broadComparison) : true;

        bool broadMatch = address.IndexOf(pattern, broadComparison) >= 0;

        if (!broadMatch)
        {
            return MatchCategory.GenericPartialMatch;
        }

        // Full Match
        if (isCaseCorrect && isPositionCorrect)
        {
            return MatchCategory.FullMatch;
        }

        // Partial Match Categories
        if (isCaseCorrect)
        {
            return MatchCategory.CaseMatch;
        }
        if (isPositionCorrect)
        {
            return MatchCategory.PositionMatch;
        }

        return MatchCategory.GenericPartialMatch;
    }

    private static void SaveMatchToFile(string desiredPattern, string address, string privateKey, string? seedPhrase, bool isFullMatch, MatchCategory category, long? index = null)
    {
        string baseFolder = "Results";


        string categoryFolderName = category.ToString();
        string finalFolderPath = Path.Combine(baseFolder, categoryFolderName);
        Directory.CreateDirectory(finalFolderPath);

        string filename = Path.Combine(finalFolderPath, $"{desiredPattern}.txt");

        using (StreamWriter writer = new StreamWriter(filename, true)) // 'true' to append
        {
            writer.WriteLine("------------------------------------------");
            writer.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Match Type: {category}");
            writer.WriteLine($"Address: {address}");
            if (!string.IsNullOrEmpty(privateKey))
            {
                writer.WriteLine($"Private Key: {privateKey}");
            }
            if (!string.IsNullOrEmpty(seedPhrase))
            {
                writer.WriteLine($"Seed Phrase: {seedPhrase}");
            }
            if (index.HasValue)
            {
                writer.WriteLine($"Index: {index.Value}");
            }
            writer.WriteLine("------------------------------------------");
        }

        Console.WriteLine($"\n✅ Saved to: {Path.GetFullPath(filename)}");
    }
}